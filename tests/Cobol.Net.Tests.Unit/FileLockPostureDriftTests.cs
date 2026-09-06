// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §9.1.15 ADDRESSES TWO AUDIENCES WITH ONE SHARING MODE, AND THE OPERATING ENVIRONMENT'S SHARE MODE CAN
/// ONLY SERVE THE SECOND (kb/Work PB740).
///
/// <para><b>Inside the run unit</b> — <i>"Multiple paths of access may exist in the same runtime element,
/// contained elements, separate runtime elements within the same run unit, or runtime elements in different run
/// units"</i> — the gate is named exactly: <i>"Before access to a shared physical file is allowed through an
/// OPEN statement, the sharing mode and the open mode of that OPEN statement shall be allowed by all other file
/// connectors that are currently associated with the physical file, as described in … Table 19"</i>. So a pair
/// <c>FileRegistry.Conflicts</c> PERMITS shall open, and the host handle may not veto it.</para>
///
/// <para><b>Outside it</b> — <i>"The successful opening of a file establishes a file lock for the applicable
/// sharing rules, thereby preventing other run units from opening that file with incompatible sharing
/// rules"</i> — the host share mode IS that lock, and §9.1.15's three rules say what it denies.</para>
///
/// <para><b>What PB740 was.</b> One boolean — "did this SELECT write a SHARING or LOCK MODE clause?" — was spent
/// on both questions, and answered each backwards. Two clause-less connectors on one ASSIGN that Table 19
/// permits to share (<c>OPEN INPUT F-A</c> then <c>OPEN EXTEND F-B</c>) were refused by the host and the second
/// OPEN answered '30' — a status no Table 19 row and no §9.1.13.9 item produces — and the appended record was
/// never written. Its mirror, measured across processes: a connector opened <c>SHARING WITH NO OTHER</c>, the
/// mode §9.1.15 1) calls <i>exclusive access</i>, took <see cref="FileShare.ReadWrite"/>, so an external process
/// appended four bytes to a file the program held open, while a connector with NO clause refused the same
/// write.</para>
///
/// <para>The guard is therefore the WHOLE agreement, not the reported cell: every (sharing mode × open mode)
/// pair on both sides, measured through real connectors on a real physical file, against
/// <c>FileRegistry.Conflicts</c>' own verdict — because the reported pair (INPUT + EXTEND) is one cell of a
/// disagreement the two layers can have anywhere, and the golden that had certified Table 19 could not see it
/// (both of its clause-less legs held the ACCESS axis fixed).</para>
///
/// <para>⛔ AND WHAT THE OPERATING ENVIRONMENT CAN EXPRESS IS MEASURED, NOT ASSUMED (kb/Work PB795). The file
/// lock is only ever as strong as the host's share modes, and .NET reaches them on Unix through an advisory
/// <c>flock</c> with two states: <see cref="FileShare.None"/> is <c>LOCK_EX</c> and every other value is
/// <c>LOCK_SH</c>. §9.1.15 2)'s <i>"restricts concurrent access … to input mode"</i> therefore has no
/// expression on such a host at all, and asserting it there was two days of red CI that named nothing.
/// <see cref="ExpectedOutsideWriter"/> translates the standard's answer through
/// <see cref="HostCapability.Sharing"/>'s MEASUREMENT, <see cref="TheHostsShareModeSemanticsAreTheDocumentedOnes"/>
/// pins the measurement itself against the determination in <c>DESIGN-runtime-library.md</c>, and the bounded
/// widening is asserted as a DELTA so that it measures the widening rather than the host.</para>
/// </summary>
public sealed class FileLockPostureDriftTests
{
    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb740-{tag}-{Guid.NewGuid():N}.dat");

    private static void TryDelete(string host)
    {
        foreach (string p in new[] { host, host + ".cbattr" })
            try { File.Delete(p); } catch (IOException) { }
    }

