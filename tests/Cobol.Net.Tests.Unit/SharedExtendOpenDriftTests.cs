// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN <c>OPEN</c> HAS ONLY I-O STATUSES AS OUTCOMES — never an escaping exception (kb/Work PB713).
/// ISO §14.9.27.4 GR1 makes every OPEN <i>"cause the value of the I-O status associated with file-name-1 to be
/// updated to one of the values in 9.1.13"</i>, GR25 makes an unsuccessful one leave <i>"the file … not
/// affected"</i>, and §9.1.13.6 item 1 gives the residual host failure its value ('30'). §9.1.15 says which
/// opens are ALLOWED — <i>"the sharing with all other mode allows concurrent access to a physical file through
/// other file connectors specifying input, I-O, or extend mode"</i> — and it puts the gate on the FILE
/// CONNECTORS and §14.9.27.4 Table 19, not on the operating environment's handle.
///
/// <para><b>What PB713 was.</b> A sharing-active <c>OPEN EXTEND</c> opened its append writer and THEN measured
/// the file's existing record count from a SECOND handle on the same path: <c>File.ReadLines</c> for the
/// line-sequential framing and a three-argument <c>FileStream</c> for the varying one, both requesting
/// <see cref="FileShare.Read"/>, which does not admit the Write access the connector was already holding. The
/// host refused, and because the measurement ran from <c>FileRegistry.SharedOpenAttempt</c> — AFTER
/// <c>FileConnector.Open</c> had returned, outside its try — the refusal left the run unit as an unhandled
/// <c>IOException</c> whose message named "another process" that was the program itself. The fixed-width
/// framing measured <c>FileInfo.Length</c>, took no handle, and passed: one dispatch, three arms, one of them
/// tested.</para>
///
/// <para>So the guard is the WHOLE MATRIX, not the reported arm: every organization × framing a shared
/// <c>OPEN EXTEND</c> can take, in every spelling that makes a connector a §9.1.15 participant. A shape added
/// later — a fourth framing, a fourth organization — fails here the day it is registered rather than the day a
/// user writes <c>SHARING</c> on it.</para>
/// </summary>
public sealed class SharedExtendOpenDriftTests
{
    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb713-{tag}-{Guid.NewGuid():N}.dat");

    /// <summary>How a connector becomes a §9.1.15 sharing participant. All three reach
    /// <c>FileRegistry.RegisterSharing</c>, which is what sets <c>FileConnector.SharedStreams</c> and puts the
    /// connector in the record-locking posture map — the gate the EXTEND write-base seeding reads.</summary>
    public enum Spelling
    {
        /// <summary>SHARING WITH ALL OTHER + LOCK MODE IS MANUAL (§14.9.27.3 SR8 requires the LOCK MODE clause
        /// for the ALL spelling, which is why the corpus under-covers this band).</summary>
        AllOtherClause,

        /// <summary>A LOCK MODE clause and NO SHARING clause — §9.1.15's undetermined implementor default
        /// (kb/Work PB322), registered as a null sharing mode.</summary>
        LockModeOnly,

        /// <summary>No file-control clause at all; the OPEN statement itself carries the SHARING phrase
        /// (§14.9.27 — <c>FileRegistry.OpenShared</c> registers the posture on the spot). The phrase is READ
        /// ONLY rather than ALL because §14.9.27.3 SR8 admits the ALL spelling only when <i>"the LOCK MODE
        /// clause … is specified in the file control entry"</i>, which this spelling by construction has not
        /// got — an ALL phrase here would be non-conforming source, and a guard built on non-conforming source
        /// measures nothing.</summary>
        OpenPhrase,
    }

    public static TheoryData<Spelling> Spellings() =>
        new(Spelling.AllOtherClause, Spelling.LockModeOnly, Spelling.OpenPhrase);

