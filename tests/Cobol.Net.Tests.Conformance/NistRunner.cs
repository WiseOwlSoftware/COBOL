// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>What one NIST program's run produced: its REPORT (the CCVS print file, or stdout for a DISPLAY-only
/// program) on the acceptance basis, and EVERY OTHER thing the run wrote — see <see cref="NistOutputs"/>.</summary>
/// <param name="Ok">The program compiled and ran to a zero exit.</param>
/// <param name="Report">The report, normalized (<see cref="NistRunner.Normalize"/>).</param>
/// <param name="Detail">Compile/run diagnostics when <paramref name="Ok"/> is false, else the run's stderr.</param>
/// <param name="Written">Every file the program itself wrote besides its report, name → bytes, plus its stdout
/// when it also wrote a print file (<see cref="NistOutputs.StdoutName"/>). When the run EXITED NON-ZERO (the
/// abnormal termination a <c>TERMINATES</c> row declares) there is no report to separate out, so it holds EVERY
/// file the run wrote, the print file among them, and the stdout whenever it printed anything: the files an
/// abnormal end leaves are graded as fully as a normal end's (kb/Work PB804; campaign PB3353).</param>
internal sealed record NistRunResult(bool Ok, string Report, string Detail, IReadOnlyDictionary<string, byte[]> Written);

/// <summary>Compiles a NIST CCVS program (with its X-card preprocessing) and runs it in an isolated temp directory,
/// predecessors of its chain first, collecting EVERYTHING it wrote (kb/Work PB804).</summary>
internal static class NistRunner
{
    /// <summary>Compile and run <paramref name="testName"/>; a program with chain predecessors in
    /// <c>tests/nist/corpus.tsv</c> runs them (in order) in the same directory first, so the consumer's TF### input
    /// files exist — deterministic start-clean, zero cross-test coupling under xunit parallelism.</summary>
    public static NistRunResult Run(string testName)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Nist_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = TestRepo.Nist("programs", testName + ".cob");
            if (!File.Exists(src)) return Failed($"source not found: {src}");
            string dll = Path.Combine(dir, testName + ".dll");

            // Guard parity (scripts/guard.sh:120): the CCVS-85 switch programs (NC174A/NC254A) run with external
            // SWITCH-1 ON, SWITCH-2 unset — their goldens assume exactly that.
            var env = new Dictionary<string, string?> { ["COBOL_SWITCH_1"] = "ON" };

            // Chain predecessors: each producer compiles from its OWN .cob and runs in THIS directory so the
            // consumer's shared TF### input files exist. Chains are self-sufficient in an isolated dir — every
            // chain's first member re-creates its file via OPEN OUTPUT — so the legacy guard's cross-chain
            // ordering constraints do not carry over.
            if (CorpusManifest.Chains.TryGetValue(testName, out var predecessors))
                foreach (string p in predecessors)
                {
                    string pDll = Path.Combine(dir, p + ".dll");
                    var pResult = CompiledProgramCache.Compile(CorpusManifest.CompileOptions(p, pDll));
                    if (!pResult.Success)
                        return Failed($"[chain {p}] compile {pResult.Status}: {string.Join("\n", pResult.Errors)}");
                    string pDat = TestRepo.Nist("data", p + ".dat");
                    var (pOk, _, pDetail) = CutRunner.Run(pDll, dir, File.Exists(pDat) ? pDat : null, env);
                    if (!pOk) return Failed($"[chain {p}] run exit non-zero: {pDetail}");
                }

            var result = CompiledProgramCache.Compile(CorpusManifest.CompileOptions(testName, dll));
            if (!result.Success)
                return Failed($"[compile] {result.Status}: {string.Join("\n", result.Errors)}");

            // ⛔ WHAT THE PROGRAM WROTE IS WHAT CHANGED SINCE HERE (kb/Work PB804). The snapshot is taken after every
            // compile and every predecessor, so the compiler's artefacts and a predecessor's files are never
            // mistaken for this program's output — and a shared TF### file the program REWRITES still is its.
            var before = NistOutputs.Snapshot(dir);
            string dat = TestRepo.Nist("data", testName + ".dat");
            var (runOk, stdout, runDetail) = CutRunner.Run(dll, dir, File.Exists(dat) ? dat : null, env);
            if (!runOk)
            {
                // ⛔ A RUN THAT ENDED ABNORMALLY STILL LEFT FILES (kb/Work PB804; the owner's PB3353 requirement that the
                // exception paths are covered as fully as the success paths). A TERMINATES row asserts the run unit's
                // end, and what the program had written by then — the closed files, the print file, the console output —
                // is what a regression that loses or corrupts a record at that end would change.
                var left = NistOutputs.Written(dir, before, reportName: null);
                NistOutputs.AddStdout(left, stdout);
                return new NistRunResult(false, "", $"[run] exit non-zero: {runDetail}", left);
            }

