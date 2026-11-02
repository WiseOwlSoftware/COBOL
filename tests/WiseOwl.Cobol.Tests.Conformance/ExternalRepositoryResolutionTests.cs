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
}