    /// <summary>Make <paramref name="name"/> a sharing participant in <paramref name="spelling"/>'s way, then
    /// OPEN it in <paramref name="mode"/>. Returns the resulting I-O status.</summary>
    private static string ShareAndOpen(FileRegistry reg, string name, Spelling spelling, FileOpenMode mode)
    {
        switch (spelling)
        {
            case Spelling.AllOtherClause:
                reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
                break;
            case Spelling.LockModeOnly:
                reg.RegisterSharing(name, FileRegistry.ImplementorDefaultSharing, FileLockMode.Manual, false);
                break;
            case Spelling.OpenPhrase:
                // No SELECT clause: the phrase on the OPEN is what makes it a participant. READ ONLY, not ALL
                // — §14.9.27.3 SR8, see the enum member.
                reg.OpenShared(name, mode, hasSharingOverride: true, FileSharing.ReadOnly,
                    FileRetryKind.None, 0, noRewind: false, reg.HostPathOf(name), assignDynamic: false, page: null);
                return reg.Status(name);
        }
        reg.OpenStatic(name, mode);
        return reg.Status(name);
    }

    // ── Sequential organization: the three framings §9.1.7.2's two file types give it ────────────────────────

    /// <summary>The framings a sequential connector can carry on disk. Named, not booleans, so a fourth one
    /// cannot be added without landing in this matrix.</summary>
    public enum Framing
    {
        /// <summary>LINE SEQUENTIAL — newline-delimited records. PB713's reported arm.</summary>
        LineSequential,

        /// <summary>Record sequential, FIXED width — uniform blocks. The arm that always passed, kept as the
        /// control that proves the matrix is measuring the difference and not the harness.</summary>
        FixedRecordSequential,

        /// <summary>Record sequential, RECORD IS VARYING — length-prefixed frames (§13.18.43 GR2). PB713's
        /// confirmed sibling: it crashed through <c>RecordFraming.ReadStore</c> for the identical reason.</summary>
        VaryingRecordSequential,
    }

    public static TheoryData<Framing, Spelling> SequentialShapes()
    {
        var data = new TheoryData<Framing, Spelling>();
        foreach (var f in Enum.GetValues<Framing>())
            foreach (var s in Enum.GetValues<Spelling>())
                data.Add(f, s);
        return data;
    }

    /// <summary>The subset of <see cref="SequentialShapes"/> whose connector carries a LOCK MODE clause, so
    /// §12.4.5.9 GR1 puts record locking in effect and the write ordinal has an observable
    /// (<see cref="SharedExtend_WriteOrdinalContinuesTheExistingRecords"/> says why).</summary>
    public static TheoryData<Framing, Spelling> LockBearingSequentialShapes()
    {
        var data = new TheoryData<Framing, Spelling>();
        foreach (var f in Enum.GetValues<Framing>())
            foreach (var s in new[] { Spelling.AllOtherClause, Spelling.LockModeOnly })
                data.Add(f, s);
        return data;
    }

    private static void RegisterSequential(FileRegistry reg, string name, string host, Framing framing) =>
        reg.Register(name, host, recordWidth: 4, lineSequential: framing == Framing.LineSequential,
            optional: false,
            varyMin: framing == Framing.VaryingRecordSequential ? 3 : -1,
            varyMax: framing == Framing.VaryingRecordSequential ? 8 : -1);

