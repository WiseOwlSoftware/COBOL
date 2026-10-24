// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB804 — the NIST differential grades EVERY file a CCVS program writes, not the one string it used to read
/// (<see cref="NistOutputs"/>). These tests are the harness's own evidence that the grading can FAIL
/// (<c>feedback_green_gates_arent_evidence</c>): every way a written file can be wrong — different bytes, a different
/// length, a file with no golden, a golden the program no longer writes — is made red here, and the whole path is
/// falsified on a real program: RW104A, whose second report file <c>xxxxx049.txt</c> hid the PB792 defect behind a
/// byte-identical <c>rw104a.txt</c> while the battery was green.
/// </summary>
public sealed class NistWrittenFilesTests
{
    private static Dictionary<string, byte[]> Files(params (string Name, string Text)[] files) =>
        files.ToDictionary(f => f.Name, f => System.Text.Encoding.UTF8.GetBytes(f.Text));

    /// <summary>A scratch golden directory holding <paramref name="files"/>; the caller deletes it.</summary>
    private static string GoldenDirOf(Dictionary<string, byte[]> files)
    {
        string dir = ScratchDirectory.Create("nistgold");
        foreach (var (name, bytes) in files) File.WriteAllBytes(Path.Combine(dir, name), bytes);
        return dir;
    }

    [Fact]
    public void Grade_IdenticalFiles_HaveNoProblem()
    {
        var written = Files(("xxxxx014.txt", "REC1\nREC2\n"), ("tf021.txt", "AB"));
        string gold = GoldenDirOf(written);
        try { Assert.Empty(NistOutputs.Grade(written, gold)); }
        finally { ScratchDirectory.Remove(gold); }
    }

    [Fact]
    public void Grade_OneByteDifferent_NamesTheFileAndTheOffset()
    {
        string gold = GoldenDirOf(Files(("xxxxx049.txt", "DETAIL LINE  X")));
        try
        {
            var problems = NistOutputs.Grade(Files(("xxxxx049.txt", "DETAIL LINE  Y")), gold);
            string problem = Assert.Single(problems);
            Assert.StartsWith("xxxxx049.txt: differs from its golden at byte 13 ", problem);
        }
        finally { ScratchDirectory.Remove(gold); }
    }

    [Fact]
    public void Grade_ATruncatedOrExtendedFile_IsAProblemAtTheEndOfTheShorter()
    {
        string gold = GoldenDirOf(Files(("tf021.txt", "ABCD")));
        try
        {
            Assert.StartsWith("tf021.txt: differs from its golden at byte 3 ", Assert.Single(NistOutputs.Grade(Files(("tf021.txt", "ABC")), gold)));
            Assert.StartsWith("tf021.txt: differs from its golden at byte 4 ", Assert.Single(NistOutputs.Grade(Files(("tf021.txt", "ABCDE")), gold)));
        }
        finally { ScratchDirectory.Remove(gold); }
    }

    /// <summary>A line sequential file ends its records with the HOST's newline (DOC-A.1-114), so the same program
    /// writes CR LF on Windows and LF on Linux: in a TEXT file that is not a difference, and a lone CR (record data) is.
    /// In a BINARY file the bytes do not depend on the host, so a newline translation is a difference.</summary>
    [Fact]
    public void Grade_HostNewlineInATextFile_IsNotADifference_ButInABinaryFileOrAsALoneCrItIs()
    {
        string gold = GoldenDirOf(Files(("report.txt", "A\nB\n"), ("binary.dat", "A\u0001\nB"), ("lone.txt", "A\rB\n")));
        try
        {
            // report.txt: CR LF against the golden's LF is not a difference. lone.txt: "A\rB\r\n" canonicalizes to
            // "A\rB\n", the golden. binary.dat: the translated newline is the one difference.
            var written = Files(("report.txt", "A\r\nB\r\n"), ("binary.dat", "A\u0001\r\nB"), ("lone.txt", "A\rB\r\n"));
            string problem = Assert.Single(NistOutputs.Grade(written, gold));
            Assert.StartsWith("binary.dat: differs from its golden at byte 2 ", problem);
            // A lone CR is record data, so an LF in its place is a difference.
            Assert.StartsWith("lone.txt: differs", Assert.Single(NistOutputs.Grade(
                Files(("report.txt", "A\nB\n"), ("binary.dat", "A\u0001\nB"), ("lone.txt", "A\nB\n")), gold)));
        }
        finally { ScratchDirectory.Remove(gold); }
    }