            // The CCVS report lands in the print file (assign target → <lowercased>.txt in the run dir); a
            // DISPLAY-only program produces no print file and its report is stdout. That is the WHOLE discovery
            // rule: the former "first *.txt in the directory" fallback guessed which file was the report, and the
            // answer is now never a guess — every file the program wrote is graded (NistOutputs.Grade).
            string reportName = testName.ToLowerInvariant() + ".txt";
            string printFile = Path.Combine(dir, reportName);
            bool hasPrintFile = File.Exists(printFile);
            string report = hasPrintFile ? File.ReadAllText(printFile) : stdout;

            var written = NistOutputs.Written(dir, before, reportName);
            if (hasPrintFile) NistOutputs.AddStdout(written, stdout);
            return new NistRunResult(true, Normalize(report), runDetail, written);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    private static NistRunResult Failed(string detail) =>
        new(false, "", detail, new Dictionary<string, byte[]>());

    /// <summary>The NIST acceptance basis (exactly <c>scripts/guard.sh</c>'s <c>normalize()</c>): drop CR, strip
    /// per-line trailing spaces, and mask the COMPUTED= operand (a value some CCVS programs print that is not part of
    /// the pass/fail decision). Applied identically to the golden and the produced output.
    /// <para>⛔ A LONE CR IS DROPPED, NOT TURNED INTO A LINE BREAK. <c>ReplaceLineEndings("\n")</c> — what this used
    /// to call — maps a bare CR to LF, which silently ignored the basis above: a WRITE … ADVANCING 0 is an OVERPRINT
    /// written as a carriage return (docs/CONFORMANCE.md "ADVANCING 0"; kb/Work PB1667), and the NIST print programs
    /// (SQ101M, SQ207M) check that the overprinted line follows its marker on the same line. The basis exists to
    /// compare listing TEXT, and a carriage return is not text.</para></summary>
    public static string Normalize(string s)
    {
        var lines = s.Replace("\r\n", "\n").Replace("\r", "").ReplaceLineEndings("\n").Split('\n')
            .Select(line => System.Text.RegularExpressions.Regex.Replace(line.TrimEnd(' '), "COMPUTED=  [0-9]*", "COMPUTED=  XXXXXXXXX"));
        return string.Join("\n", lines).TrimEnd('\n');
    }
}

/// <summary>
/// ⛔ THE EVIDENCE BASE OF THE NIST DIFFERENTIAL IS EVERYTHING A PROGRAM WRITES, NOT ONE FILE OF IT (kb/Work PB804).
/// <para>The harness used to read exactly ONE string per program (the print file, else the first <c>*.txt</c>, else
/// stdout), normalize it and delete the run directory, so a CCVS program that writes a SECOND file — RW104A's report
/// to <c>xxxxx049.txt</c>, a SORT/MERGE or sequential/relative/indexed file's contents, the console output beside a
/// print file — had that file neither read nor kept. PB792 hid for a battery cycle behind exactly that: RW104A lost a
/// report field's COLUMN under <c>--permissive</c> while <c>rw104a.txt</c> stayed byte-identical to its golden.</para>
/// <para>Now the files a run WROTE (created or modified since just before the program ran — the compiler's artefacts
/// and a chain predecessor's files predate that point) are each compared BYTE-FOR-BYTE (<see cref="Canonical"/>: a
/// text file's host newline aside) with <c>tests/nist/valid-files/&lt;TEST&gt;/&lt;file&gt;</c>, and the two sets must
/// be EQUAL: a file written with no golden is red (capture it), and a golden the program no longer writes is red.
/// The next program that writes a second file is graded with no change here. <c>scripts/guard-verdict.sh</c>
/// (<c>guard_files_apply</c>) is the shell guards' arm of the same rule, so CI's NIST leg grades the same files.</para>
/// <para>The expected bytes are the program's own output — the size and layout of records on the storage medium
/// are the implementor's (ISO §13.18.43.4 GR2: "The size of records on physical storage media may be different due
/// to control information required by the operating environment"), so these goldens pin WiseOwl COBOL's behavior,
/// not the standard's: a deliberate format change re-captures them
/// (<see cref="UpdateVariable"/>=1, then review <c>git diff tests/nist/valid-files</c>). The standard-defined
/// content of those files — what each record holds — is what the CCVS program itself reads back and reports in its
/// print file, which stays the authority on correctness.</para>
/// </summary>
internal static class NistOutputs
{
    /// <summary>The name a program's console output carries among its written files, when it ALSO wrote a print
    /// file (a DISPLAY-only program's stdout is its report instead). Parentheses cannot begin a file name a COBOL
    /// ASSIGN produces.</summary>
    internal const string StdoutName = "(stdout)";

