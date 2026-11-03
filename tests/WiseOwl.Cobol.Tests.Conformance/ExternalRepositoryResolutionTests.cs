// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE EXTERNAL REPOSITORY'S PROVIDER AND ITS FINDINGS, end to end through the compiler (ISO §12.3.8.4 GR10 c) / GR11 c),
/// §8.13; DESIGN-external-repository §8.2, §8.6, §9.2, §9.3, §14, §17.6; kb/Work PB2103). Each Fact compiles a library into a
/// directory and a caller against it, as two separate compilations, and asserts what the caller's compilation says: these are
/// the cases that need a directory layout, an option or a damaged file no manifest case states (the multi-unit goldens of
/// §17.1 cover the default layout, <c>conformance-suite/wiseowl/cases/</c>).
/// <para>The rules, checked: §12.3.8.3 SR14 "the name of a program for which information exists in the external repository"
/// and §12.3.8.4 GR10 c) "the details are taken from the external repository for the program with the same name as the
/// externalized name of the program prototype"; §8.13 "The implementor shall provide a mechanism that allows the user to
/// specify whether to update the external repository when a compilation unit is compiled" and "… whether to flag differences
/// in prototypes and definitions in the compilation group from the information in the external repository".</para>
/// </summary>
public sealed class ExternalRepositoryResolutionTests
{
    private const string Callee = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103P.
        DATA DIVISION.
        LINKAGE SECTION.
        01 N PIC 9(4).
        PROCEDURE DIVISION USING N.
            DISPLAY "CALLEE " N
            GOBACK.
        END PROGRAM R2103P.
        """;

    private const string CalleeChanged = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103P.
        DATA DIVISION.
        LINKAGE SECTION.
        01 N PIC X(4).
        PROCEDURE DIVISION USING N.
            DISPLAY "CALLEE " N
            GOBACK.
        END PROGRAM R2103P.
        """;