    /// <summary>⛔ THE REGRESSION. Every (framing × sharing spelling) opens EXTEND over a seeded file, appends,
    /// and reads both records back — no exception, a success-family status at every step (§9.1.13.2), and the
    /// appended record following the existing one (§14.9.51.4 GR18: <i>"the first record written after the
    /// execution of the OPEN statement with the EXTEND phrase is the successor of the last record in the
    /// physical file"</i>).</summary>
    [Theory]
    [MemberData(nameof(SequentialShapes))]
    public void SharedExtend_OpensAndAppends_ForEverySequentialFraming(Framing framing, Spelling spelling)
    {
        string host = Tmp($"seq-{framing}-{spelling}");
        try
        {
            var reg = new FileRegistry();
            RegisterSequential(reg, "F", host, framing);
            Assert.Equal(FileStatusCode.Success, ShareAndOpen(reg, "F", spelling, FileOpenMode.Output));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("F", "AAAA", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("F");

            // The statement PB713 killed the run unit on.
            Assert.Equal(FileStatusCode.Success, ShareAndOpen(reg, "F", spelling, FileOpenMode.Extend));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("F", "BBBB", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("F");

            var back = new FileRegistry();
            RegisterSequential(back, "R", host, framing);
            back.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success,
                back.ReadShared("R", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string a));
            Assert.Equal(FileStatusCode.Success,
                back.ReadShared("R", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string b));
            Assert.Equal("AAAA", a);
            Assert.Equal("BBBB", b);
            back.Close("R");
        }
        finally { TryDelete(host); }
    }

    /// <summary>The VALUE the seeding exists for, not merely its survival: §9.1.16 makes a sequential record's
    /// lock identity its ordinal in the physical file, and §14.9.51.4 GR19 fixes the base at <i>"the records
    /// present in the physical file when it was opened"</i>. One record exists, so the record the EXTEND session
    /// writes WITH LOCK is ordinal <b>2</b> — measured through a sibling connector's view of the lock table, the
    /// only thing that can tell 2 from 1.
    /// <para>A green here with a broken seed is impossible in the direction that matters: a base of 0 would lock
    /// ordinal 1, which the assertions below reject explicitly rather than by omission.</para>
    /// <para>⛔ ONLY THE TWO LOCK-MODE-BEARING SPELLINGS APPEAR, and the omission is a fact about the standard
    /// rather than a convenience: §12.4.5.9 GR1 makes a connector with no LOCK MODE clause take the implementor
    /// default, which for COBOL.NET is no record locking, so a <see cref="Spelling.OpenPhrase"/> connector sets
    /// no lock and its write ordinal has no observable at all. Asserting over it would assert the absence of a
    /// lock that the standard never asked for. Its OPEN and its append are still covered, by
    /// <see cref="SharedExtend_OpensAndAppends_ForEverySequentialFraming"/>.</para></summary>
    [Theory]
    [MemberData(nameof(LockBearingSequentialShapes))]
    public void SharedExtend_WriteOrdinalContinuesTheExistingRecords(Framing framing, Spelling spelling)
    {
        string host = Tmp($"ord-{framing}-{spelling}");
        try
        {
            var reg = new FileRegistry();
            RegisterSequential(reg, "W", host, framing);
            RegisterSequential(reg, "O", host, framing);   // the sibling that OBSERVES the lock; never opened
            reg.RegisterSharing("O", FileSharing.AllOther, FileLockMode.Manual, multiple: false);

            Assert.Equal(FileStatusCode.Success, ShareAndOpen(reg, "W", spelling, FileOpenMode.Output));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("W", "AAAA", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("W");

            Assert.Equal(FileStatusCode.Success, ShareAndOpen(reg, "W", spelling, FileOpenMode.Extend));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("W", "BBBB", -1, FileRecordLock.WithLock, FileRetryKind.None, 0, page: null));
            Assert.True(reg.IsLockedByOther("O", "2"),
                "§14.9.51.4 GR19 — the record appended by a shared EXTEND is the successor of the records "
                + "present when the file was opened, so its ordinal is 2. The write-ordinal base was not seeded.");
            Assert.False(reg.IsLockedByOther("O", "1"),
                "Ordinal 1 is the record that already existed; locking it means the base was 0.");
            reg.Close("W");
        }
        finally { TryDelete(host); }
    }

    // ── The keyed organizations: no write-ordinal base, but the same OPEN contract ───────────────────────────

    /// <summary>RELATIVE and INDEXED reach the same shared <c>OPEN EXTEND</c> (§14.9.27.3 SR2 admits it for any
    /// organization at sequential access; Table 20 lists WRITE under the extend column for all three). They keep
    /// their records in a <c>RecordFraming</c> store whose whole-file load and persist were the other two
    /// share-mode-assuming opens in the subsystem — <c>ReadStore</c>'s implicit <see cref="FileShare.Read"/> and
    /// <c>WriteStore</c>'s implicit <see cref="FileShare.None"/>, the stricter form of the same defect. Neither
    /// crashed today, and that is exactly why they belong here: the matrix has to fail when a LATER change makes
    /// a keyed store overlap a held handle, not only when the reported arm regresses.</summary>
    [Theory]
    [MemberData(nameof(Spellings))]
    public void SharedExtend_OpensAndAppends_ForTheRelativeOrganization(Spelling spelling)
    {
        string host = Tmp($"rel-{spelling}");
        try
        {
            var reg = new FileRegistry();
            reg.RegisterRelative("F", host, 4, optional: false, accessMode: 0, relativeKeyDigits: 4, -1, -1);
            Assert.Equal(FileStatusCode.Success, ShareAndOpen(reg, "F", spelling, FileOpenMode.Output));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("F", "AAAA", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("F");

            Assert.Equal(FileStatusCode.Success, ShareAndOpen(reg, "F", spelling, FileOpenMode.Extend));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("F", "BBBB", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("F");

            var back = new FileRegistry();
            back.RegisterRelative("R", host, 4, false, 0, 4, -1, -1);
            back.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, back.ReadKeyedNext("R", out string a));
            Assert.Equal(FileStatusCode.Success, back.ReadKeyedNext("R", out string b));
            Assert.Equal("AAAA", a);
            Assert.Equal("BBBB", b);
            back.Close("R");
        }
        finally { TryDelete(host); }
    }

    /// <inheritdoc cref="SharedExtend_OpensAndAppends_ForTheRelativeOrganization"/>
    [Theory]
    [MemberData(nameof(Spellings))]
    public void SharedExtend_OpensAndAppends_ForTheIndexedOrganization(Spelling spelling)
    {
        string host = Tmp($"idx-{spelling}");
        try
        {
            var reg = new FileRegistry();
            reg.RegisterIndexed("F", host, 7, optional: false, accessMode: 0, primeOffset: 0, primeLength: 4, -1, -1);
            Assert.Equal(FileStatusCode.Success, ShareAndOpen(reg, "F", spelling, FileOpenMode.Output));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("F", "K002AAA", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("F");

            // §14.9.51.4 GR38 — under extend the first released record's prime key must exceed the highest key
            // present, so K006 is the conforming append over K002.
            Assert.Equal(FileStatusCode.Success, ShareAndOpen(reg, "F", spelling, FileOpenMode.Extend));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("F", "K006BBB", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("F");

            var back = new FileRegistry();
            back.RegisterIndexed("R", host, 7, false, 0, 0, 4, -1, -1);
            back.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, back.ReadKeyedNext("R", out string a));
            Assert.Equal(FileStatusCode.Success, back.ReadKeyedNext("R", out string b));
            Assert.Equal("K002AAA", a);
            Assert.Equal("K006BBB", b);
            back.Close("R");
        }
        finally { TryDelete(host); }
    }

    private static void TryDelete(string host)
    {
        foreach (string p in new[] { host, host + ".cbattr" })
            try { if (Path.GetFileName(p).Length > 0) File.Delete(p); } catch (IOException) { }
    }

    // ── The structural half: the share mode is stated in ONE place, so it cannot be omitted ──────────────────

    /// <summary>Every way the runtime can open a host PATH. The .NET defaults are the trap: the three-argument
    /// <c>FileStream</c> is <see cref="FileShare.Read"/>, the two-argument one is <see cref="FileShare.None"/>,
    /// and every <c>File.*</c> content helper is <see cref="FileShare.Read"/> — so a site that says nothing has
    /// silently declared that no other handle on this path may write, which is a statement about §9.1.15 that no
    /// call site is entitled to make.</summary>
    private static readonly Regex PathOpen = new(
        @"\bnew\s+FileStream\s*\(|\bFile\.(?:ReadLines|ReadAllLines|ReadAllText|ReadAllBytes|WriteAllText"
        + @"|WriteAllLines|WriteAllBytes|AppendAllText|AppendAllLines|OpenRead|OpenWrite|OpenText|CreateText"
        + @"|AppendText|Open|Create)\s*\(", RegexOptions.Compiled);

    /// <summary>A <c>StreamReader</c>/<c>StreamWriter</c> is legitimate over a STREAM and forbidden over a PATH,
    /// and the two are told apart by the first argument: a local stream variable (lower-case initial) or a
    /// <c>HostFile.Open…</c> call is allowed; a string literal or a <c>…Path</c> expression is the path form.
    /// The rule is spelled positively — an argument shape that is not recognized fails — because the guard has
    /// to reject the NEXT spelling of a path, not only the two that occurred.</summary>
    private static readonly Regex TextStreamCtor =
        new(@"\bnew\s+Stream(?:Reader|Writer)\s*\(\s*([^,)]*)", RegexOptions.Compiled);

    private static readonly Regex AllowedStreamArgument =
        new(@"^(?:[a-z_][A-Za-z0-9_]*|HostFile\.Open[A-Za-z]*\s*\()", RegexOptions.Compiled);

    /// <summary>The one file allowed to open a host path.</summary>
    private const string StreamHome = "FileSupport.cs";

    /// <summary>⛔ NO FILE UNDER <c>Cobol.Net.Runtime/IO</c> OPENS A HOST PATH EXCEPT <c>HostFile</c>. The share
    /// mode is not a per-site decision: it is §9.1.15's answer to "what may the other file connectors of this
    /// run unit do while this handle is open?", and it has exactly two answers (the connector's own posture and
    /// the permissive bookkeeping one), both derived inside <c>HostFile</c> from the ROLE. PB713 is what the
    /// per-site form cost: four hand-written share ternaries, two implicit constructor defaults, and a crash on
    /// the two organizations whose framing happened to need a second handle.</summary>
    [Fact]
    public void OnlyHostFileOpensAHostPath()
    {
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        Assert.True(Directory.Exists(io), $"The IO subsystem moved: {io} is not a directory.");

        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFileName(file), StreamHome, StringComparison.Ordinal)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;   // prose about the rule
                if (PathOpen.IsMatch(line))
                {
                    offenders.Add($"{Path.GetRelativePath(io, file)}:{i + 1}: {line.Trim()}");
                    continue;
                }
                if (TextStreamCtor.Match(line) is { Success: true } m
                    && !AllowedStreamArgument.IsMatch(m.Groups[1].Value.Trim()))
                    offenders.Add($"{Path.GetRelativePath(io, file)}:{i + 1}: {line.Trim()}");
            }
        }

        Assert.True(offenders.Count == 0,
            "A host-path open outside HostFile. The .NET path constructors default to FileShare.Read (three "
            + "arguments) or FileShare.None (two), which forbids a handle another file connector of this run "
            + "unit legitimately holds — and ISO §9.1.15 puts that gate on the file connectors and §14.9.27.4 "
            + "Table 19, not on the operating environment's handle. An OPEN's only outcomes are the §9.1.13 I-O "
            + "statuses (§14.9.27.4 GR1/GR25), never an escaping IOException (kb/Work PB713). Call "
            + "HostFile.OpenConnectorStream (a connector's own long-lived handle) or HostFile.OpenAuxiliary "
            + "(short-lived bookkeeping over a path a connector may hold). Offending sites:\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>The positive complement, for the reason
    /// <c>HostFileProbeDriftTests.TheProbeItselfStillLivesInItsNamedHome</c> gives: a ban over a subsystem that
    /// opens NOTHING passes exactly as green as one that opens everything in the right place. This pins both
    /// roles to their home and asserts that every stream constructed there names its <c>FileShare</c> — the
    /// omission being the whole defect.</summary>
    [Fact]
    public void BothOpenRolesStillLiveInTheirNamedHome_AndNameTheirShareMode()
    {
        string home = TestRepo.Src("Cobol.Net.Runtime", "IO", StreamHome);
        string text = File.ReadAllText(home);
        Assert.Contains("public static FileStream OpenConnectorStream(", text, StringComparison.Ordinal);
        Assert.Contains("public static FileStream OpenAuxiliary(", text, StringComparison.Ordinal);

        var shareless = new List<string>();
        string[] lines = File.ReadAllLines(home);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
            if (line.TrimStart().StartsWith("///", StringComparison.Ordinal)) continue;
            if (!line.Contains("new FileStream(", StringComparison.Ordinal)
                && !line.Contains("new(hostPath", StringComparison.Ordinal)) continue;
            if (!line.Contains("FileShare.", StringComparison.Ordinal)
                && !lines[i + 1].Contains("FileShare.", StringComparison.Ordinal))
                shareless.Add($"{StreamHome}:{i + 1}: {line.Trim()}");
        }

        Assert.True(shareless.Count == 0,
            "A stream constructed in HostFile without naming its FileShare. The share mode is the whole point "
            + "of routing every open through here (kb/Work PB713). Sites:\n  " + string.Join("\n  ", shareless));
    }

    /// <summary>⛔ §9.1.15 PARTICIPATION HAS ONE ANSWER. <c>FileConnector.SharedStreams</c> is what every stream
    /// in <c>SequentialConnector.OpenCore</c> reads for its share posture and what the EXTEND write-ordinal base
    /// is gated on, and <c>FileRegistry._connectorShares</c> is the register that records the SHARING/LOCK MODE
    /// posture — two names for one fact. They must not be maintained separately: the bit is assigned in exactly
    /// ONE place, immediately before the sole <c>c.Open(mode)</c> call, so it is DERIVED from the register at
    /// the one moment anything reads it rather than pushed at registration time behind a lookup that a
    /// registration order could miss. A second writer is a second rule (the shape of kb/Work PB321 and PB713
    /// both).</summary>
    [Fact]
    public void SharingParticipationIsAssignedInExactlyOnePlace()
    {
        string registry = TestRepo.Src("Cobol.Net.Runtime", "IO", "FileRegistry.cs");
        string[] lines = File.ReadAllLines(registry);
        var writes = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
            if (line.TrimStart().StartsWith("///", StringComparison.Ordinal)) continue;
            if (line.Contains("SharedStreams =", StringComparison.Ordinal))
                writes.Add($"FileRegistry.cs:{i + 1}: {line.Trim()}");
        }

        Assert.True(writes.Count == 1,
            $"FileConnector.SharedStreams must have exactly one writer; found {writes.Count}. Two writers are "
            + "two answers to §9.1.15's participation question, and the streams and the write-ordinal seeding "
            + "read it from opposite ends of an OPEN (kb/Work PB713). Sites:\n  " + string.Join("\n  ", writes));
        Assert.Contains("_connectorShares.ContainsKey(name)", writes[0], StringComparison.Ordinal);

        // The complement: the bit is set BEFORE the open it governs, not after it (the PB713 ordering).
        int assign = Array.FindIndex(lines, l => l.Contains("SharedStreams =", StringComparison.Ordinal)
            && !l.TrimStart().StartsWith("//", StringComparison.Ordinal));
        int open = Array.FindIndex(lines, l => l.Contains("c.Open(mode)", StringComparison.Ordinal)
            && !l.TrimStart().StartsWith("//", StringComparison.Ordinal));
        Assert.True(assign >= 0 && open > assign,
            $"The participation bit is assigned at line {assign + 1} and the open it governs is at line "
            + $"{open + 1}: OpenCore reads the bit, so assigning it afterwards would open every stream on the "
            + "PREVIOUS open's posture.");
    }