    /// <summary>Set to <c>1</c> to re-capture the goldens instead of grading against them
    /// (<c>COBOLNET_UPDATE_NIST_FILES=1 dotnet test … --filter NistDifferentialTests</c>), exactly as
    /// <c>COBOLNET_UPDATE_SNAPSHOTS</c> does for the characterization snapshots.</summary>
    internal const string UpdateVariable = "COBOLNET_UPDATE_NIST_FILES";

    internal static bool UpdateRequested => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

    /// <summary>The goldens' root: one directory per program, one file per written file.</summary>
    internal static string GoldenRoot => TestRepo.Nist("valid-files");

    /// <summary>The directory holding <paramref name="testName"/>'s written-file goldens.</summary>
    internal static string GoldenDir(string testName) => Path.Combine(GoldenRoot, testName);

    /// <summary>name → last-write time of every file in <paramref name="dir"/> now.</summary>
    internal static IReadOnlyDictionary<string, long> Snapshot(string dir) =>
        new DirectoryInfo(dir).EnumerateFiles()
            .ToDictionary(f => f.Name, f => f.LastWriteTimeUtc.Ticks, StringComparer.Ordinal);

    /// <summary>The files <paramref name="dir"/> holds that were created or modified since
    /// <paramref name="before"/>, except the program's report file <paramref name="reportName"/> (graded as the
    /// report; <see langword="null"/> when the run has no report to separate out, as after an abnormal end):
    /// name → bytes.</summary>
    internal static SortedDictionary<string, byte[]> Written(string dir, IReadOnlyDictionary<string, long> before, string? reportName)
    {
        var written = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var f in new DirectoryInfo(dir).EnumerateFiles())
        {
            if (reportName is not null && string.Equals(f.Name, reportName, StringComparison.Ordinal)) continue;
            if (before.TryGetValue(f.Name, out long ticks) && ticks == f.LastWriteTimeUtc.Ticks) continue;
            written[f.Name] = File.ReadAllBytes(f.FullName);
        }
        return written;
    }

    /// <summary>A console's text on the acceptance basis, exactly <c>guard-verdict.sh</c>'s
    /// <c>tr -d '\r' | sed 's/ *$//'</c> inside <c>$(...)</c>: carriage returns dropped, each line's trailing spaces
    /// stripped, trailing newlines dropped. A console's line ends are the HOST's, and a stdout of only blanks and
    /// newlines printed nothing (it is no written file and needs no golden). One rule in both arms: the C# arm used
    /// to compare the console byte-for-byte in <see cref="Canonical"/>'s form and so disagreed with the shell
    /// (kb/Work PB804, review round 1).</summary>
    internal static string StdoutBasis(string stdout) =>
        string.Join('\n', stdout.Replace("\r", "").Split('\n').Select(line => line.TrimEnd(' '))).TrimEnd('\n');

    /// <summary>Add the program's console output to <paramref name="written"/> as <see cref="StdoutName"/> when it
    /// printed anything on the acceptance basis (<see cref="StdoutBasis"/>). The caller decides whether the console
    /// is a file to grade: beside a print file it is, and after an abnormal end (no report) it is; a DISPLAY-only
    /// program's console output IS its report and never comes through here.</summary>
    internal static void AddStdout(IDictionary<string, byte[]> written, string stdout)
    {
        if (StdoutBasis(stdout).Length > 0) written[StdoutName] = Encoding.UTF8.GetBytes(stdout);
    }

    /// <summary>True for a file that holds only TEXT: horizontal tab, line feed, form feed, carriage return and the
    /// printable ASCII characters. Anything else (a NUL, a binary field, a keyed store's header) is a binary file.</summary>
    internal static bool IsText(ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
            if (b is not (9 or 10 or 12 or 13) && (b < 32 || b > 126)) return false;
        return true;
    }

    /// <summary>The form two files are compared in. A line sequential file ends each record with the HOST's newline
    /// — CR LF on Windows, LF on Linux (docs/CONFORMANCE.md DOC-A.1-114; ISO §12.4.5.10.3 GR2: "an implementor-defined
    /// line delimiter") — so a TEXT file is compared with each CR LF taken as LF, and the goldens hold that form
    /// (they read the same on both hosts). A CR not followed by LF is record data and stays. A binary file is
    /// compared as written: a record file's bytes do not depend on the host, so a newline translation there is a
    /// defect this must see. <c>scripts/guard-verdict.sh</c> (<c>guard_canonical_into</c>) is the same rule.</summary>
    internal static byte[] Canonical(byte[] bytes)
    {
        if (!IsText(bytes) || Array.IndexOf(bytes, (byte)'\r') < 0) return bytes;
        var canonical = new List<byte>(bytes.Length);
        for (int i = 0; i < bytes.Length; i++)
            if (!(bytes[i] == '\r' && i + 1 < bytes.Length && bytes[i + 1] == '\n')) canonical.Add(bytes[i]);
        return canonical.ToArray();
    }

    /// <summary>The form the file called <paramref name="name"/> is compared in: the console output
    /// (<see cref="StdoutName"/>) on the acceptance basis (<see cref="StdoutBasis"/>, as the shell arm compares it),
    /// every other file in its <see cref="Canonical"/> form.</summary>
    internal static byte[] Comparable(string name, byte[] bytes) =>
        name == StdoutName ? Encoding.UTF8.GetBytes(StdoutBasis(Encoding.UTF8.GetString(bytes))) : Canonical(bytes);

    /// <summary>Compare what a program wrote with its goldens in <paramref name="goldenDir"/>; every problem, empty
    /// when the two sets are equal and every file is identical in its <see cref="Canonical"/> form.</summary>
    internal static IReadOnlyList<string> Grade(IReadOnlyDictionary<string, byte[]> written, string goldenDir)
    {
        var problems = new List<string>();
        var goldens = Directory.Exists(goldenDir)
            ? Directory.EnumerateFiles(goldenDir).Select(p => Path.GetFileName(p)!).ToHashSet(StringComparer.Ordinal)
            : [];
        foreach (var (name, got) in written.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (!goldens.Contains(name))
            {
                problems.Add($"{name}: the program wrote it ({got.Length} bytes) and it has no golden in {goldenDir} — " +
                             $"review it and capture it ({UpdateVariable}=1)");
                continue;
            }
            byte[] want = Comparable(name, File.ReadAllBytes(Path.Combine(goldenDir, name)));
            byte[] have = Comparable(name, got);
            int at = FirstDifference(want, have);
            if (at >= 0)
                problems.Add($"{name}: differs from its golden at byte {at} (golden {want.Length} bytes, written {have.Length} bytes)");
        }
        foreach (string name in goldens.Order(StringComparer.Ordinal))
            if (!written.ContainsKey(name))
                problems.Add($"{name}: it has a golden in {goldenDir} but the program did not write it");
        return problems;
    }

    /// <summary>The offset of the first byte at which the two differ (the shorter's length when one is a prefix of the
    /// other), or −1 when they are identical.</summary>
    internal static int FirstDifference(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++)
            if (a[i] != b[i]) return i;
        return a.Length == b.Length ? -1 : n;
    }

    /// <summary>Replace the goldens in <paramref name="dir"/> with <paramref name="written"/> (the update mode):
    /// stale goldens go, a program that wrote nothing leaves no directory.</summary>
    internal static void Capture(string dir, IReadOnlyDictionary<string, byte[]> written)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        if (written.Count == 0) return;
        Directory.CreateDirectory(dir);
        foreach (var (name, bytes) in written) File.WriteAllBytes(Path.Combine(dir, name), Canonical(bytes));
    }
}