    [Fact]
    public void Canonical_TextDropsTheCrOfEachCrLf_AndABinaryFileIsLeftAlone()
    {
        Assert.Equal("A\nB\r"u8.ToArray(), NistOutputs.Canonical("A\r\nB\r"u8.ToArray()));
        Assert.Equal("A\r"u8.ToArray(), NistOutputs.Canonical("A\r"u8.ToArray()));
        byte[] binary = [0x41, 0x00, 0x0D, 0x0A];
        Assert.Same(binary, NistOutputs.Canonical(binary));
        Assert.True(NistOutputs.IsText([]));
        Assert.True(NistOutputs.IsText("TAB\there\f\r\n ~"u8));
        Assert.False(NistOutputs.IsText([0x41, 0x7F]));
        Assert.False(NistOutputs.IsText([0xC3, 0xA9]));
    }

    [Fact]
    public void Grade_AFileWithNoGolden_IsAProblem()
    {
        string gold = GoldenDirOf(Files(("tf021.txt", "AB")));
        try
        {
            var problems = NistOutputs.Grade(Files(("tf021.txt", "AB"), ("xxxxx001.txt", "NEW")), gold);
            string problem = Assert.Single(problems);
            Assert.StartsWith("xxxxx001.txt: the program wrote it (3 bytes) and it has no golden", problem);
        }
        finally { ScratchDirectory.Remove(gold); }
    }

    [Fact]
    public void Grade_AGoldenTheProgramNoLongerWrites_IsAProblem()
    {
        string gold = GoldenDirOf(Files(("tf021.txt", "AB"), ("xxxxx014.txt", "CD")));
        try
        {
            string problem = Assert.Single(NistOutputs.Grade(Files(("tf021.txt", "AB")), gold));
            Assert.StartsWith("xxxxx014.txt: it has a golden", problem);
            Assert.EndsWith("but the program did not write it", problem);
        }
        finally { ScratchDirectory.Remove(gold); }
    }

    [Fact]
    public void Grade_NoGoldenDirectoryAndNothingWritten_IsClean_ButWrittenFilesAreNot()
    {
        string missing = Path.Combine(Path.GetTempPath(), "CobolNet_NoSuchGolden_" + Guid.NewGuid().ToString("N"));
        Assert.Empty(NistOutputs.Grade(Files(), missing));
        Assert.Single(NistOutputs.Grade(Files(("xxxxx014.txt", "A")), missing));
    }

