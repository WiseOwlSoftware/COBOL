// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A CLASS ANOTHER COMPILATION GROUP DEFINES, IMPORTED FROM THE EXTERNAL REPOSITORY (ISO §2023:12.3.8.3 SR6, §2023:12.3.8.4 GR6;
/// DESIGN-external-repository §8.4, §10.1, §10.2, §14; kb/Work PB2104). Each Fact compiles a library of classes into a directory
/// and a caller against it, as two separate compilations, and asserts what the caller's compilation says or does. The
/// multi-unit golden of §17.1 (<c>conformance-suite/wiseowl/cases/12.3.8-REPOSITORY/pb2104_import_class_under_another_word</c>)
/// covers the default layout and the run-time identity; these are the cases that need a directory layout, a damaged chain or an
/// option no manifest case states.
/// <para>The rules, checked: §2023:12.3.8.3 SR6 "a) If literal-1 is specified, there shall be information in the external repository
/// for the class literal-1" and "b) If literal-1 is not specified, there shall be information in the external repository for the
/// class object-class-name-1"; §2023:12.3.8.4 GR6 "The compiler shall use the information specified for object-class-name-1 together
/// with the external repository to determine the details of the class that is to be used".</para>
/// </summary>
public sealed class ExternalRepositoryClassImportTests
{
    private static string Base(string word, string externalized) => $"""
        IDENTIFICATION DIVISION.
        CLASS-ID. {word} AS "{externalized}" INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        OBJECT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 BAL PIC 9(4) VALUE 0.
        PROCEDURE DIVISION.
        METHOD-ID. DEPOSIT.
        DATA DIVISION.
        LINKAGE SECTION.
        01 AMT PIC 9(4).
        PROCEDURE DIVISION USING AMT.
            ADD AMT TO BAL.
        END METHOD DEPOSIT.
        METHOD-ID. BALANCE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 OUT-BAL PIC 9(4).
        PROCEDURE DIVISION RETURNING OUT-BAL.
            MOVE BAL TO OUT-BAL.
        END METHOD BALANCE.
        END OBJECT.
        END CLASS {word}.
        """;

    /// <summary>A class that inherits an imported one and overrides its method, calling SUPER (the chain's second link).</summary>
    private const string Savings = """
        IDENTIFICATION DIVISION.
        CLASS-ID. SAVINGS AS "P2104-Savings" INHERITS FROM LEDGER.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS LEDGER AS "P2104-Ledger".
        OBJECT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 RATE PIC 9(2) VALUE 5.
        PROCEDURE DIVISION.
        METHOD-ID. DEPOSIT OVERRIDE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 AMT PIC 9(4).
        PROCEDURE DIVISION USING AMT.
            ADD RATE TO AMT
            INVOKE SUPER "DEPOSIT" USING AMT.
        END METHOD DEPOSIT.
        END OBJECT.
        END CLASS SAVINGS.
        """;

