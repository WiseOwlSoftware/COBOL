// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ ISO §9.1.15's FILE LOCK AGAINST OTHER RUN UNITS, AS TABLE 19 ITSELF (kb/Work PB833, PB2484). <i>"The successful
/// opening of a file establishes a file lock for the applicable sharing rules, thereby preventing other run units
/// from opening that file with incompatible sharing rules."</i> The applicable rules are rules 1)–3), which
/// <see cref="Table19"/> prints cell by cell, and a second run unit is bound to them exactly as a second connector
/// of this one is.
/// <para><b>The mechanism.</b> A connector's OPEN <i>publishes</i> its Table 19 column — the one cell-group an
/// OPEN request is later judged against (<see cref="Table19.Column"/>) — and then asks whether any OTHER holder
/// has published a column that its own request row refuses
/// (<see cref="Table19.Cell(OpenRequestRow, ExistingSharingColumn)"/>). It holds the column for the life of the OPEN and
/// gives it back with its CLOSE (§9.1.15, <i>"The file lock is removed by an explicit or implicit CLOSE
/// statement"</i>); a run unit that dies loses it with its descriptors. HOW a column is published and tested is the
/// host's (<see cref="IColumnLockHost"/>): a shared byte-range lock on the physical file on Linux
/// (<see cref="OfdColumnLockHost"/>), a lock file per holder beside it on every other Unix
/// (<see cref="SidecarColumnLockHost"/>), an exclusive byte of a range per column through a read handle of the holder's own
/// on Windows (<see cref="HandleColumnLockHost"/>, kb/Work PB3482); this class is the POLICY and is the same on all three.</para>
/// <para><b>Why publish first.</b> Two run units opening at once cannot both pass: whichever publishes second
/// finds the first already there, and a request against a column it refuses is answered '61' (§9.1.13.9 1)). It
/// is the ordering that makes the test race-free without a mutex, and it needs a test that ignores the asker's
/// OWN publication — which is what every host is chosen for, because a class that refuses its own kind (NO OTHER,
/// an OUTPUT) would otherwise refuse itself.</para>
/// <para><b>What it is NOT.</b> The table is read from <see cref="Table19"/>, never rewritten: the cross-run-unit
/// answer and the in-run-unit answer are one lookup, so they cannot drift (<c>RunUnitFileLockDriftTests</c> proves
/// it over every pair, on each host). A host share mode (<see cref="FileLockPosture"/>) is still taken on the connector's own
/// handle — it is the only thing a non-COBOL process meets, and on a host whose <see cref="FileShare"/> is
/// mandatory and per-access it already carries most of the table; this lock carries the rest, on the hosts that can
/// hold it. ⚠ It binds RUN UNITS: a process that does not take part in the protocol (a text editor, another
/// language) meets only the host share mode, which §9.1.15 permits the implementor to define ("Other facilities
/// may specify some degree of file sharing, however, their interaction with COBOL file sharing is defined by the
/// implementor", Annex A.1 item 75).</para>
/// <para><b>The protocol is a contract between run units</b> — two versions of this runtime must agree on it — so
/// the number of each column is spelled out below (<see cref="WireNumber"/>) and not derived from an enum's ordinal.</para>
/// </summary>
internal sealed class RunUnitFileLock : IDisposable
{
    /// <summary>The number a column is published under. ⛔ A WIRE VALUE: the byte at 2<sup>62</sup> + n of the physical
    /// file (<see cref="OfdColumnLockHost"/>) and the prefix of a holder's lock file (<see cref="SidecarColumnLockHost"/>);
    /// changing it splits the run units of two runtime versions into groups that cannot see each other.</summary>
    internal static int WireNumber(ExistingSharingColumn column) => column switch
    {
        ExistingSharingColumn.NoOtherAnyMode => 0,
        ExistingSharingColumn.ReadOnlyNonInput => 1,
        ExistingSharingColumn.ReadOnlyInput => 2,
        ExistingSharingColumn.AllOtherNonInput => 3,
        ExistingSharingColumn.AllOtherInput => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(column), $"{column} is not an ISO §14.9.27.4 Table 19 column"),
    };

    private static readonly ExistingSharingColumn[] Columns = Enum.GetValues<ExistingSharingColumn>();

    /// <summary>⛔ THE ONE CHOICE OF HOST, made once: Linux's open-file-description locks where the process shape
    /// carries them (<see cref="OfdRegionLocks.Available"/>); otherwise, off Windows, the lock files of
    /// <see cref="SidecarColumnLockHost"/>; and on Windows the byte ranges of <see cref="HandleColumnLockHost"/>. Its share
    /// modes are mandatory and per-access (<see cref="HostFile.ShareModesAreMandatory"/>), so the connector's own handle
    /// carries most of the table, but not all of it: a share mode cannot say that an INPUT opener owes writes
    /// (<see cref="FileLockPosture.WhenOwedWritesAreAdmitted"/>, kb/Work PB3482). A host is a fact of the process, so the
    /// platform question is asked HERE and never at a call site (kb/Work PB795).</summary>
    internal static IColumnLockHost HostOfThisProcess { get; } =
        OfdRegionLocks.Available ? OfdColumnLockHost.Instance
        : HostFile.ShareModesAreMandatory ? HandleColumnLockHost.Instance
        : SidecarColumnLockHost.Instance;

    private IDisposable? _hold;

    /// <summary>What the lock is established as — the host, the physical file and the Table 19 column the OPEN published
    /// and the columns its row refuses.</summary>
    private (IColumnLockHost Host, string HostPath, ExistingSharingColumn Own, ExistingSharingColumn[] Refused) _as;

    /// <summary>What the lock was before the last <see cref="Amend"/> replaced it — the column <see cref="Revert"/>
    /// publishes again; null when nothing has been amended.</summary>
    private (IColumnLockHost Host, string HostPath, ExistingSharingColumn Own, ExistingSharingColumn[] Refused)? _before;

    private RunUnitFileLock(IDisposable hold, (IColumnLockHost, string, ExistingSharingColumn, ExistingSharingColumn[]) taken) =>
        (_hold, _as) = (hold, taken);

    /// <summary>The column an OPEN in (<paramref name="sharing"/>, <paramref name="mode"/>) publishes and the columns its
    /// Table 19 row refuses — the one derivation <see cref="Take(IColumnLockHost?, string, FileSharing, FileOpenMode, out RunUnitFileLock?)"/>
    /// and <see cref="Amend"/> share.</summary>
    private static (ExistingSharingColumn Own, ExistingSharingColumn[] Refused) RequestOf(FileSharing sharing, FileOpenMode mode)
    {
        var row = Table19.Row(sharing, mode);
        return (Table19.Column(sharing, mode), Columns.Where(column => Table19.Cell(row, column) == OpenSharingOutcome.UnsuccessfulOpen).ToArray());
    }

    /// <summary>The outcome of asking for the lock.</summary>
    internal enum Outcome
    {
        /// <summary>The lock is held; <c>held</c> carries it.</summary>
        Held,
        /// <summary>Another run unit holds a column Table 19 refuses this request: §9.1.13.9 1)'s '61'.</summary>
        Refused,
        /// <summary>This host cannot hold the lock (<see cref="HostOfThisProcess"/>), or the file cannot be reached
        /// for it; the connector has the file lock its <see cref="FileShare"/> gives and nothing more.</summary>
        Unavailable,
    }

    /// <summary>Establish the file lock of an OPEN in (<paramref name="sharing"/>, <paramref name="mode"/>) on
    /// <paramref name="hostPath"/> through this process's host. <paramref name="held"/> is non-null exactly when the
    /// outcome is <see cref="Outcome.Held"/>; a refused or unavailable lock leaves nothing behind.</summary>
    internal static Outcome Take(string hostPath, FileSharing sharing, FileOpenMode mode, out RunUnitFileLock? held) =>
        Take(HostObserver.Value?.Invoke(HostOfThisProcess) ?? HostOfThisProcess, hostPath, sharing, mode, out held);

    /// <summary>A test's view of every request the connectors of the calling flow make of the host
    /// (<see cref="HostOfThisProcess"/>): it wraps the host a <see cref="Take(string, FileSharing, FileOpenMode, out RunUnitFileLock?)"/>
    /// establishes through, and the lock keeps that wrapper for its <see cref="Amend"/> and <see cref="Revert"/>. It is how
    /// a test COUNTS the requests an OPEN makes (<see cref="FileConnector.Open"/> makes at most two, kb/Work PB3498). An
    /// <see cref="AsyncLocal{T}"/>, so only the calling test's own flow observes (the pattern of
    /// <c>KeyedStore.MediumObserver</c>).</summary>
    internal static readonly AsyncLocal<Func<IColumnLockHost, IColumnLockHost>?> HostObserver = new();

    /// <summary>The same through <paramref name="host"/> (null: a host with no lock, <see cref="Outcome.Unavailable"/>).
    /// Every host is held to the same table by <c>RunUnitFileLockDriftTests</c>, whichever the process is running on.</summary>
    internal static Outcome Take(IColumnLockHost? host, string hostPath, FileSharing sharing, FileOpenMode mode,
        out RunUnitFileLock? held)
    {
        held = null;
        if (host is null) return Outcome.Unavailable;
        var (own, refused) = RequestOf(sharing, mode);
        var outcome = host.Establish(hostPath, own, refused, supersedes: null, out var hold);
        if (outcome == Outcome.Held) held = new RunUnitFileLock(hold!, (host, hostPath, own, refused));
        return outcome;
    }

    /// <summary>⛔ REPLACE THIS LOCK BY THE ONE AN OPEN IN (<paramref name="sharing"/>, <paramref name="mode"/>) ESTABLISHES,
    /// WITH NO INSTANT IN WHICH THE FILE IS UNLOCKED (kb/Work PB3483; <see cref="IColumnLockHost.Establish"/>'s
    /// <c>supersedes</c>). The one caller is a file lock a CLOSE under commit control KEPT: §14.9.7.4 GR4, <i>"For files
    /// specified in active APPLY COMMIT clauses that have been closed and not reopened prior to the COMMIT statement, the
    /// file locks are released"</i>, so the connector's own re-OPEN, which establishes a lock of its own that the kept one
    /// would refuse as another holder's, must not give the kept one back first and take it again if the OPEN fails
    /// (§14.9.27.4 GR25, <i>"If the execution of the OPEN statement is unsuccessful, the file is not affected"</i>): that
    /// left the file unlocked for two instants, in which another run unit's OPEN against the kept column was admitted.
    /// <see cref="Outcome.Held"/>: this lock now holds the new column, and <see cref="Revert"/> can publish the old one
    /// again. <see cref="Outcome.Refused"/> or <see cref="Outcome.Unavailable"/>: the lock is exactly as it was.</summary>
    internal Outcome Amend(FileSharing sharing, FileOpenMode mode)
    {
        var (own, refused) = RequestOf(sharing, mode);
        var outcome = _as.Host.Establish(_as.HostPath, own, refused, _hold, out var hold);
        if (outcome != Outcome.Held) return outcome;
        (_before, _as, _hold) = (_as, (_as.Host, _as.HostPath, own, refused), hold);
        return outcome;
    }

    /// <summary>Does another run unit's connector hold an INPUT column (ALL OTHER or READ ONLY input) — the only holders that
    /// can owe writes while open in the input mode (<see cref="FileLockPosture.WhenOwedWritesAreAdmitted"/>, kb/Work PB3482)?
    /// Answered by a host whose share modes are mandatory (<see cref="HandleColumnLockHost"/>); false for any other and for a
    /// lock that was given back.</summary>
    internal bool AnotherInputOpenerHolds() =>
        _hold is IPublishedColumn published
        && (published.AnotherHolds(ExistingSharingColumn.AllOtherInput) || published.AnotherHolds(ExistingSharingColumn.ReadOnlyInput));

    /// <summary>Is this lock held through an open handle on the physical file (<see cref="IPublishedColumn.HoldsAHandle"/>:
    /// the Windows column holder)? Such a handle is its owner's own, which a probe for another's handle must admit.</summary>
    internal bool HoldsAHandle => _hold is IPublishedColumn { HoldsAHandle: true };

    /// <summary>Publish again the column this lock held before <see cref="Amend"/>, with no test — the amended OPEN was
    /// unsuccessful and §14.9.27.4 GR25 leaves the file unaffected, so the kept lock stands as the CLOSE left it. A holder
    /// the new column admitted in the meantime was admitted by an OPEN in progress, which is the OPEN's own instant and not a
    /// window. False (and the lock given back) when the host cannot publish it again.</summary>
    internal bool Revert()
    {
        if (_before is not { } before) return true;
        if (before.Host.Establish(before.HostPath, before.Own, [], _hold, out var hold) != Outcome.Held)
        {
            Dispose();
            return false;
        }
        (_as, _hold, _before) = (before, hold, null);
        return true;
    }

    /// <summary>Remove the file lock — the CLOSE's half of §9.1.15. Idempotent.</summary>
    public void Dispose()
    {
        _hold?.Dispose();
        _hold = null;
    }
}