    [Fact]
    public void Written_IsExactlyTheFilesCreatedOrModifiedSinceTheSnapshot()
    {
        string dir = ScratchDirectory.Create("nistwritten");
        try
        {
            // What the harness leaves before the program runs: the compiled program, a chain predecessor's files.
            File.WriteAllText(Path.Combine(dir, "prog.dll"), "dll");
            File.WriteAllText(Path.Combine(dir, "pred.txt"), "the predecessor's report");
            File.WriteAllText(Path.Combine(dir, "tf021.txt"), "before");
            var stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (string f in Directory.EnumerateFiles(dir)) File.SetLastWriteTimeUtc(f, stamp);
            var before = NistOutputs.Snapshot(dir);

            // What the program does: rewrites tf021.txt, creates a second report and its print file.
            File.WriteAllText(Path.Combine(dir, "tf021.txt"), "after");
            File.WriteAllText(Path.Combine(dir, "xxxxx049.txt"), "second report");
            File.WriteAllText(Path.Combine(dir, "prog.txt"), "the print file");

            var written = NistOutputs.Written(dir, before, "prog.txt");
            Assert.Equal(["tf021.txt", "xxxxx049.txt"], written.Keys);
            Assert.Equal("after", System.Text.Encoding.UTF8.GetString(written["tf021.txt"]));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void Capture_ReplacesStaleGoldens_AndAProgramThatWroteNothingLeavesNoDirectory()
    {
        string root = ScratchDirectory.Create("nistcapture");
        try
        {
            string dir = Path.Combine(root, "XX101A");
            NistOutputs.Capture(dir, Files(("a.txt", "1"), ("b.txt", "2")));
            Assert.Equal(["a.txt", "b.txt"], Directory.EnumerateFiles(dir).Select(Path.GetFileName).Order());
            NistOutputs.Capture(dir, Files(("b.txt", "3")));
            Assert.Equal(["b.txt"], Directory.EnumerateFiles(dir).Select(Path.GetFileName));
            Assert.Equal("3", File.ReadAllText(Path.Combine(dir, "b.txt")));
            NistOutputs.Capture(dir, Files());
            Assert.False(Directory.Exists(dir));
        }
        finally { ScratchDirectory.Remove(root); }
    }

    /// <summary>The falsification PB804 asks for, on the real program: RW104A writes its report-writer output to
    /// <c>xxxxx049.txt</c> besides the print file. The control proves the run is graded and clean; then one byte of
    /// that second file is perturbed, and the grading must go red naming it — the change the battery was GREEN over
    /// when PB792 lost the <c>COLUMN 20</c> of a detail line.</summary>
    [Fact]
    public void Rw104a_SecondReportFile_IsGraded_AndOneBytePerturbationIsRed()
    {
        var run = NistRunner.Run("RW104A");
        Assert.True(run.Ok, run.Detail);
        Assert.True(run.Written.ContainsKey("xxxxx049.txt"), "RW104A no longer writes its report-writer file xxxxx049.txt");

        string real = NistOutputs.GoldenDir("RW104A");
        Assert.Empty(NistOutputs.Grade(run.Written, real));                  // the control: graded, and clean

        string copy = ScratchDirectory.Create("nistfalsify");
        try
        {
            foreach (string f in Directory.EnumerateFiles(real)) File.Copy(f, Path.Combine(copy, Path.GetFileName(f)));
            string target = Path.Combine(copy, "xxxxx049.txt");
            byte[] bytes = File.ReadAllBytes(target);
            int at = bytes.Length / 2;
            bytes[at] ^= 0x01;                                                // one bit of one byte of the SECOND file
            File.WriteAllBytes(target, bytes);

            string problem = Assert.Single(NistOutputs.Grade(run.Written, copy));
            Assert.StartsWith($"xxxxx049.txt: differs from its golden at byte {at} ", problem);
        }
        finally { ScratchDirectory.Remove(copy); }
    }

    /// <summary>The console output is graded on the acceptance basis in BOTH arms (guard-verdict.sh compares it with
    /// CRs dropped, per-line trailing spaces stripped and trailing newlines ignored, and a console of only blanks
    /// printed nothing): trailing blanks and blank lines are no difference, a real difference is one, and a blank
    /// console is no written file. The former C# arm compared the bytes and so disagreed with the shell.</summary>
    [Fact]
    public void Stdout_IsGradedOnTheAcceptanceBasis_NotByteForByte()
    {
        Assert.Equal("HELLO\nWORLD", NistOutputs.StdoutBasis("HELLO   \r\nWORLD  \r\n\r\n\n"));
        Assert.Equal("", NistOutputs.StdoutBasis("   \n  \n"));
        Assert.Equal("\nX", NistOutputs.StdoutBasis("\nX\n"));                     // a LEADING blank line is output
        string gold = GoldenDirOf(Files((NistOutputs.StdoutName, "HELLO\nWORLD\n")));
        try
        {
            Assert.Empty(NistOutputs.Grade(Files((NistOutputs.StdoutName, "HELLO   \r\nWORLD\r\n\r\n\r\n")), gold));
            Assert.StartsWith("(stdout): differs from its golden", Assert.Single(NistOutputs.Grade(Files((NistOutputs.StdoutName, "HELLO\nWORLD!\n")), gold)));
            Assert.StartsWith("(stdout): differs from its golden", Assert.Single(NistOutputs.Grade(Files((NistOutputs.StdoutName, "HELLO\n\nWORLD\n")), gold)));
        }
        finally { ScratchDirectory.Remove(gold); }
        // The same trailing blanks in a FILE are a difference: only the console is host-shaped text.
        string fileGold = GoldenDirOf(Files(("tf021.txt", "HELLO\n")));
        try { Assert.Single(NistOutputs.Grade(Files(("tf021.txt", "HELLO  \n")), fileGold)); }
        finally { ScratchDirectory.Remove(fileGold); }

        var written = new Dictionary<string, byte[]>();
        NistOutputs.AddStdout(written, "   \n\n");
        Assert.Empty(written);                                                       // blanks printed nothing: no golden needed
        NistOutputs.AddStdout(written, "HELLO\n");
        Assert.Equal(["(stdout)"], written.Keys);
    }

    /// <summary>The abnormal path is graded as fully as the success path (the owner's PB3353 requirement; review
    /// round 1 of kb/Work PB804): a program a TERMINATES row declares to end its run unit on a fatal I-O status still
    /// leaves files, and they are graded. ST146A OPENs OUTPUT SQ-FS1 (<c>xxxxx014.txt</c>), WRITEs 1000 records and
    /// CLOSEs it; its SORT output procedure OPENs OUTPUT SQ-FS2 (<c>xxxxx015.txt</c>); the X-card dump READs the
    /// closed SQ-FS1 (status 47) and ends the run unit. The control is the real run; then one byte of SQ-FS1's
    /// golden is perturbed and the grading must go red naming it.</summary>
    [Fact]
    public void St146a_FilesLeftAtTheAbnormalEnd_AreGraded_AndOneBytePerturbationIsRed()
    {
        var run = NistRunner.Run("ST146A");
        Assert.False(run.Ok);
        Assert.Contains("EC-I-O-LOGIC-ERROR", run.Detail);
        Assert.Contains("xxxxx014.txt", run.Written.Keys);                           // SQ-FS1, closed before the end
        Assert.Contains("xxxxx015.txt", run.Written.Keys);                           // SQ-FS2, opened by the output procedure
        string real = NistOutputs.GoldenDir("ST146A");
        Assert.Empty(NistOutputs.Grade(run.Written, real));

        string copy = ScratchDirectory.Create("nistst146a");
        try
        {
            foreach (string f in Directory.EnumerateFiles(real)) File.Copy(f, Path.Combine(copy, Path.GetFileName(f)));
            string target = Path.Combine(copy, "xxxxx014.txt");
            byte[] bytes = File.ReadAllBytes(target);
            int at = bytes.Length / 2;
            bytes[at] ^= 0x01;
            File.WriteAllBytes(target, bytes);
            string problem = Assert.Single(NistOutputs.Grade(run.Written, copy));
            Assert.StartsWith($"xxxxx014.txt: differs from its golden at byte {at} ", problem);
        }
        finally { ScratchDirectory.Remove(copy); }
    }

    [Fact]
    public void EveryGoldenDirectory_BelongsToAGreenProgram()
    {
        // A TERMINATES row is a green row too: the files its run leaves at the abnormal end are graded (PB804, review round 1).
        var runs = CorpusManifest.Green().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var strays = Directory.EnumerateDirectories(NistOutputs.GoldenRoot)
            .Select(d => Path.GetFileName(d)!).Where(n => !runs.Contains(n)).Order().ToList();
        Assert.True(strays.Count == 0,
            "tests/nist/valid-files holds goldens for programs the differential does not run "
            + $"(a pending or absent row): {string.Join(", ", strays)}");
        var loose = Directory.EnumerateFiles(NistOutputs.GoldenRoot).Select(Path.GetFileName).Order().ToList();
        Assert.True(loose.Count == 0, $"tests/nist/valid-files holds files outside a program's directory: {string.Join(", ", loose)}");
        var empty = Directory.EnumerateDirectories(NistOutputs.GoldenRoot)
            .Where(d => !Directory.EnumerateFileSystemEntries(d).Any()).Select(d => Path.GetFileName(d)!).Order().ToList();
        Assert.True(empty.Count == 0, $"empty golden directories (a program that writes nothing has none): {string.Join(", ", empty)}");
        var nested = Directory.EnumerateDirectories(NistOutputs.GoldenRoot).SelectMany(Directory.EnumerateDirectories)
            .Select(d => Path.GetRelativePath(NistOutputs.GoldenRoot, d)).Order().ToList();
        Assert.True(nested.Count == 0, $"a program's golden directory holds only files (NistOutputs.Grade reads nothing deeper):{string.Join(", ", nested)}");
    }
}
