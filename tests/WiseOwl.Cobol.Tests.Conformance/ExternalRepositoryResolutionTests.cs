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

    /// <summary>kb/Work PB4633: a verb's COBOL1760 sentence says WHY the word is no program prototype. A word a PROGRAM
    /// specifier DECLARES whose specifier no provider answered (REPO-1, §12.3.8.3 SR14) is declared, so the sentence
    /// must not say it is "neither declared"; a word no specifier declares keeps that sentence.</summary>
    private static string Refusal1760(CompilerDriver.Result r, string verbAndWord) =>
        Assert.Single(r.Errors, e => e.Contains("COBOL1760", StringComparison.Ordinal)
                                     && e.Contains(verbAndWord + ":", StringComparison.Ordinal));

    [Fact]
    public void ADeclaredSpecifierNoProviderAnswered_IsNotCalledUndeclaredByCallCancelOrCallAs()
    {
        WithDirectory("repo-1-declared", dir =>
        {
            const string source = """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. R2103C.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    PROGRAM R2103P.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 A PIC 9(4) VALUE 42.
                PROCEDURE DIVISION.
                    CALL R2103P USING A
                    CANCEL R2103P
                    CALL "ZZ" AS R2103P USING A
                    CANCEL NOSUCH
                    GOBACK.
                END PROGRAM R2103C.
                """;
            var r = Compile(dir, "R2103C", source);
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5962", StringComparison.Ordinal));
            foreach (string declared in new[] { "CALL R2103P", "CANCEL R2103P", "CALL AS R2103P" })
            {
                string text = Refusal1760(r, declared);
                Assert.Contains("declared by a PROGRAM specifier in the REPOSITORY paragraph", text, StringComparison.Ordinal);
                Assert.Contains("§12.3.8.3 SR14", text, StringComparison.Ordinal);
                Assert.DoesNotContain("neither declared", text, StringComparison.Ordinal);
            }
            string undeclared = Refusal1760(r, "CANCEL NOSUCH");
            Assert.Contains("neither declared by a PROGRAM specifier", undeclared, StringComparison.Ordinal);
            Assert.DoesNotContain("§12.3.8.3 SR14", undeclared, StringComparison.Ordinal);
        });
    }

    /// <summary>kb/Work PB4708: the SECOND route to a declared word with no prototype. <c>PROGRAM R4708P AS ""</c> is refused
    /// by §12.3.8.3 SR2 (a zero-length literal-3), so it registers no ProgramSpecifiers row, yet the REPOSITORY paragraph
    /// DECLARES the word: CALL, CANCEL and CALL AS say so, cite SR2 rather than SR14 (no REPO-1 exists on this route), and
    /// the USAGE PROGRAM-POINTER TO screen does not add its own "not declared" refusal.</summary>
    [Fact]
    public void ASpecifierWhoseLiteral3IsRefused_IsDeclaredAndNamesSr2()
    {
        WithDirectory("repo-literal3-refused", dir =>
        {
            const string source = """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. R4708C.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    PROGRAM R4708P AS "".
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 A PIC 9(4) VALUE 42.
                01 P TYPEDEF USAGE PROGRAM-POINTER TO R4708P.
                PROCEDURE DIVISION.
                    CALL R4708P USING A
                    CANCEL R4708P
                    CALL "ZZ" AS R4708P USING A
                    GOBACK.
                END PROGRAM R4708C.
                """;
            var r = Compile(dir, "R4708C", source);
            Assert.False(r.Success);
            foreach (string declared in new[] { "CALL R4708P", "CANCEL R4708P", "CALL AS R4708P" })
            {
                string text = Refusal1760(r, declared);
                Assert.Contains("declared by a PROGRAM specifier in the REPOSITORY paragraph", text, StringComparison.Ordinal);
                Assert.Contains("AS literal-3 was refused (ISO §12.3.8.3 SR2)", text, StringComparison.Ordinal);
                Assert.DoesNotContain("neither declared", text, StringComparison.Ordinal);
                Assert.DoesNotContain("SR14", text, StringComparison.Ordinal);
            }
            Assert.DoesNotContain(r.Errors, e => e.Contains("PROGRAM-POINTER TO R4708P", StringComparison.Ordinal)
                                                 && e.Contains("is not a program-prototype-name", StringComparison.Ordinal));
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
            // kb/Work PB4708: no REPO-1 exists on this route, so the CALL's sentence cites no SR14 refusal either.
            string call = Refusal1760(r, "CALL R2103P");
            Assert.Contains("refused by that repository's own finding", call, StringComparison.Ordinal);
            Assert.DoesNotContain("SR14", call, StringComparison.Ordinal);
            Assert.DoesNotContain("neither declared", call, StringComparison.Ordinal);
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
    public void APreviousBuildAnotherRuntimeMajorCompiled_IsRepo12_AndIsOverwritten()
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

    private const string ClassOnly = """
        IDENTIFICATION DIVISION.
        CLASS-ID. R2103K INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        END CLASS R2103K.
        """;

    private const string ProgramOfTheClass = """
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

    [Fact]   // REPO-11 (§6.4): the entry's formal is an object reference to a class neither this group nor any entry has.
    public void AnEntryNamingAClassNoEntryPublishes_IsRepo11()
    {
        WithDirectory("repo-11", dir =>
        {
            // The class is published by an assembly of its own (kb/Work PB2104: a class the entry's own assembly or another one
            // publishes is IMPORTED, so it is answered); the program's assembly is the only place searched.
            string classes = Path.Combine(dir, "classes"), programs = Path.Combine(dir, "programs");
            Assert.True(Compile(classes, "R2103K", ClassOnly).Success);
            var library = Compile(programs, "R2103O", ProgramOfTheClass, repositories: [classes]);
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
                """, repositories: [programs]);
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5971", StringComparison.Ordinal) && e.Contains("R2103K", StringComparison.Ordinal));
        });
    }

    // ISO §12.3.8.4 GR11 b): a function prototype definition of the group supplies the details and "the details in the
    // external repository are ignored" (cite.py --check 12.3.8.4 "the details in the external repository are ignored").
    // So an entry the group's prototype makes irrelevant is never a fault of the compilation, whatever is wrong with it.

    private static string FunctionLibrary(string asClause, string name = "R2103F") => $"""
        IDENTIFICATION DIVISION.
        FUNCTION-ID. {name}{asClause}.
        DATA DIVISION.
        LINKAGE SECTION.
        01 X PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING X RETURNING R.
            COMPUTE R = X * 2
            GOBACK.
        END FUNCTION {name}.
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

    /// <summary>Makes the entry at <paramref name="dll"/> one another runtime major compiled: its recorded runtime version
    /// (the attribute blob's UTF-8 text, which the metadata reader decodes) names the NEXT major
    /// (<see cref="RuntimeVersionSkew.OtherMajor"/>; the previous one at an all-nines major, <see cref="RuntimeVersionSkew.OtherMajorOf"/>),
    /// in the same number of bytes so every offset of the assembly stays. REPO-7 and REPO-12 fire on any major that differs from
    /// the running runtime's (REPO-7's input, <c>RuntimeAbi.Skew</c>), so the direction is immaterial.</summary>
    private static void MakeStale(string dll) =>
        Assert.True(RuntimeVersionSkew.Patch(dll, System.Text.Encoding.UTF8) > 0, "the library records its runtime version nowhere in UTF-8");

    /// <summary>A custom-attribute blob's SerString for text shorter than 128 bytes: one length byte, then the UTF-8 (ECMA-335
    /// II.23.3). Searching for the length-prefixed form matches a named argument or a value, never a longer text it sits in.</summary>
    private static byte[] SerString(string text)
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(text);
        Assert.True(utf8.Length < 128);
        return [(byte)utf8.Length, .. utf8];
    }

    /// <summary>Damages ONE kind of record of the compiled library at <paramref name="dll"/>: every attribute blob's named argument
    /// <paramref name="argument"/> is renamed (the last character changed, so the blob keeps its length), which the reader meets
    /// as the argument being absent. The elementary entries' <c>SignKind</c> is mandatory (<c>RepositoryReader</c> refuses an
    /// elementary entry that states none), so the entry is malformed at its record (REPO-4's input).</summary>
    private static void RenameNamedArgument(string dll, string argument)
    {
        string renamed = argument[..^1] + (argument[^1] == 'X' ? 'Y' : 'X');
        Assert.True(ByteImage.PatchFile(dll, SerString(argument), SerString(renamed)) > 0, $"no attribute of the library names the argument {argument}");
    }

    /// <summary>Damages the description of a library's formal so the reader accepts it and the codec cannot rebuild it: the
    /// redefinition <paramref name="target"/> names (a named argument the reader does not cross-check) is changed to a name no
    /// preceding sibling carries (REPO-5's input). The target's own entry keeps its name.</summary>
    private static void RedirectRedefinition(string dll, string target, string newTarget) =>
        Assert.True(ByteImage.PatchFile(dll, [.. SerString("RedefinesTarget"), .. SerString(target)],
            [.. SerString("RedefinesTarget"), .. SerString(newTarget)]) > 0, $"no description of the library redefines '{target}'");

    /// <summary>Damages the library's index (the assembly attribute <c>CobolRepositoryAttribute.Units</c>): the separator
    /// after the unit's externalized name becomes a character the row grammar has no use for, so the row has one field instead of
    /// two (<c>RepositoryIndex.TryDecodeUnit</c>, REPO-4's other input).</summary>
    private static void BreakIndexRow(string dll, string unit) =>
        Assert.True(ByteImage.PatchFile(dll, System.Text.Encoding.UTF8.GetBytes($":{unit}:{unit}"), System.Text.Encoding.UTF8.GetBytes($":{unit}={unit}")) > 0,
            $"the index of the library has no row for {unit}");

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

    [Fact]   // GR11 b): the same, with an entry another runtime major compiled (REPO-7's entry).
    public void AGroupFunctionPrototype_IgnoresAnEntryAnotherRuntimeMajorCompiled()
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

    [Fact]   // REPO-13 across the repository (D-R3 step 3): the externalized name wins, and the function only the word names is reported. The case pb2103_repo13_across_repository runs it.
    public void AFunctionExternalizedAsTheWord_AndAnotherNamedByIt_BothInTheRepository_IsRepo13()
    {
        WithDirectory("repo-13", dir =>
        {
            Assert.True(Compile(dir, "R2103G", FunctionLibrary(ShadowingClause)).Success);   // F AS "R2103G": the word R2103F, externalized R2103G
            Assert.True(Compile(dir, "R2103F", FunctionLibrary(" AS \"R2103F\"", "R2103X")).Success);   // X AS "R2103F": externalized R2103F
            var r = Compile(dir, "R2103FM", FunctionSpecifierCaller);
            Assert.True(r.Success, Codes(r));
            Assert.Contains(r.Warnings, w => w.Contains("COBOL2969", StringComparison.Ordinal) && w.Contains("R2103G", StringComparison.Ordinal));
            Assert.DoesNotContain("COBOL5962", Codes(r), StringComparison.Ordinal);
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "R2103FM.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("0042", stdout);   // the program activated, whichever function it was, ran: 21 * 2
        });
    }

    private const string SplitDefinition = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103SD AS "R2103S".
        DATA DIVISION.
        LINKAGE SECTION.
        01 D-G.
           03 D-S.
              05 D-A PIC 9(2).
           03 D-B PIC X(2).
        PROCEDURE DIVISION USING D-G.
            GOBACK.
        END PROGRAM R2103SD.
        """;

    private static string SplitPrototypeCaller(string secondField) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103S IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-G.
           05 L-A PIC 9(2).
           05 L-B {secondField}.
        PROCEDURE DIVISION USING L-G.
        END PROGRAM R2103S.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103SM.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            PROGRAM R2103S.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-G.
           05 W-A PIC 9(2) VALUE 7.
           05 W-B PIC X(2) VALUE "XY".
        PROCEDURE DIVISION.
            CALL R2103S USING W-G
            GOBACK.
        END PROGRAM R2103SM.
        """;

    [Fact]   // §13.7.3 SR2 over two groups: a prototype that groups and names its fields otherwise than the definition draws no REPO-9; one that lays them out otherwise does.
    public void APrototypeAndItsDefinitionInTwoUnits_AreComparedByLayout_NotByGroupingOrNames()
    {
        WithDirectory("repo-split", dir =>
        {
            Assert.True(Compile(dir, "R2103S", SplitDefinition).Success);
            var same = Compile(dir, "R2103SM", SplitPrototypeCaller("PIC X(2)"));
            Assert.True(same.Success, Codes(same));
            Assert.DoesNotContain("COBOL5969", Codes(same), StringComparison.Ordinal);
            var other = Compile(dir, "R2103SM", SplitPrototypeCaller("PIC X(3)"));
            Assert.Contains(other.Warnings, w => w.Contains("COBOL5969", StringComparison.Ordinal));   // the control: a layout that differs is reported
        });
    }

    private const string ProgramPrototypeOnly = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103P IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 N PIC 9(4).
        PROCEDURE DIVISION USING N.
        END PROGRAM R2103P.
        """;

    private const string FunctionPrototypeOnly = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. R2103F IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 X PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING X RETURNING R.
        END FUNCTION R2103F.
        """;

    [Fact]   // §10.6.3 GR1: compiling a program prototype ALONE generates the external-repository information a later compilation reads (GR10 c)).
    public void AProgramPrototypeCompiledAlone_PublishesItsSignature_AndACallerIsCheckedAgainstIt()
    {
        WithDirectory("repo-proto-alone", dir =>
        {
            Assert.True(Compile(dir, "R2103P", ProgramPrototypeOnly).Success);
            var refused = Compile(dir, "R2103C", Caller("X"));
            Assert.False(refused.Success);
            Assert.Contains("COBOL1688", Codes(refused), StringComparison.Ordinal);   // the formal is PIC 9(4); the argument PIC X(10)
            var bound = Compile(dir, "R2103C", Caller("A"));
            Assert.True(bound.Success, Codes(bound));
            Assert.True(bound.Inputs.ReadsRepository);
        });
    }

    [Fact]   // §10.6.3 GR1, the function prototype's half: the entry it publishes answers a later FUNCTION specifier (GR11 c)) — and with update off it does not.
    public void AFunctionPrototypeCompiledAlone_PublishesItsSignature_UnlessUpdateIsOff()
    {
        WithDirectory("repo-fn-proto-alone", dir =>
        {
            Assert.True(Compile(dir, "R2103F", FunctionPrototypeOnly).Success);
            var bound = Compile(dir, "R2103FM", FunctionSpecifierCaller);
            Assert.True(bound.Success, Codes(bound));
            Assert.True(bound.Inputs.ReadsRepository);
            Assert.True(Compile(dir, "R2103F", FunctionPrototypeOnly, update: false).Success);
            var silent = Compile(dir, "R2103FM", FunctionSpecifierCaller);
            Assert.False(silent.Success);   // built without signatures: the entry is found, and cannot answer (REPO-8)
            Assert.Contains("COBOL5968", Codes(silent), StringComparison.Ordinal);
        });
    }

    // REPO-4 and REPO-5 (ISO §8.13: the information in the external repository is what the compilation reads; DESIGN-external-repository
    // §14, §15.3 "nothing an entry holds is trusted"): a compiled library with ONE record damaged in a way the reader or the codec
    // detects. Each case asserts the code, and that no REPO-1 (COBOL5962) follows it: the entry was found, it is the entry that cannot answer.

    private const string RedefiningLibrary = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103R.
        DATA DIVISION.
        LINKAGE SECTION.
        01 G.
           05 RDFBASEA PIC X.
           05 RDFOVERA REDEFINES RDFBASEA PIC X.
        PROCEDURE DIVISION USING G.
            GOBACK.
        END PROGRAM R2103R.
        """;

    private const string RedefiningCaller = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. R2103RC.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            PROGRAM R2103R.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 G.
           05 A PIC X.
        PROCEDURE DIVISION.
            CALL R2103R USING G
            GOBACK.
        END PROGRAM R2103RC.
        """;

    [Fact]   // REPO-4, the reader's half: an elementary entry of a formal states no SignKind (RepositoryReader.Read -> Malformed).
    public void AnEntryWhoseRecordIsMalformed_IsRepo4()
    {
        WithDirectory("repo-4-record", dir =>
        {
            Assert.True(Compile(dir, "R2103P", Callee).Success);
            RenameNamedArgument(Path.Combine(dir, "R2103P.dll"), "SignKind");
            var r = Compile(dir, "R2103C", Caller("A"));
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5965", StringComparison.Ordinal) && e.Contains("R2103P.dll", StringComparison.Ordinal)
                && e.Contains("SignKind", StringComparison.Ordinal));
            Assert.DoesNotContain("COBOL5962", Codes(r), StringComparison.Ordinal);
        });
    }

    [Fact]   // REPO-4, the index's half: a row of the units index with one field (RepositoryIndex.TryDecodeUnit refuses it).
    public void AnEntryWhoseIndexIsMalformed_IsRepo4()
    {
        WithDirectory("repo-4-index", dir =>
        {
            Assert.True(Compile(dir, "R2103P", Callee).Success);
            BreakIndexRow(Path.Combine(dir, "R2103P.dll"), "R2103P");
            var r = Compile(dir, "R2103C", Caller("A"));
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5965", StringComparison.Ordinal) && e.Contains("R2103P.dll", StringComparison.Ordinal));
            Assert.DoesNotContain("COBOL5962", Codes(r), StringComparison.Ordinal);
        });
    }

    [Fact]   // REPO-5: the reader accepts the record and the codec cannot rebuild it (a REDEFINES naming no preceding sibling).
    public void AnEntryWhoseDescriptionTheCodecCannotRebuild_IsRepo5()
    {
        WithDirectory("repo-5", dir =>
        {
            Assert.True(Compile(dir, "R2103R", RedefiningLibrary).Success);
            var intact = Compile(dir, "R2103RC", RedefiningCaller);
            Assert.True(intact.Success, Codes(intact));   // the undamaged entry is readable, so the damage is the only difference
            RedirectRedefinition(Path.Combine(dir, "R2103R.dll"), "RDFBASEA", "RDFBASEB");
            var r = Compile(dir, "R2103RC", RedefiningCaller);
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5966", StringComparison.Ordinal) && e.Contains("RDFBASEB", StringComparison.Ordinal));
            Assert.DoesNotContain("COBOL5962", Codes(r), StringComparison.Ordinal);
            Assert.DoesNotContain("COBOL5965", Codes(r), StringComparison.Ordinal);   // not the reader's finding
        });
    }
}