    /// <summary>The four spellings a connector's §9.1.15 sharing mode can have at the registry. The first is the
    /// undetermined implementor default (<c>FileRegistry.ImplementorDefaultSharing</c>, kb/Work PB322) reached
    /// the way a program reaches it — by writing no clause at all.</summary>
    public enum Mode { ClauseLess, NoOther, ReadOnly, AllOther }

    private static FileSharing? SharingOf(Mode m) => m switch
    {
        Mode.NoOther => FileSharing.NoOther,
        Mode.ReadOnly => FileSharing.ReadOnly,
        Mode.AllOther => FileSharing.AllOther,
        _ => FileRegistry.ImplementorDefaultSharing,
    };

    public static TheoryData<Mode, FileOpenMode, Mode, FileOpenMode> EveryPair()
    {
        var data = new TheoryData<Mode, FileOpenMode, Mode, FileOpenMode>();
        foreach (var sa in Enum.GetValues<Mode>())
            foreach (var ma in Enum.GetValues<FileOpenMode>())
                foreach (var sb in Enum.GetValues<Mode>())
                    foreach (var mb in Enum.GetValues<FileOpenMode>())
                        data.Add(sa, ma, sb, mb);
        return data;
    }

    private static void RegisterAndShare(FileRegistry reg, string name, string host, Mode m)
    {
        reg.Register(name, host, recordWidth: 4, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
        // ClauseLess registers NOTHING: a SELECT with neither clause never reaches RegisterSharing, which is
        // exactly the shape PB740 measured. The other three record their declared sharing mode.
        if (m != Mode.ClauseLess) reg.RegisterSharing(name, SharingOf(m), FileLockMode.Manual, multiple: false);
    }

    // ── The agreement, measured on real handles ─────────────────────────────────────────────────────────────

    /// <summary>⛔ THE REGRESSION, AS A THEOREM RATHER THAN A CASE. For EVERY (sharing mode × open mode) on the
    /// existing side crossed with every one on the incoming side, two connectors open one physical file through
    /// a real registry and the outcome shall be the arbiter's: a pair <c>Conflicts</c> permits gets a
    /// success-family status (§9.1.13.2), and a pair it refuses gets '61' (§9.1.13.9 item 1). '30' — the host
    /// refusing what the standard allowed — cannot appear anywhere in the matrix.</summary>
    [Theory]
    [MemberData(nameof(EveryPair))]
    public void TheHostHandleNeverVetoesWhatTable19Permits(Mode sa, FileOpenMode ma, Mode sb, FileOpenMode mb)
    {
        string host = Tmp($"{sa}{ma}-{sb}{mb}");
        try
        {
            var reg = new FileRegistry();
            reg.Register("S", host, 4, false, false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Write("S", "SEED", -1, page: null);
            reg.Close("S");

            RegisterAndShare(reg, "A", host, sa);
            RegisterAndShare(reg, "B", host, sb);

            reg.OpenStatic("A", ma);
            Assert.True(reg.Status("A")[0] == '0',
                $"The FIRST open, alone on the physical file, answered '{reg.Status("A")}' — the matrix measures "
                + "the SECOND open and cannot say anything if the first one did not happen.");

            reg.OpenStatic("B", mb);
            bool refused = FileRegistry.Conflicts((SharingOf(sa), ma), (SharingOf(sb), mb));
            string got = reg.Status("B");

            if (refused)
                Assert.True(got == FileStatusCode.FileSharingConflict,
                    $"Table 19 refuses ({sa} {ma}) × ({sb} {mb}), so §9.1.13.9 item 1's '61' is the only answer; "
                    + $"got '{got}'.");
            else
                Assert.True(got[0] == '0',
                    $"Table 19 PERMITS ({sa} {ma}) × ({sb} {mb}) — §9.1.15 puts the gate on the file connectors "
                    + $"and Table 19, not on the operating environment's handle — but the OPEN answered '{got}'. "
                    + "'30' here is the host vetoing an open the standard allowed (kb/Work PB740).");

            reg.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>The pair PB740 reported, end to end and with its DATA: two clause-less connectors on one ASSIGN,
    /// <c>OPEN INPUT</c> then <c>OPEN EXTEND</c>, the appended record reaching the physical file (§14.9.51.4
    /// GR12 <i>"releases a logical record to the operating environment"</i>, GR18 the successor relationship)
    /// — and the READER's file position indicator surviving the widening its sibling forced, which is the half
    /// a status-only assertion cannot see: the reposture rebuilds the handle, and a rebuild at the wrong offset
    /// re-serves a record the program has already read (§9.1.12).</summary>
    [Fact]
    public void ClauseLessInputThenExtend_Appends_AndTheReaderKeepsItsPosition()
    {
        string host = Tmp("input-extend");
        try
        {
            var reg = new FileRegistry();
            reg.Register("S", host, 4, false, false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Write("S", "AAAA", -1, page: null);
            reg.Write("S", "BBBB", -1, page: null);
            reg.Close("S");

            reg.Register("A", host, 4, false, false, -1, -1);
            reg.Register("B", host, 4, false, false, -1, -1);

            reg.OpenStatic("A", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.Status("A"));
            Assert.Equal(FileStatusCode.Success,
                reg.ReadShared("A", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string r1));
            Assert.Equal("AAAA", r1);

            // The OPEN that used to answer '30'. It widens A's handle, which is A's handle being REBUILT.
            reg.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.Status("B"));
            reg.Write("B", "CCCC", -1, page: null);
            Assert.Equal(FileStatusCode.Success, reg.Status("B"));

            // A's file position indicator is where its own READ left it — not rewound by the rebuild.
            Assert.Equal(FileStatusCode.Success,
                reg.ReadShared("A", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string r2));
            Assert.Equal("BBBB", r2);

            reg.CloseAll();
            Assert.Equal("AAAABBBBCCCC", File.ReadAllText(host));
        }
        finally { TryDelete(host); }
    }

    // ── The file lock against everything that is not this run unit ──────────────────────────────────────────

    /// <summary>⛔ THE MIRROR, and it needs no second process: a host share mode names no requester, so a handle
    /// opened by this test is exactly as foreign to the connector's handle as one opened by another program.
    /// §9.1.15 1) — <i>"The sharing with no other mode specifies exclusive access to a physical file"</i> — with
    /// two control arms, because the defect was a RELATIVE inversion and an absolute assertion about one mode
    /// cannot see one: the clause-less arm is the posture PB740 left untouched (an open owner question), and
    /// the ALL OTHER arm is §9.1.15 3)'s <i>"allows concurrent access"</i>. Before the fix the three arms read
    /// (refused, refused, refused) with NO OTHER the most permissive of them at the OS.
    /// <para>⚠ THE ARMS ARE THE STANDARD'S; WHAT THE HOST CAN MAKE OF THEM IS MEASURED (kb/Work PB795). The
    /// clause-less arm asserted that a <see cref="FileShare.Read"/> file lock refuses an outside WRITER, and on
    /// a host whose sharing is one advisory lock with two states that refusal cannot exist —
    /// <see cref="ExpectedOutsideWriter"/> is where the translation happens, and it degrades exactly one rule
    /// on exactly the hosts that measurably cannot express it.</para></summary>
    [Theory]
    [InlineData(Mode.NoOther, false, false)]     // §9.1.15 1) exclusive — nothing else may read or write
    [InlineData(Mode.ClauseLess, true, false)]   // the undetermined default, unchanged by PB740 (owner question)
    [InlineData(Mode.AllOther, true, true)]      // §9.1.15 3) concurrent access through other connectors
    public void TheFileLockIsTheSharingMode_MeasuredFromOutsideTheConnector(
        Mode mode, bool outsiderMayRead, bool outsiderMayWrite)
    {
        string host = Tmp($"lock-{mode}");
        try
        {
            var reg = new FileRegistry();
            reg.Register("S", host, 4, false, false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Write("S", "SEED", -1, page: null);
            reg.Close("S");

            RegisterAndShare(reg, "F", host, mode);
            reg.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.Status("F"));

            (bool expectWrite, string why) = ExpectedOutsideWriter(mode, outsiderMayWrite);
            Assert.Equal(outsiderMayRead, HostCapability.OutsiderCan(host, FileAccess.Read));
            Assert.True(expectWrite == HostCapability.OutsiderCan(host, FileAccess.Write),
                $"({mode}) an outside writer: expected {expectWrite}. {why}");

            // §9.1.15: "The file lock is removed by an explicit or implicit CLOSE statement executed for that
            // file connector" — so after the CLOSE every arm reads the same.
            reg.Close("F");
            Assert.True(HostCapability.OutsiderCan(host, FileAccess.Write));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ ISO §9.1.15'S ANSWER, TRANSLATED THROUGH WHAT THIS HOST'S SHARE MODES CAN ACTUALLY EXPRESS —
    /// and the translation is a MEASUREMENT (<see cref="HostCapability.Sharing"/>), never an
    /// <c>OSPlatform</c> literal, so a host that gains or loses the ability moves this and turns
    /// <see cref="TheHostsShareModeSemanticsAreTheDocumentedOnes"/> red rather than silently re-classifying
    /// every arm (kb/Work PB795).
    /// <para><b>The determination, and it belongs to the RUNTIME, not to this test</b>
    /// (<c>DESIGN-runtime-library.md</c>). .NET implements <see cref="FileShare"/> on Unix with an advisory
    /// <c>flock</c> that has two states — <see cref="FileShare.None"/> takes <c>LOCK_EX</c> and every other
    /// value takes <c>LOCK_SH</c> — so §9.1.15 2)'s <i>"restricts concurrent access … to input mode"</i> has no
    /// expression there at all. The posture is NOT changed to compensate: mapping it onto
    /// <see cref="FileShare.None"/> would refuse the reader rule 2 explicitly ADMITS, trading an under-refusal
    /// for an over-refusal of access the standard grants. So on such a host rule 2's file lock degrades toward
    /// rule 3's <i>"allows concurrent access"</i> and never toward rule 1's exclusivity — a user on Linux gets
    /// weaker protection from other run units than on Windows, and this is where that is written down instead
    /// of being a red test nobody could explain.</para>
    /// <para>Rule 1 survives everywhere, because <c>LOCK_EX</c> is exactly the one thing a binary advisory lock
    /// CAN say — subject to it being advisory, which binds only processes that also take the lock.</para></summary>
    private static (bool Expected, string Why) ExpectedOutsideWriter(Mode mode, bool isoAnswer)
    {
        var host = HostCapability.Sharing;
        if (host.SeparatesReadersFromWriters)
            return (isoAnswer,
                "This host's share modes are access-discriminating, so §9.1.15 is enforced as written. "
                + host.Because);
        if (FileLockPosture.OfSharingMode(SharingOf(mode)) == FileShare.None)
            return (isoAnswer,
                "§9.1.15 1)'s exclusive access is the one rule a binary advisory lock can still express, so "
                + "this arm is unchanged on this host. " + host.Because);
        return (true,
            "⚠ MEASURED HOST LIMIT, NOT A DEFECT IN THE POSTURE: this host cannot express a share mode that "
            + "admits a reader and refuses a writer, so §9.1.15 2)'s file lock degrades toward rule 3's "
            + "concurrent access rather than toward rule 1's exclusivity (widening it to FileShare.None would "
            + "refuse the reader rule 2 admits). The determination is in DESIGN-runtime-library.md; kb/Work "
            + "PB795 is why it is written down. " + host.Because);
    }

    /// <summary>⛔ THE HOST CAPABILITY EVERY ARM ABOVE LEANS ON, ASSERTED RATHER THAN ASSUMED (kb/Work PB795).
    /// Three of these are properties EVERY host this compiler supports must have, and the fourth is the
    /// documented split between them. Without this test the translation in
    /// <see cref="ExpectedOutsideWriter"/> could quietly answer "this host enforces nothing" and every arm
    /// above would pass by expecting nothing — the exact shape of a green test holding a gap open.</summary>
    [Fact]
    public void TheHostsShareModeSemanticsAreTheDocumentedOnes()
    {
        var host = HostCapability.Sharing;
        Assert.True(host.EnforcesExclusiveAccess,
            "§9.1.15 1) — a FileShare.None handle shall exclude an outside reader AND an outside writer. This "
            + "host excludes neither or only one, so nothing above can measure a file lock at all. " + host.Because);
        Assert.True(host.ReadShareAdmitsAnOutsideReader,
            "§9.1.15 2) restricts other access *to input mode*; it does not exclude it. " + host.Because);
        Assert.True(host.ReadWriteShareAdmitsAnOutsideWriter,
            "§9.1.15 3) — *allows concurrent access*. " + host.Because);
        Assert.True(OperatingSystem.IsWindows() == host.SeparatesReadersFromWriters,
            "THE DOCUMENTED DETERMINATION (DESIGN-runtime-library.md, kb/Work PB795): Windows share modes are "
            + "mandatory and per-access, so §9.1.15 2) is enforced as written there; every other host reaches "
            + "FileShare through .NET's advisory flock, which has one shared state for every mode but None and "
            + "therefore cannot express rule 2. If this assertion moved, the runtime's own guarantee moved with "
            + "it and the design doc has to be re-derived — never this line adjusted. " + host.Because);
    }

    /// <summary>The widening is BOUNDED, in both directions — the fact that lets PB740 fix the in-run-unit half
    /// without spending the clause-less connector's protection, which is the owner's question and not this
    /// note's. A clause-less reader alone denies an outside writer; a clause-less appender joins it, which
    /// rebuilds the reader's handle permissively — and the outside writer is STILL denied, because the host
    /// checks a new handle against EVERY outstanding one and the appender's own posture still refuses it. When
    /// the appender closes, the reader narrows back.</summary>
    [Fact]
    public void WideningForASiblingDoesNotAdmitAnOutsider_AndIsGivenBackAtTheClose()
    {
        string host = Tmp("bounded");
        try
        {
            var reg = new FileRegistry();
            reg.Register("S", host, 4, false, false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Write("S", "SEED", -1, page: null);
            reg.Close("S");

            reg.Register("A", host, 4, false, false, -1, -1);
            reg.Register("B", host, 4, false, false, -1, -1);

            reg.OpenStatic("A", FileOpenMode.Input);
            (bool alone, string why) = ExpectedOutsideWriter(Mode.ClauseLess, isoAnswer: false);
            Assert.True(alone == HostCapability.OutsiderCan(host, FileAccess.Write),
                $"A clause-less connector alone: an outside writer, expected {alone}. {why}");

            reg.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.Status("B"));
            Assert.True(alone == HostCapability.OutsiderCan(host, FileAccess.Write),
                "⛔ THE INVARIANT, AND IT IS A DELTA: the reader was widened so its sibling appender could open, "
                + "and that shall admit an outsider NOTHING it did not already have — the host checks a new "
                + "handle against every outstanding one, so the run unit's effective lock stays the "
                + "INTERSECTION of its connectors' postures. Asserted as \"unchanged by the widening\" rather "
                + "than as \"refused\", because on a host that cannot express a reader-only file lock the "
                + "outsider was never refused to begin with and an absolute assertion would measure the host "
                + "instead of the widening (kb/Work PB795). " + why);
            Assert.True(HostCapability.OutsiderCan(host, FileAccess.Read), "Neither posture denies a reader.");

            reg.Close("B");
            Assert.True(alone == HostCapability.OutsiderCan(host, FileAccess.Write),
                "The set shrank back to one clause-less reader, so its file lock shall narrow back to exactly "
                + "what that connector alone establishes. " + why);
            reg.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>The OTHER sentence each of §9.1.15's three rules ends with, which the file lock and Table 19
    /// between them do not cover: <i>"Record locks are ignored"</i> (rule 1, exclusive access — there is no
    /// other connector for a lock to exclude), against <i>"Record locks are in effect"</i> (rules 2 and 3).
    /// Measured where a lock is observable at all — through a sibling connector's view of the lock table — so
    /// the three rules are asserted whole rather than in the half the OS handle happens to show.</summary>
    [Theory]
    [InlineData(Mode.NoOther, false)]    // §9.1.15 1) "Record locks are ignored."
    [InlineData(Mode.ReadOnly, true)]    // §9.1.15 2) "Record locks are in effect."
    [InlineData(Mode.AllOther, true)]    // §9.1.15 3) "Record locks are in effect."
    public void TheSharingModeAlsoDecidesWhetherRecordLocksAreInEffect(Mode mode, bool locksInEffect)
    {
        string host = Tmp($"locks-{mode}");
        try
        {
            var reg = new FileRegistry();
            reg.Register("S", host, 4, false, false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Write("S", "SEED", -1, page: null);
            reg.Close("S");

            reg.Register("F", host, 4, false, false, -1, -1);
            reg.RegisterSharing("F", SharingOf(mode), FileLockMode.Manual, multiple: false);
            reg.Register("Q", host, 4, false, false, -1, -1);   // the observer; never opened, so Table 19 is silent

            reg.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.Status("F"));
            Assert.Equal(FileStatusCode.Success,
                reg.ReadShared("F", false, FileRecordLock.WithLock, false, false, FileRetryKind.None, 0, out _));

            Assert.Equal(locksInEffect, reg.IsLockedByOther("Q", "1"));
            reg.Close("F");
        }
        finally { TryDelete(host); }
    }

    // ── The structural half ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE POSTURE IS DERIVED IN ONE PLACE. Every <c>FileShare</c> value under
    /// <c>Cobol.Net.Runtime/IO</c> lives in <c>FileLockPosture</c> (which derives it) or <c>FileSupport</c>
    /// (whose bookkeeping role has a fixed one) — anywhere else is a call site deciding §9.1.15 for itself,
    /// which is the shape that produced PB740's two opposite wrong answers from one boolean.</summary>
    [Fact]
    public void NoOtherSiteUnderRuntimeIoNamesAShareMode()
    {
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        Assert.True(Directory.Exists(io), $"The IO subsystem moved: {io} is not a directory.");

        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name is "FileLockPosture.cs" or "FileSupport.cs") continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal) || t.StartsWith("///", StringComparison.Ordinal))
                    continue;
                if (lines[i].Contains("FileShare.", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetRelativePath(io, file)}:{i + 1}: {t}");
            }
        }

        Assert.True(offenders.Count == 0,
            "A FileShare value outside FileLockPosture. §9.1.15's file lock is a DERIVATION — the arbitrated "
            + "sharing mode, widened by the connectors Table 19 has admitted — and a site that names a share "
            + "mode has answered it locally. Sites:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>The positive complement, for the reason a ban over a subsystem that names no share mode passes
    /// exactly as green: the derivation still lives in its named home, still answers all three standard modes
    /// AND the undetermined default, and the registry still applies it in one place.</summary>
    [Fact]
    public void TheDerivationStillLivesInItsNamedHome()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", "Sharing", "FileLockPosture.cs"));
        Assert.Contains("public static FileShare OfSharingMode(", text, StringComparison.Ordinal);
        Assert.Contains("public static FileShare For(", text, StringComparison.Ordinal);
        foreach (string arm in new[] { "FileSharing.NoOther => FileShare.None", "FileSharing.ReadOnly => FileShare.Read",
                                       "FileSharing.AllOther => FileShare.ReadWrite", "null => FileShare.Read" })
            Assert.Contains(arm, text, StringComparison.Ordinal);

        string registry = TestRepo.Src("Cobol.Net.Runtime", "IO", "FileRegistry.cs");
        var writers = File.ReadAllLines(registry)
            .Where(l => l.Contains("HostShare =", StringComparison.Ordinal)
                        && !l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                        && !l.TrimStart().StartsWith("///", StringComparison.Ordinal))
            .ToList();
        Assert.True(writers.Count == 1,
            $"The §9.1.15 file lock must be applied in exactly one place; found {writers.Count}:\n  "
            + string.Join("\n  ", writers));
    }

    /// <summary>The theorem behind the measured matrix, checked symbolically over all 35 printed cells: wherever
    /// Table 19 prints <i>Normal open</i>, the two connectors' §9.1.15 postures are mutually admissible at the
    /// host — the incoming access allowed by the existing share mode, and the incoming share mode allowing the
    /// existing access. That is the property that makes "the arbiter decides, the handle obeys" true by
    /// construction rather than by the widening happening to be applied everywhere.</summary>
    [Fact]
    public void EveryNormalOpenCellHasMutuallyAdmissiblePostures()
    {
        foreach (var exS in Table19.StandardModes)
            foreach (var exM in Enum.GetValues<FileOpenMode>())
                foreach (var incS in Table19.StandardModes)
                    foreach (var incM in Enum.GetValues<FileOpenMode>())
                    {
                        if (Table19.Cell(incS, incM, exS, exM) != OpenSharingOutcome.NormalOpen) continue;
                        var exShare = FileLockPosture.For(exS, [incM]);   // widened by the admitted incoming one
                        var incShare = FileLockPosture.For(incS, [exM]);
                        var exAccess = FileLockPosture.AccessOf(exM);
                        var incAccess = FileLockPosture.AccessOf(incM);
                        Assert.True(exShare.HasFlag(FileLockPosture.Admitting(incAccess)),
                            $"Table 19 permits ({exS} {exM}) + ({incS} {incM}) but the existing connector's "
                            + $"file lock {exShare} does not admit {incAccess}.");
                        Assert.True(incShare.HasFlag(FileLockPosture.Admitting(exAccess)),
                            $"Table 19 permits ({exS} {exM}) + ({incS} {incM}) but the incoming connector's "
                            + $"file lock {incShare} does not admit {exAccess}.");
                    }
    }

    /// <summary>And the base postures alone — before any widening — already satisfy the same theorem for every
    /// DETERMINED pair, which is why a connector that names its sharing mode never needs its handle rebuilt.
    /// The widening exists for the UNDETERMINED default, whose posture is not derivable until kb/Work PB322 is
    /// answered; this fact is what says so out loud rather than leaving it to be noticed.</summary>
    [Fact]
    public void DeterminedModesNeedNoWidening()
    {
        foreach (var exS in Table19.StandardModes)
            foreach (var exM in Enum.GetValues<FileOpenMode>())
                foreach (var incS in Table19.StandardModes)
                    foreach (var incM in Enum.GetValues<FileOpenMode>())
                    {
                        if (Table19.Cell(incS, incM, exS, exM) != OpenSharingOutcome.NormalOpen) continue;
                        Assert.True(FileLockPosture.OfSharingMode(exS)
                                .HasFlag(FileLockPosture.Admitting(FileLockPosture.AccessOf(incM))),
                            $"({exS} {exM}) would have to be widened to admit ({incS} {incM}).");
                    }

        // The complement, so this does not read as "widening is dead code": the reported pair needs it.
        Assert.False(FileLockPosture.OfSharingMode(null)
            .HasFlag(FileLockPosture.Admitting(FileLockPosture.AccessOf(FileOpenMode.Extend))));
        Assert.True(FileLockPosture.For(null, [FileOpenMode.Extend])
            .HasFlag(FileLockPosture.Admitting(FileLockPosture.AccessOf(FileOpenMode.Extend))));
    }

    /// <summary>The rebuild is a real member of the connector contract, not a call-site edit — a new
    /// organization that takes a long-lived host handle and forgets it would silently reintroduce the veto,
    /// so the base implementation exists and the sequential connector overrides it.</summary>
    [Fact]
    public void ThePostureRebuildIsPartOfTheConnectorContract()
    {
        var basem = typeof(FileConnector).GetMethod("Reposture",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(basem);
        Assert.True(basem!.IsVirtual, "Reposture shall be overridable: a connector holding a host handle owes a rebuild.");
        var seq = typeof(SequentialConnector).GetMethod("Reposture",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(seq);
        Assert.Equal(typeof(SequentialConnector), seq!.DeclaringType);
    }
}
