// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A CONNECTOR IS A RECORD-LOCKING PARTICIPANT EXACTLY WHEN ITS RECORD LOCKS CAN BE SET, AND A SHARING CLAUSE, A
/// SHARING OR RETRY PHRASE ON THE OPEN, OR THE NO OTHER MODE CONFERS NONE (kb/Work PB3372). ISO §12.4.5.9.4 GR1:
/// <i>"If the LOCK MODE clause is omitted from a file control entry, a) If there is a SHARING clause in that file
/// control entry, no record locks are set by the execution of I-O statements through the associated file
/// connector"</i> and <i>"b) … 1. If an OPEN statement for the associated file connector has a SHARING phrase, no
/// record locks are set by the execution of I-O statements for that opening of the associated file connector"</i>;
/// GR3: <i>"If a physical file is open in the sharing with no other mode, the LOCK MODE clause has no effect"</i>.
/// <para><c>FileConnector.SharedPhysical</c> is the participation fact: the §9.1.16 lock identity the release-ordinal
/// mint of a sequential <c>OPEN EXTEND</c> numbers (a whole-file count of the records already there) and the
/// read-write host access a locking writer needs. <c>FileRegistry.OpenCore</c> used to make ANY SHARING or RETRY
/// phrase a participant by registering a lock-less posture on the spot, and <c>SharedOpenAttempt</c> read the mere
/// presence of a registered posture, so a SELECT with a SHARING clause alone and an OPEN carrying a SHARING or RETRY
/// phrase paid the count and asked for the access for locks the standard says are never set. Participation is now
/// derived at each OPEN from the ONE predicate <c>FileRegistry.RecordLocksSetUnder</c>, which is also what
/// <c>LocksEffective</c> asks at every I-O statement, so the two cannot disagree.</para>
/// </summary>
public sealed class RecordLockingParticipationDriftTests
{
    private static FileConnector Connector(FileRegistry reg, string name) =>
        (FileConnector)typeof(FileRegistry).GetMethod("Require", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(reg, [name])!;

    private static void RegisterSequential(FileRegistry reg, string name, string host,
        SharedExtendOpenDriftTests.Framing framing, bool optional = false) =>
        reg.Register(name, host, recordWidth: 4,
            lineSequential: framing == SharedExtendOpenDriftTests.Framing.LineSequential, optional: optional,
            varyMin: framing == SharedExtendOpenDriftTests.Framing.VaryingRecordSequential ? 3 : -1,
            varyMax: framing == SharedExtendOpenDriftTests.Framing.VaryingRecordSequential ? 8 : -1);

    /// <summary>Two records "AAAA" and "BBBB" in <paramref name="host"/>, written by a registry of its own.</summary>
    private static void Seed(string host, SharedExtendOpenDriftTests.Framing framing)
    {
        var seed = new FileRegistry();
        RegisterSequential(seed, "S", host, framing);
        seed.OpenStatic("S", FileOpenMode.Output);
        Assert.Equal("00", seed.Status("S"));
        foreach (string record in new[] { "AAAA", "BBBB" })
            Assert.Equal("00", seed.WriteShared("S", record, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
        seed.Close("S");
        Assert.Equal("00", seed.Status("S"));
    }

    private static void Open(FileRegistry reg, string name, FileOpenMode mode, FileSharing? phraseSharing = null,
        FileRetryKind retry = FileRetryKind.None, long retryAmount = 0)
    {
        // The emitter's two entries: an OPEN that writes no SHARING or RETRY phrase is the plain one.
        if (phraseSharing is null && retry == FileRetryKind.None) { reg.OpenStatic(name, mode); return; }
        reg.OpenShared(name, mode, hasSharingOverride: phraseSharing is not null,
            phraseSharing.GetValueOrDefault(), retry, retryAmount, OpenTapePhrase.None,
            reg.HostPathOf(name), assignDynamic: false, page: null);
    }

    /// <summary>One way a connector comes to be opened EXTEND, with the answers §12.4.5.9.4 gives for it. The
    /// ordinal base is measured (and so non-zero) only for a participant whose file lock admits no other writer; a
    /// writer that may meet another counts its ordinals off the medium instead (kb/Work PB2692).</summary>
    public sealed record Case(string Name, FileSharing? ClauseSharing, FileLockMode? ClauseLock,
        FileSharing? PhraseSharing, FileRetryKind Retry, bool Participates, long ExpectedOrdinalBase)
    {
        public override string ToString() => Name;
    }

    public static IEnumerable<Case> Cases() =>
    [
        // GR1 a): a SHARING clause and no LOCK MODE clause — no record locks are set.
        new("SelectSharingAllOtherOnly", FileSharing.AllOther, FileLockMode.None, null, FileRetryKind.None, false, 0),
        new("SelectSharingReadOnlyOnly", FileSharing.ReadOnly, FileLockMode.None, null, FileRetryKind.None, false, 0),
        // GR1 b) 1.: no LOCK MODE clause and a SHARING phrase on the OPEN.
        new("NoClauseSharingPhrase", null, null, FileSharing.ReadOnly, FileRetryKind.None, false, 0),
        // GR1 b) 2.: no SHARING anywhere — the implementor default (here: no record locking). A RETRY phrase says
        // nothing about locks, so the open stays a non-participant.
        new("NoClauseRetryPhraseOnly", null, null, null, FileRetryKind.Times, false, 0),
        new("NoClauseSharingAndRetryPhrases", null, null, FileSharing.ReadOnly, FileRetryKind.Times, false, 0),
        new("NoClausePlainOpen", null, null, null, FileRetryKind.None, false, 0),
        // The LOCK MODE clause has its effects (GR3 "Otherwise") — a participant.
        new("LockModeReadOnlyClause", FileSharing.ReadOnly, FileLockMode.Manual, null, FileRetryKind.None, true, 2),
        new("LockModeAutomaticReadOnlyClause", FileSharing.ReadOnly, FileLockMode.Automatic, null, FileRetryKind.None, true, 2),
        new("LockModeAllOtherClause", FileSharing.AllOther, FileLockMode.Manual, null, FileRetryKind.None, true, 0),
        new("LockModeClauseAllOtherPhrase", null, FileLockMode.Manual, FileSharing.AllOther, FileRetryKind.None, true, 0),
        // §9.1.15 / §14.9.27.4 GR23: the OPEN's SHARING phrase overrides the clause, and the posture is per OPEN.
        new("NoOtherClauseOverriddenByReadOnlyPhrase", FileSharing.NoOther, FileLockMode.Manual, FileSharing.ReadOnly, FileRetryKind.None, true, 2),
        new("ReadOnlyClauseOverriddenByNoOtherPhrase", FileSharing.ReadOnly, FileLockMode.Manual, FileSharing.NoOther, FileRetryKind.None, false, 0),
        // GR3: the physical file is open in the sharing with no other mode — the LOCK MODE clause has no effect.
        new("LockModeNoOtherClause", FileSharing.NoOther, FileLockMode.Manual, null, FileRetryKind.None, false, 0),
        // …including when NO OTHER is only the implementor default of the mode (EXTEND — FileRegistry.ImplementorDefaultSharing).
        new("LockModeOnlyClauseExtendDefaultNoOther", null, FileLockMode.Manual, null, FileRetryKind.None, false, 0),
    ];

    public static TheoryData<SharedExtendOpenDriftTests.Framing, string> Shapes()
    {
        var data = new TheoryData<SharedExtendOpenDriftTests.Framing, string>();
        foreach (var f in Enum.GetValues<SharedExtendOpenDriftTests.Framing>())
            foreach (var c in Cases())
                data.Add(f, c.Name);
        return data;
    }

    private static Case CaseNamed(string name) => Cases().Single(c => c.Name == name);

    private static string OpenAs(FileRegistry reg, string name, Case c, FileOpenMode mode)
    {
        if (c.ClauseSharing is not null || c.ClauseLock is not null)
            reg.RegisterSharing(name, c.ClauseSharing, c.ClauseLock ?? FileLockMode.None, multiple: false);
        Open(reg, name, mode, c.PhraseSharing, c.Retry, c.Retry == FileRetryKind.None ? 0 : 3);
        return reg.Status(name);
    }

    /// <summary>⛔ THE RULE, over every spelling × every framing: the EXTEND opens successfully, the connector
    /// participates exactly when <see cref="Case.Participates"/>, and the whole-file ordinal count is paid only by
    /// a participant (the physical file's mint reads the seeded number back).</summary>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void ParticipationIsWhetherRecordLocksCanBeSet(SharedExtendOpenDriftTests.Framing framing, string caseName)
    {
        var c = CaseNamed(caseName);
        string host = FileHarness.Tmp($"{framing}-{caseName}");
        try
        {
            Seed(host, framing);
            var reg = new FileRegistry();
            RegisterSequential(reg, "F", host, framing);
            Assert.Equal("00", OpenAs(reg, "F", c, FileOpenMode.Extend));

            var f = Connector(reg, "F");
            Assert.NotNull(f.Physical);   // the medium's state reaches EVERY connector (kb/Work PB753)
            Assert.Equal(c.Participates, f.SharedPhysical is not null);
            Assert.Equal(c.ExpectedOrdinalBase, f.Physical!.ReleasedOrdinal);

            Assert.Equal("00", reg.WriteShared("F", "CCCC", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("F");
            Assert.Equal("00", reg.Status("F"));
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>GR1 a) / b) 1. made observable: a WRITE … WITH LOCK through a connector whose SHARING clause or
    /// phrase stands without a LOCK MODE clause sets NO record lock, so a sibling that DOES set locks sees record
    /// 3 as free; the control (a LOCK MODE clause under SHARING WITH ALL OTHER) locks it. Every framing.</summary>
    [Theory]
    [InlineData(SharedExtendOpenDriftTests.Framing.LineSequential)]
    [InlineData(SharedExtendOpenDriftTests.Framing.FixedRecordSequential)]
    [InlineData(SharedExtendOpenDriftTests.Framing.VaryingRecordSequential)]
    public void AWriteWithLockSetsALockOnlyUnderALockModeClause(SharedExtendOpenDriftTests.Framing framing)
    {
        foreach (string name in new[] { "SelectSharingAllOtherOnly", "LockModeAllOtherClause" })
        {
            var c = CaseNamed(name);
            string host = FileHarness.Tmp($"wl-{framing}-{name}");
            try
            {
                Seed(host, framing);
                var reg = new FileRegistry();
                RegisterSequential(reg, "W", host, framing);
                RegisterSequential(reg, "O", host, framing);   // the sibling that OBSERVES locks
                reg.RegisterSharing("O", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
                Assert.Equal("00", OpenAs(reg, "W", c, FileOpenMode.Extend));
                Assert.Equal("00", reg.WriteShared("W", "CCCC", -1, FileRecordLock.WithLock, FileRetryKind.None, 0, page: null));
                Assert.Equal(c.Participates, reg.IsLockedByOther("O", "3"));
                reg.Close("W");
            }
            finally { FileHarness.TryDelete(host); }
        }
    }

    /// <summary>The conflict path stays whole for a participant: a sibling reader that reaches the record a
    /// locking writer released WITH LOCK gets the record operation conflict '51' (§14.9.30.4 GR9/GR10) and the
    /// record before it is delivered '00'; the same read against a lock-less writer's record answers '00'.</summary>
    [Theory]
    [InlineData(SharedExtendOpenDriftTests.Framing.LineSequential)]
    [InlineData(SharedExtendOpenDriftTests.Framing.FixedRecordSequential)]
    [InlineData(SharedExtendOpenDriftTests.Framing.VaryingRecordSequential)]
    public void ASiblingReadMeetsTheParticipantsLock_AndNeverALockLessWritersRecord(SharedExtendOpenDriftTests.Framing framing)
    {
        foreach (var (name, status) in new[] { ("LockModeAllOtherClause", "51"), ("SelectSharingAllOtherOnly", "00") })
        {
            var c = CaseNamed(name);
            string host = FileHarness.Tmp($"rd-{framing}-{name}");
            try
            {
                Seed(host, framing);
                var reg = new FileRegistry();
                RegisterSequential(reg, "W", host, framing);
                RegisterSequential(reg, "O", host, framing);
                reg.RegisterSharing("O", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
                Assert.Equal("00", OpenAs(reg, "W", c, FileOpenMode.Extend));
                Assert.Equal("00", reg.WriteShared("W", "CCCC", -1, FileRecordLock.WithLock, FileRetryKind.None, 0, page: null));
                reg.OpenStatic("O", FileOpenMode.Input);
                Assert.Equal("00", reg.Status("O"));
                Assert.Equal("00", reg.ReadShared("O", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string r1));
                Assert.Equal("00", reg.ReadShared("O", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string r2));
                Assert.Equal(("AAAA", "BBBB"), (r1, r2));
                Assert.Equal(status, reg.ReadShared("O", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string r3));
                if (status == "00") Assert.Equal("CCCC", r3);
                reg.Close("O");
                reg.Close("W");
            }
            finally { FileHarness.TryDelete(host); }
        }
    }

    /// <summary>The unsuccessful OPENs, each through the phrase spellings that used to register a posture before the
    /// open was admitted: an absent non-optional file is '35' (§9.1.13.6), a sibling's NO OTHER open is '61'
    /// (§9.1.13.9 item 1) — also after a RETRY phrase exhausts (§14.7.9.3) — and a second OPEN of an open connector
    /// is '41' (§14.9.27.4 GR2) that leaves the participation of the OPEN in force. None of them leaves the
    /// connector a participant it was not.</summary>
    [Theory]
    [InlineData("NoClauseSharingPhrase")]
    [InlineData("NoClauseRetryPhraseOnly")]
    [InlineData("NoClauseSharingAndRetryPhrases")]
    [InlineData("SelectSharingReadOnlyOnly")]
    [InlineData("LockModeReadOnlyClause")]
    public void UnsuccessfulOpensAnswerTheirStatusesAndConferNoParticipation(string caseName)
    {
        var c = CaseNamed(caseName);
        var framing = SharedExtendOpenDriftTests.Framing.FixedRecordSequential;

        // '35': the file is absent and not OPTIONAL.
        string absent = FileHarness.Tmp($"absent-{caseName}");
        var reg35 = new FileRegistry();
        RegisterSequential(reg35, "F", absent, framing);
        Assert.Equal("35", OpenAs(reg35, "F", c, FileOpenMode.Extend));
        Assert.False(Connector(reg35, "F").IsOpen);
        FileHarness.TryDelete(absent);

        // '61': a sibling holds the file NO OTHER (Table 19 refuses every opener); a RETRY phrase that exhausts
        // answers the conflict's own status.
        string host = FileHarness.Tmp($"busy-{caseName}");
        try
        {
            Seed(host, framing);
            var reg = new FileRegistry();
            RegisterSequential(reg, "G", host, framing);
            RegisterSequential(reg, "F", host, framing);
            reg.OpenStatic("G", FileOpenMode.IO);   // the implementor default of I-O is NO OTHER
            Assert.Equal("00", reg.Status("G"));
            Assert.Equal("61", OpenAs(reg, "F", c, FileOpenMode.Extend));
            Assert.False(Connector(reg, "F").IsOpen);
            Open(reg, "F", FileOpenMode.Extend, c.PhraseSharing, FileRetryKind.Times, 2);
            Assert.Equal("61", reg.Status("F"));
            reg.Close("G");

            // '41': the open that is still in force keeps its participation.
            Assert.Equal("00", reg.Status("G"));
            Open(reg, "F", FileOpenMode.Extend, c.PhraseSharing, c.Retry, c.Retry == FileRetryKind.None ? 0 : 3);
            Assert.Equal("00", reg.Status("F"));
            bool participates = Connector(reg, "F").SharedPhysical is not null;
            Assert.Equal(c.Participates, participates);
            Open(reg, "F", FileOpenMode.Extend, FileSharing.ReadOnly, FileRetryKind.Times, 1);
            Assert.Equal("41", reg.Status("F"));
            Assert.Equal(participates, Connector(reg, "F").SharedPhysical is not null);
            reg.Close("F");
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The posture is derived at EVERY open, so an OPEN's phrase leaves nothing behind for the next one: a
    /// connector opened EXTEND under a LOCK MODE clause and SHARING WITH READ ONLY participates; closed and reopened
    /// with SHARING WITH NO OTHER in the phrase (GR3), it does not; and a clause-less connector that carried a
    /// SHARING phrase on one open is a plain, lock-less open the next time (it used to stay registered for the
    /// life of the run unit).</summary>
    [Fact]
    public void ParticipationIsDerivedAtEachOpen_AndAPhraseLeavesNothingBehind()
    {
        var framing = SharedExtendOpenDriftTests.Framing.FixedRecordSequential;
        string host = FileHarness.Tmp("perop");
        try
        {
            Seed(host, framing);
            var reg = new FileRegistry();
            RegisterSequential(reg, "F", host, framing);
            reg.RegisterSharing("F", FileSharing.ReadOnly, FileLockMode.Manual, multiple: false);
            Open(reg, "F", FileOpenMode.Extend);
            Assert.NotNull(Connector(reg, "F").SharedPhysical);
            reg.Close("F");
            Open(reg, "F", FileOpenMode.Extend, FileSharing.NoOther);
            Assert.Equal("00", reg.Status("F"));
            Assert.Null(Connector(reg, "F").SharedPhysical);
            reg.Close("F");
            Open(reg, "F", FileOpenMode.Extend);
            Assert.NotNull(Connector(reg, "F").SharedPhysical);
            reg.Close("F");

            var plain = new FileRegistry();
            RegisterSequential(plain, "P", host, framing);
            Open(plain, "P", FileOpenMode.Extend, FileSharing.ReadOnly);
            Assert.Null(Connector(plain, "P").SharedPhysical);
            plain.Close("P");
            plain.OpenStatic("P", FileOpenMode.Extend);
            Assert.Equal("00", plain.Status("P"));
            Assert.Null(Connector(plain, "P").SharedPhysical);
            plain.Close("P");
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>Structurally, the participation fact and the per-statement lock question are ONE rule written
    /// once: <c>RecordLocksSetUnder</c> exists in the registry and both <c>LocksEffective</c> and the association
    /// call read it, and the OPEN phrase registers no posture of its own.</summary>
    [Fact]
    public void ParticipationAndLocksEffectiveShareOneRule()
    {
        string source = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "FileRegistry.cs"));
        string[] code = source.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)).ToArray();
        Assert.Single(code, l => l.Contains("bool RecordLocksSetUnder(", StringComparison.Ordinal));
        Assert.Contains(code, l => l.Contains("AssociatePhysical(", StringComparison.Ordinal)
            && l.Contains("RecordLocksSetUnder(", StringComparison.Ordinal));
        Assert.Equal(2, code.Count(l => l.Contains("RecordLocksSetUnder(", StringComparison.Ordinal)
            && !l.Contains("bool RecordLocksSetUnder(", StringComparison.Ordinal)));
        Assert.DoesNotContain(code, l => l.Contains("phraseParticipant", StringComparison.Ordinal));
        Assert.DoesNotContain(code, l => l.Contains("RegisterSharing(name, null, FileLockMode.None, false)", StringComparison.Ordinal));
    }

}
