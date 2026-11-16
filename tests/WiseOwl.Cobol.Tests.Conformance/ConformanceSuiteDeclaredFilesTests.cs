// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The two files a conformance test declares about its run (DESIGN-conformance-suite.md §3.1, §5; kb/Work PB5755, PB5765):
/// <c>stdin</c>, the file the in-process runner feeds the program as its standard input, and <c>output-file</c>, a file the
/// program leaves in its run directory that the runner compares once the process has ended. PLANTED tests, each over a
/// program of its own in a scratch directory (<see cref="ConformanceSuiteRunner.Judge"/> takes absolute paths as readily as
/// suite-relative ones): no suite test depends on either facility here, and each assertion fails when the facility is
/// missing. <c>run.py</c>'s side is held by <c>run.py --self-test</c> and <c>generate.py --self-test</c>; the verdict
/// comparison of <see cref="BothRunnersVerdictDriftTestsBase{TSlot}"/> holds the two runners together on every suite test that
/// declares either.
/// </summary>
public sealed class ConformanceSuiteDeclaredFilesTests
{
    private const string Edition = "2023";

    /// <summary>A program that ACCEPTs two records and shows each between brackets, so the eight characters of each (the item's
    /// size) and the end-of-input value are visible.</summary>
    private const string EchoProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ECHOIN.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 A PIC X(8).
        01 B PIC X(8).
        PROCEDURE DIVISION.
            ACCEPT A
            ACCEPT B
            DISPLAY "[" A "]"
            DISPLAY "[" B "]"
            STOP RUN.
        """;

    /// <summary>A program that writes two records to a file of its run directory and ends with STOP RUN, never closing it
    /// (§1985:VI.6.24.4 GR2 closes it: the implicit CLOSE is what leaves the records on the file).</summary>
    private const string WriterProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. WRITER.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "R.DAT" ORGANIZATION IS LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 R PIC X(3).
        PROCEDURE DIVISION.
            OPEN OUTPUT F
            MOVE "AAA" TO R
            WRITE R
            MOVE "BBB" TO R
            WRITE R
            STOP RUN.
        """;