    private static string Client(string specifiers, string word) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P2104C.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            {specifiers}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 A OBJECT REFERENCE {word}.
        01 AMT PIC 9(4) VALUE 20.
        01 B PIC 9(4).
        PROCEDURE DIVISION.
            INVOKE {word} "New" RETURNING A
            INVOKE A "DEPOSIT" USING AMT
            INVOKE A "BALANCE" RETURNING B
            DISPLAY "BALANCE " B
            GOBACK.
        END PROGRAM P2104C.
        """;

    private static CompilerDriver.Result Compile(string dir, string name, string source, IReadOnlyList<string>? repositories = null,
        IReadOnlyList<string>? references = null)
    {
        Directory.CreateDirectory(dir);
        string src = CompiledProgramCache.StageSource(Path.Combine(dir, name + ".cob"), source);
        return CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: 2014,
            SourceFormat: InitialReferenceFormat.Free, RepositoryPaths: repositories, ReferencePaths: references));
    }

    private static void WithDirectory(string tag, Action<string> body)
    {
        string dir = ScratchDirectory.Create(tag);
        try { body(dir); }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static string Codes(CompilerDriver.Result r) => string.Join("\n", r.Errors.Concat(r.Warnings));

    [Fact]   // SR6 b): the imported class is found by the identity the specifier states (lit ?? word), whatever the importing word.
    public void AClassOfAnotherGroup_IsFoundByIdentity_AndItsMethodsRun()
    {
        WithDirectory("class-import", dir =>
        {
            Assert.True(Compile(dir, "P2104LEDGER", Base("LEDGER", "P2104-Ledger")).Success);
            var r = Compile(dir, "P2104C", Client("CLASS ACCT AS \"P2104-Ledger\"", "ACCT"));
            Assert.True(r.Success, Codes(r));
            Assert.True(r.Inputs.ReadsRepository);
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "P2104C.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BALANCE 0020", stdout);
        });
    }

    [Fact]   // The word alone is the identity when no AS phrase is written (§2023:12.3.8.4 GR2); a --reference file is searched after the directories.
    public void AClassIsImportedByItsWord_FromAReferencedAssembly()
    {
        WithDirectory("class-reference", dir =>
        {
            string lib = Path.Combine(dir, "lib"), app = Path.Combine(dir, "app");
            Assert.True(Compile(lib, "P2104LEDGER", Base("LEDGER", "P2104-Ledger")).Success);
            Assert.True(Compile(lib, "P2104WORD", Base("P2104WORD", "P2104WORD")).Success);
            var r = Compile(app, "P2104C", Client("CLASS P2104WORD", "P2104WORD"), repositories: [app],
                references: [Path.Combine(lib, "P2104WORD.dll")]);
            Assert.True(r.Success, Codes(r));
            // The deployment contract (§2023:11.4): a class assembly the build references statically is loaded by the CLR from the
            // application directory, so it is deployed with the run unit as any .NET application's reference is.
            File.Copy(Path.Combine(lib, "P2104WORD.dll"), Path.Combine(app, "P2104WORD.dll"));
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(app, "P2104C.dll"), app);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BALANCE 0020", stdout);
        });
    }

    [Fact]   // The chain: a class that inherits an imported one is itself imported, its superclass by identity, its override linked.
    public void AnImportedClassInheritingAnImportedClass_ImportsTheWholeChain()
    {
        WithDirectory("class-chain", dir =>
        {
            Assert.True(Compile(dir, "P2104LEDGER", Base("LEDGER", "P2104-Ledger")).Success);
            var sub = Compile(dir, "P2104SAV", Savings);
            Assert.True(sub.Success, Codes(sub));
            var r = Compile(dir, "P2104C", Client("CLASS SAV AS \"P2104-Savings\"", "SAV"));
            Assert.True(r.Success, Codes(r));
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "P2104C.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BALANCE 0025", stdout);   // the override adds its RATE (5) to the 20 deposited, then calls the imported base's DEPOSIT
        });
    }

    [Fact]   // SR6: no information for the class is REPO-1 AT THE SPECIFIER, used or not.
    public void AClassSpecifierNothingAnswers_IsRepo1_NamingThePlacesSearched()
    {
        WithDirectory("class-repo1", dir =>
        {
            var r = Compile(dir, "P2104C", Client("CLASS P2104NOSUCH", "P2104NOSUCH"));
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5962", StringComparison.Ordinal)
                && e.Contains("REPOSITORY CLASS P2104NOSUCH", StringComparison.Ordinal)
                && e.Contains("searched: the directory", StringComparison.Ordinal));
            var unused = Compile(dir, "P2104U", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. P2104U.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    CLASS P2104NOSUCH.
                PROCEDURE DIVISION.
                    GOBACK.
                END PROGRAM P2104U.
                """);
            Assert.False(unused.Success);
            Assert.Contains("COBOL5962", Codes(unused), StringComparison.Ordinal);
        });
    }

    [Fact]   // SR9: no information for the interface is REPO-1 at the specifier, as SR6's is for a class.
    public void AnInterfaceSpecifierNothingAnswers_IsRepo1()
    {
        WithDirectory("iface-repo1", dir =>
        {
            var r = Compile(dir, "P2104U", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. P2104U.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    INTERFACE P2104NOIF.
                PROCEDURE DIVISION.
                    GOBACK.
                END PROGRAM P2104U.
                """);
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5962", StringComparison.Ordinal) && e.Contains("REPOSITORY INTERFACE P2104NOIF", StringComparison.Ordinal));
        });
    }

    [Fact]   // SR16: a PROPERTY specifier needs a property of the classes and interfaces declared in its own REPOSITORY paragraph.
    public void APropertySpecifierNoClassOfItsParagraphHas_IsRepo1()
    {
        WithDirectory("property-repo1", dir =>
        {
            Assert.True(Compile(dir, "P2104LEDGER", Base("LEDGER", "P2104-Ledger")).Success);
            var r = Compile(dir, "P2104U", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. P2104U.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    CLASS ACCT AS "P2104-Ledger"
                    PROPERTY P2104NOPROP.
                PROCEDURE DIVISION.
                    GOBACK.
                END PROGRAM P2104U.
                """);
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5962", StringComparison.Ordinal) && e.Contains("REPOSITORY PROPERTY P2104NOPROP", StringComparison.Ordinal));
        });
    }

    [Fact]   // The standard class BASE and a class the group defines need no repository entry (SR6 reads the group's own definitions too).
    public void TheStandardClassAndAGroupClass_AreInformationEnough()
    {
        WithDirectory("class-group", dir =>
        {
            var r = Compile(dir, "P2104G", Base("P2104G", "P2104-G"));
            Assert.True(r.Success, Codes(r));
            Assert.DoesNotContain("COBOL5962", Codes(r), StringComparison.Ordinal);
        });
    }

    [Fact]   // REPO-11 (GR6): an imported class whose superclass no entry publishes.
    public void AnImportedClassWhoseBaseNoEntryPublishes_IsRepo11()
    {
        WithDirectory("class-repo11", dir =>
        {
            string lib = Path.Combine(dir, "lib"), sub = Path.Combine(dir, "sub"), app = Path.Combine(dir, "app");
            Assert.True(Compile(lib, "P2104LEDGER", Base("LEDGER", "P2104-Ledger")).Success);
            Assert.True(Compile(sub, "P2104SAV", Savings, repositories: [lib]).Success);
            var r = Compile(app, "P2104C", Client("CLASS SAV AS \"P2104-Savings\"", "SAV"), repositories: [sub]);   // the base's directory is not searched
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5971", StringComparison.Ordinal)
                && e.Contains("its superclass the class \"P2104-Ledger\"", StringComparison.Ordinal));
        });
    }

    [Fact]   // REPO-6: two needed assemblies of one simple name are one module namespace (§2023:4.5, §2023:8.6).
    public void TwoNeededAssembliesOfOneSimpleName_AreRepo6()
    {
        WithDirectory("class-repo6", dir =>
        {
            string one = Path.Combine(dir, "one"), two = Path.Combine(dir, "two"), app = Path.Combine(dir, "app");
            Assert.True(Compile(one, "P2104SAME", Base("P2104ONE", "P2104-One")).Success);
            Assert.True(Compile(two, "P2104SAME", Base("P2104TWO", "P2104-Two")).Success);
            string client = $"""
                IDENTIFICATION DIVISION.
                PROGRAM-ID. P2104C.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    CLASS ONE AS "P2104-One"
                    CLASS TWO AS "P2104-Two".
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 A OBJECT REFERENCE ONE.
                01 B OBJECT REFERENCE TWO.
                PROCEDURE DIVISION.
                    GOBACK.
                END PROGRAM P2104C.
                """;
            var r = Compile(app, "P2104C", client, repositories: [one, two]);
            Assert.False(r.Success);
            Assert.Contains(r.Errors, e => e.Contains("COBOL5976", StringComparison.Ordinal) && e.Contains("Cobol.P2104SAME", StringComparison.Ordinal));
        });
    }
}