    /// <summary>The runtime half of the same invariant, measured rather than read off the source: a connector
    /// whose SELECT declares neither clause opens with the exclusive posture and seeds no write base, and one
    /// that declares a clause opens shared — <b>on the very first OPEN after the registration</b>, which is the
    /// ordering the fact above pins in the text.</summary>
    [Fact]
    public void ParticipationTakesEffectOnTheFirstOpenAfterRegistration()
    {
        string host = Tmp("posture");
        try
        {
            var reg = new FileRegistry();
            reg.Register("P", host, 4, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
            reg.OpenStatic("P", FileOpenMode.Output);
            Assert.Equal(FileStatusCode.Success, reg.Status("P"));
            reg.Close("P");

            // The clause arrives, then the very next OPEN must already carry the posture: a shared EXTEND seeds
            // its base, so a WITH LOCK write locks ordinal 1 over the empty file this connector just created.
            reg.RegisterSharing("P", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
            reg.Register("Q", host, 4, false, false, -1, -1);
            reg.RegisterSharing("Q", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
            reg.OpenStatic("P", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.Status("P"));
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("P", "AAAA", -1, FileRecordLock.WithLock, FileRetryKind.None, 0, page: null));
            Assert.True(reg.IsLockedByOther("Q", "1"),
                "The connector was a §9.1.15 participant before this OPEN, so the OPEN had to seed its write "
                + "base; an unseeded base (−1) leaves LastWrittenRecordId empty and nothing is locked.");
            reg.Close("P");
        }
        finally { TryDelete(host); }
    }
}