    private static SuiteResult Judge(string source, string expectedStdout, string? stdin = null, SuiteOutputFile? outputFile = null,
        string? declaredFile = null, string? declaredContent = null)
    {
        string dir = ScratchDirectory.Create("declared");
        try
        {
            const string id = "planted";
            File.WriteAllText(Path.Combine(dir, id + ".cob"), source + "\n", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(dir, id + ".out"), expectedStdout, new UTF8Encoding(false));
            if (declaredFile is not null) File.WriteAllText(Path.Combine(dir, declaredFile), declaredContent!, new UTF8Encoding(false));
            var test = new SuiteTest(id, "planted", Path.Combine(dir, id + ".cob"), "free", [], [], [], "accept", [],
                new Dictionary<string, string> { [""] = Path.Combine(dir, id + ".out") }, 0, null, false,
                new Dictionary<string, string>(), null, null,
                stdin is null ? null : Path.Combine(dir, stdin),
                outputFile is null ? null : new SuiteOutputFile(outputFile.Name, Path.Combine(dir, outputFile.Expected)));
            return ConformanceSuiteRunner.Judge(Edition, test);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void ADeclaredStandardInput_IsFedToTheRun()
    {
        SuiteResult result = Judge(EchoProgram, "[L1      ]\n[L2      ]\n", stdin: "in.txt", declaredFile: "in.txt", declaredContent: "L1\nL2\n");
        Assert.True(result.Verdict == ConformanceSuiteRunner.Pass, $"{result.Verdict}: {result.Reason}");
    }

    [Fact]
    public void ATestThatDeclaresNoStandardInput_ReadsEndOfInput()
    {
        // The two records the declared-input test reads are what the run shows when the input is closed: blank (the
        // determination for end of input, docs/conformance/7/DOC-A.1-5.md). A runner that fed the declared input to every
        // test, or to none, would pass one of the two tests and fail the other.
        SuiteResult closed = Judge(EchoProgram, "[        ]\n[        ]\n");
        Assert.True(closed.Verdict == ConformanceSuiteRunner.Pass, $"{closed.Verdict}: {closed.Reason}");
        SuiteResult fed = Judge(EchoProgram, "[        ]\n[        ]\n", stdin: "in.txt", declaredFile: "in.txt", declaredContent: "L1\nL2\n");
        Assert.True(fed.Verdict == ConformanceSuiteRunner.Fail && fed.Reason.Contains("[L1      ]"), $"{fed.Verdict}: {fed.Reason}");
    }

    [Fact]
    public void ADeclaredOutputFile_PassesWhenTheProgramLeavesTheExpectedContent()
    {
        SuiteResult result = Judge(WriterProgram, "", outputFile: new SuiteOutputFile("R.DAT", "expected.dat"),
            declaredFile: "expected.dat", declaredContent: "AAA\nBBB\n");
        Assert.True(result.Verdict == ConformanceSuiteRunner.Pass, $"{result.Verdict}: {result.Reason}");
    }

    [Fact]
    public void ADeclaredOutputFile_ThatDiffers_FailsNamingTheFileAndTheLine()
    {
        SuiteResult result = Judge(WriterProgram, "", outputFile: new SuiteOutputFile("R.DAT", "expected.dat"),
            declaredFile: "expected.dat", declaredContent: "AAA\nCCC\n");
        Assert.True(result.Verdict == ConformanceSuiteRunner.Fail && result.Reason.Contains("the output file R.DAT differs from")
                    && result.Reason.Contains("at line 2: expected 'CCC', got 'BBB'"), $"{result.Verdict}: {result.Reason}");
    }

    [Fact]
    public void ADeclaredOutputFile_TheProgramDidNotWrite_FailsNamingTheFile()
    {
        SuiteResult result = Judge(WriterProgram, "", outputFile: new SuiteOutputFile("OTHER.DAT", "expected.dat"),
            declaredFile: "expected.dat", declaredContent: "AAA\nBBB\n");
        Assert.True(result.Verdict == ConformanceSuiteRunner.Fail && result.Reason.Contains("left no output file OTHER.DAT"),
            $"{result.Verdict}: {result.Reason}");
    }

    [Fact]
    public void ADeclaredOutputFile_WithNoExpectedContent_FailsNamingTheExpectedFile()
    {
        SuiteResult result = Judge(WriterProgram, "", outputFile: new SuiteOutputFile("R.DAT", "absent.dat"));
        Assert.True(result.Verdict == ConformanceSuiteRunner.Fail && result.Reason.Contains("absent.dat is absent"),
            $"{result.Verdict}: {result.Reason}");
    }

    [Fact]
    public void ADeclaredOutputFile_DoesNotReplaceTheStandardOutputComparison()
    {
        SuiteResult result = Judge(WriterProgram, "SOMETHING\n", outputFile: new SuiteOutputFile("R.DAT", "expected.dat"),
            declaredFile: "expected.dat", declaredContent: "AAA\nBBB\n");
        Assert.True(result.Verdict == ConformanceSuiteRunner.Fail && result.Reason.Contains("the output differs from"),
            $"{result.Verdict}: {result.Reason}");
    }

    [Fact]
    public void TheManifestCarriesBothDeclaredFiles_AndATestWithNeitherHasNone()
    {
        const string Json = """
            {"editions": {"2023": [
              {"id": "a", "clause": "c", "source": "cases/c/a.cob", "source-format": "free", "files": [], "requires": [], "words": [],
               "expect": "accept", "implementor-defined": [], "expected-output": {"": "cases/c/a.out"}, "in-suite": true, "options": {},
               "stdin": "cases/c/a.in", "output-file": {"name": "R.DAT", "expected": "cases/c/a.R.exp"}},
              {"id": "b", "clause": "c", "source": "cases/c/b.cob", "source-format": "free", "files": [], "requires": [], "words": [],
               "expect": "accept", "implementor-defined": [], "expected-output": {"": "cases/c/b.out"}, "in-suite": true, "options": {}}
            ]}, "diagnostics": {}}
            """;
        WiseOwlManifest manifest = WiseOwlManifest.Parse(Json);
        SuiteTest a = manifest.Editions["2023"]["a"], b = manifest.Editions["2023"]["b"];
        Assert.Equal("cases/c/a.in", a.Stdin);
        Assert.Equal(new SuiteOutputFile("R.DAT", "cases/c/a.R.exp"), a.OutputFile);
        Assert.Null(b.Stdin);
        Assert.Null(b.OutputFile);
    }
}