    private static string Caller(string argument) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103C.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            PROGRAM R2103P.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 A PIC 9(4) VALUE 42.
        01 X PIC X(10) VALUE "ABCD".
        PROCEDURE DIVISION.
            CALL R2103P USING {argument}
            GOBACK.
        END PROGRAM R2103C.
        """;

    private static CompilerDriver.Result Compile(string dir, string name, string source, IReadOnlyList<string>? repositories = null,
        IReadOnlyList<string>? references = null, bool update = true, bool check = true)
    {
        Directory.CreateDirectory(dir);
        string src = CompiledProgramCache.StageSource(Path.Combine(dir, name + ".cob"), source);
        return CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: 2014,
            SourceFormat: InitialReferenceFormat.Free, RepositoryUpdate: update, RepositoryPaths: repositories,
            ReferencePaths: references, RepositoryCheck: check));
    }

    private static void WithDirectory(string tag, Action<string> body)
    {
        string dir = ScratchDirectory.Create(tag);
        try { body(dir); }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static string Codes(CompilerDriver.Result r) => string.Join("\n", r.Errors.Concat(r.Warnings));

    [Fact]   // GR10 c): the details are the entry's, so the CALL is checked at compile time against the definition built apart.
    public void ACallerIsCheckedAgainstTheEntryInItsOutputDirectory()
    {
        WithDirectory("repo-c", dir =>
        {
            Assert.True(Compile(dir, "R2103P", Callee).Success);
            var refused = Compile(dir, "R2103C", Caller("X"));
            Assert.False(refused.Success);
            Assert.Contains("COBOL1688", Codes(refused), StringComparison.Ordinal);
            var bound = Compile(dir, "R2103C", Caller("A"));
            Assert.True(bound.Success, Codes(bound));
            Assert.True(bound.Inputs.ReadsRepository);
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "R2103C.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("CALLEE 0042", stdout);
        });
    }

    [Fact]   // REPO-1 (SR14), naming the place searched; the CALL through the unregistered prototype is COBOL1760 (§8.2 step 4).
    public void ASpecifierNoProviderAnswers_IsRepo1_NamingThePlacesSearched()
    {
        WithDirectory("repo-1", dir =>
        {
            var r = Compile(dir, "R2103C", Caller("A"));
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5962", StringComparison.Ordinal) && e.Contains("searched: the directory", StringComparison.Ordinal));
            Assert.Contains("COBOL1760", Codes(r), StringComparison.Ordinal);
        });
    }

    [Fact]   // §9.3: a --repository directory is searched instead of the output directory, and a --reference file after it.
    public void TheNamedDirectoriesAndReferences_AreTheSearchSpace()
    {
        WithDirectory("repo-dirs", dir =>
        {
            string lib = Path.Combine(dir, "lib"), app = Path.Combine(dir, "app");
            Assert.True(Compile(lib, "R2103P", Callee).Success);
            Assert.True(Compile(app, "R2103C", Caller("A"), repositories: [lib]).Success);
            Assert.True(Compile(app, "R2103C", Caller("A"), repositories: [app], references: [Path.Combine(lib, "R2103P.dll")]).Success);
            Assert.Contains("COBOL5962", Codes(Compile(app, "R2103C", Caller("A"))), StringComparison.Ordinal);
        });
    }

    [Fact]   // REPO-10: a named place that does not exist is the command's error.
    public void ANamedRepositoryThatDoesNotExist_IsRepo10()
    {
        WithDirectory("repo-10", dir =>
        {
            var r = Compile(dir, "R2103C", Caller("A"), repositories: [Path.Combine(dir, "missing")],
                references: [Path.Combine(dir, "missing.dll")]);
            Assert.False(r.Success);
            Assert.Equal(2, r.Errors.Count(e => e.Contains("COBOL5970", StringComparison.Ordinal)));
        });
    }

    [Fact]   // REPO-8: an entry built with --repository-update off carries no signature to bind against (§8.13, §9.1).
    public void AnEntryBuiltWithoutSignatures_IsRepo8()
    {
        WithDirectory("repo-8", dir =>
        {
            Assert.True(Compile(dir, "R2103P", Callee, update: false).Success);
            var r = Compile(dir, "R2103C", Caller("A"));
            Assert.False(r.Success);
            Assert.Contains("COBOL5968", Codes(r), StringComparison.Ordinal);
            Assert.DoesNotContain("COBOL5962", Codes(r), StringComparison.Ordinal);
        });
    }

    [Fact]   // REPO-3: the file named after the unit is no managed assembly, and nothing else publishes the unit.
    public void AFileNamedAfterTheUnitThatIsNoAssembly_IsRepo3()
    {
        WithDirectory("repo-3", dir =>
        {
            File.WriteAllText(Path.Combine(dir, "R2103P.dll"), "not a portable executable");
            var r = Compile(dir, "R2103C", Caller("A"));
            Assert.False(r.Success);
            Assert.Contains("COBOL5964", Codes(r), StringComparison.Ordinal);
            Assert.DoesNotContain("COBOL5962", Codes(r), StringComparison.Ordinal);
        });
    }

    [Fact]   // REPO-2 (§8.6): the first entry in search order wins; a later different build of the name that differs warns.
    public void ALaterDifferingEntry_IsRepo2_AndTheFirstIsUsed()
    {
        WithDirectory("repo-2", dir =>
        {
            string first = Path.Combine(dir, "first"), second = Path.Combine(dir, "second"), app = Path.Combine(dir, "app");
            Assert.True(Compile(first, "R2103P", Callee).Success);
            Assert.True(Compile(second, "R2103P", CalleeChanged).Success);
            var r = Compile(app, "R2103C", Caller("A"), repositories: [first, second]);
            Assert.True(r.Success, Codes(r));
            Assert.Contains(r.Warnings, w => w.Contains("COBOL5963", StringComparison.Ordinal) && w.Contains(second, StringComparison.Ordinal));
        });
    }

    [Fact]   // REPO-9 (§9.2): the previous build of the output differs; --repository-check off does not compare.
    public void ARebuildThatChangesTheSignature_IsRepo9_UnlessCheckingIsOff()
    {
        WithDirectory("repo-9", dir =>
        {
            Assert.True(Compile(dir, "R2103P", Callee).Success);
            var changed = Compile(dir, "R2103P", CalleeChanged);
            Assert.True(changed.Success, Codes(changed));
            Assert.Contains(changed.Warnings, w => w.Contains("COBOL5969", StringComparison.Ordinal) && w.Contains("USING formal 1", StringComparison.Ordinal));
            var unchecked_ = Compile(dir, "R2103P", Callee, check: false);
            Assert.True(unchecked_.Success);
            Assert.DoesNotContain(unchecked_.Warnings, w => w.Contains("COBOL5969", StringComparison.Ordinal));
            var same = Compile(dir, "R2103P", Callee);
            Assert.DoesNotContain(same.Warnings, w => w.Contains("COBOL5969", StringComparison.Ordinal));
        });
    }

    [Fact]   // REPO-12 (§9.2): the previous build of the output was compiled by another runtime major, so it is not compared.
    public void APreviousBuildAnOlderRuntimeCompiled_IsRepo12_AndIsOverwritten()
    {
        WithDirectory("repo-12", dir =>
        {
            Assert.True(Compile(dir, "R2103P", Callee).Success);
            MakeStale(Path.Combine(dir, "R2103P.dll"));
            var rebuilt = Compile(dir, "R2103P", CalleeChanged);
            Assert.True(rebuilt.Success, Codes(rebuilt));
            Assert.Contains(rebuilt.Warnings, w => w.Contains("COBOL5972", StringComparison.Ordinal));
            Assert.DoesNotContain(rebuilt.Warnings, w => w.Contains("COBOL5969", StringComparison.Ordinal));   // not compared: no REPO-9
        });
    }

    [Fact]   // §9.3: the output being produced is never resolved against, even when its previous build publishes the name.
    public void TheOutputsOwnPreviousBuild_AnswersNoSpecifier()
    {
        WithDirectory("repo-own", dir =>
        {
            Assert.True(Compile(dir, "R2103C", Callee + "\n" + Caller("A")).Success);
            var r = Compile(dir, "R2103C", Caller("A"));
            Assert.Contains("COBOL5962", Codes(r), StringComparison.Ordinal);
        });
    }

    [Fact]   // §17.6: a group that answers every specifier itself consults no repository.
    public void AGroupThatAnswersEverySpecifier_ReadsNoRepository()
    {
        WithDirectory("repo-zero", dir =>
        {
            var r = Compile(dir, "R2103G", Callee + "\n" + Caller("A"));
            Assert.True(r.Success, Codes(r));
            Assert.False(r.Inputs.ReadsRepository);
        });
    }

    private const string ClassLibrary = """
        IDENTIFICATION DIVISION.
        CLASS-ID. R2103K INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        END CLASS R2103K.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103O.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS R2103K.
        DATA DIVISION.
        LINKAGE SECTION.
        01 O USAGE OBJECT REFERENCE R2103K.
        PROCEDURE DIVISION USING O.
            GOBACK.
        END PROGRAM R2103O.
        """;

    [Fact]   // REPO-11 (§6.4): the entry's formal is an object reference to a class this group does not have.
    public void AnEntryNamingAClassTheGroupLacks_IsRepo11()
    {
        WithDirectory("repo-11", dir =>
        {
            var library = Compile(dir, "R2103O", ClassLibrary);
            Assert.True(library.Success, Codes(library));
            var r = Compile(dir, "R2103U", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. R2103U.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    PROGRAM R2103O.
                PROCEDURE DIVISION.
                    GOBACK.
                END PROGRAM R2103U.
                """);
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5971", StringComparison.Ordinal) && e.Contains("R2103K", StringComparison.Ordinal));
        });
    }

    // ISO §12.3.8.4 GR11 b): a function prototype definition of the group supplies the details and "the details in the
    // external repository are ignored" (cite.py --check 12.3.8.4 "the details in the external repository are ignored").
    // So an entry the group's prototype makes irrelevant is never a fault of the compilation, whatever is wrong with it.

    private static string FunctionLibrary(string asClause) => $"""
        IDENTIFICATION DIVISION.
        FUNCTION-ID. R2103F{asClause}.
        DATA DIVISION.
        LINKAGE SECTION.
        01 X PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING X RETURNING R.
            COMPUTE R = X * 2
            GOBACK.
        END FUNCTION R2103F.
        """;

    private const string FunctionPrototypeCaller = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. R2103F IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 X PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING X RETURNING R.
        END FUNCTION R2103F.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103FM.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION R2103F.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 N PIC 9(4) VALUE 21.
        PROCEDURE DIVISION.
            DISPLAY FUNCTION R2103F(N)
            GOBACK.
        END PROGRAM R2103FM.
        """;

    private const string FunctionSpecifierCaller = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103FM.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION R2103F.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 N PIC 9(4) VALUE 21.
        PROCEDURE DIVISION.
            DISPLAY FUNCTION R2103F(N)
            GOBACK.
        END PROGRAM R2103FM.
        """;

    private const string ShadowingClause = " AS \"R2103G\"";

    /// <summary>Makes the entry at <paramref name="dll"/> one an older runtime major compiled: its recorded runtime version
    /// (the attribute blob's UTF-8 text, which the metadata reader decodes) names the previous major, in the same number of
    /// bytes so every offset of the assembly stays (REPO-7's input, <c>RuntimeAbi.Skew</c>).</summary>
    private static void MakeStale(string dll)
    {
        byte[] bytes = File.ReadAllBytes(dll);
        var runtime = WiseOwl.Cobol.Runtime.RuntimeAbi.Version;
        var current = System.Text.Encoding.UTF8.GetBytes(runtime.ToString());
        var previous = System.Text.Encoding.UTF8.GetBytes(new Version(runtime.Major - 1, runtime.Minor, runtime.Build, runtime.Revision).ToString());
        Assert.Equal(current.Length, previous.Length);
        int replaced = 0;
        for (int at = bytes.AsSpan().IndexOf(current); at >= 0; at = bytes.AsSpan().IndexOf(current))
        {
            previous.CopyTo(bytes, at);
            replaced++;
        }
        Assert.True(replaced > 0, "the library records its runtime version nowhere in UTF-8");
        File.WriteAllBytes(dll, bytes);
    }

    [Fact]   // GR11 b): a library built without signatures (REPO-8's entry) beside the group's own prototype of its function.
    public void AGroupFunctionPrototype_IgnoresAnEntryBuiltWithoutSignatures()
    {
        WithDirectory("repo-fn-b8", dir =>
        {
            Assert.True(Compile(dir, "R2103F", FunctionLibrary(""), update: false).Success);
            var r = Compile(dir, "R2103FM", FunctionPrototypeCaller);
            Assert.True(r.Success, Codes(r));
            Assert.Empty(r.Warnings);
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "R2103FM.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("0042", stdout);   // the function activated is the one externalized R2103F: 21 * 2
        });
    }

    [Fact]   // GR11 b): the same, with an entry an older runtime major compiled (REPO-7's entry).
    public void AGroupFunctionPrototype_IgnoresAnEntryAnOlderRuntimeCompiled()
    {
        WithDirectory("repo-fn-b7", dir =>
        {
            Assert.True(Compile(dir, "R2103F", FunctionLibrary("")).Success);
            MakeStale(Path.Combine(dir, "R2103F.dll"));
            var r = Compile(dir, "R2103FM", FunctionPrototypeCaller);
            Assert.True(r.Success, Codes(r));
            Assert.Empty(r.Warnings);
        });
    }

    [Fact]   // GR11 c) is still asked when the group has no prototype: the same two entries are REPO-8 and REPO-7.
    public void AFunctionSpecifierTheGroupDoesNotAnswer_ReportsTheEntrysFault()
    {
        WithDirectory("repo-fn-c", dir =>
        {
            Assert.True(Compile(dir, "R2103F", FunctionLibrary(""), update: false).Success);
            var unsigned = Compile(dir, "R2103FM", FunctionSpecifierCaller);
            Assert.False(unsigned.Success);
            Assert.Contains("COBOL5968", Codes(unsigned), StringComparison.Ordinal);
            Assert.DoesNotContain("COBOL5962", Codes(unsigned), StringComparison.Ordinal);
            Assert.True(Compile(dir, "R2103F", FunctionLibrary("")).Success);
            MakeStale(Path.Combine(dir, "R2103F.dll"));
            var stale = Compile(dir, "R2103FM", FunctionSpecifierCaller);
            Assert.False(stale.Success);
            Assert.Contains("COBOL5967", Codes(stale), StringComparison.Ordinal);
            Assert.DoesNotContain("COBOL5962", Codes(stale), StringComparison.Ordinal);
        });
    }

    [Fact]   // D-R3 step 2: the first entry carrying the word is the answer; an unreadable one is its own finding (no REPO-1 beside it), a later one nobody's.
    public void AWordAnsweredByTheFirstEntry_ReportsOnlyThatEntrysFault()
    {
        WithDirectory("repo-fn-word", dir =>
        {
            string first = Path.Combine(dir, "first"), second = Path.Combine(dir, "second"), app = Path.Combine(dir, "app");
            Assert.True(Compile(first, "R2103G", FunctionLibrary(ShadowingClause)).Success);
            Assert.True(Compile(second, "R2103H", FunctionLibrary(" AS \"R2103H\"")).Success);
            MakeStale(Path.Combine(second, "R2103H.dll"));
            var answered = Compile(app, "R2103FM", FunctionSpecifierCaller, repositories: [first, second]);
            Assert.True(answered.Success, Codes(answered));   // R2103G answers; R2103H also carries the word R2103F but is no one's answer
            MakeStale(Path.Combine(first, "R2103G.dll"));
            var refused = Compile(app, "R2103FM", FunctionSpecifierCaller, repositories: [first, second]);
            Assert.False(refused.Success);
            Assert.Contains("COBOL5967", Codes(refused), StringComparison.Ordinal);
            Assert.DoesNotContain("COBOL5962", Codes(refused), StringComparison.Ordinal);
        });
    }

    [Fact]   // D-R3 step 3: a repository function carrying the word shadows the group's prototype and warns; one that cannot be read is no fault.
    public void AShadowingRepositoryFunction_WarnsWhenReadable_AndIsQuietWhenNot()
    {
        WithDirectory("repo-fn-shadow", dir =>
        {
            // R2103F AS "R2103G": the user-function-name R2103F, externalized R2103G (§8.4.6.7). The group's prototype is
            // externalized R2103F, so GR11 b) selects it and the repository function shadows it by word.
            Assert.True(Compile(dir, "R2103G", FunctionLibrary(ShadowingClause)).Success);
            var warned = Compile(dir, "R2103FM", FunctionPrototypeCaller);
            Assert.True(warned.Success, Codes(warned));
            Assert.Contains(warned.Warnings, w => w.Contains("COBOL2969", StringComparison.Ordinal) && w.Contains("R2103G", StringComparison.Ordinal));
            Assert.True(Compile(dir, "R2103G", FunctionLibrary(ShadowingClause), update: false).Success);
            var unsigned = Compile(dir, "R2103FM", FunctionPrototypeCaller);
            Assert.True(unsigned.Success, Codes(unsigned));
            Assert.Empty(unsigned.Warnings);
            Assert.True(Compile(dir, "R2103G", FunctionLibrary(ShadowingClause)).Success);
            MakeStale(Path.Combine(dir, "R2103G.dll"));
            var stale = Compile(dir, "R2103FM", FunctionPrototypeCaller);
            Assert.True(stale.Success, Codes(stale));
            Assert.Empty(stale.Warnings);
        });
    }
}
