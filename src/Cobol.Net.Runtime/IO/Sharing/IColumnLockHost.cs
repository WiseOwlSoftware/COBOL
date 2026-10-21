// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE HOST'S WORDS FOR ISO §9.1.15's FILE LOCK AGAINST OTHER RUN UNITS (kb/Work PB833, PB2484): publish a Table 19
/// column, and ask whether any OTHER holder has published a column the request row refuses. The POLICY — which column
/// an OPEN publishes, which columns its row refuses, what a refusal means — is <see cref="RunUnitFileLock"/>'s and is
/// read from <see cref="Table19"/>; a host only carries it. One host per process family
/// (<see cref="RunUnitFileLock.HostOfThisProcess"/>):
/// <list type="bullet">
/// <item><see cref="OfdColumnLockHost"/> — Linux open-file-description byte-range locks on the physical file.</item>
/// <item><see cref="SidecarColumnLockHost"/> — one advisory lock file per holder beside the physical file, for every
/// Unix that has no open-file-description locks (macOS above all).</item>
/// <item><see cref="HandleColumnLockHost"/> — Windows byte-range locks over a range per column, through a read handle of
/// the holder's own (kb/Work PB3482). Its share modes are mandatory and per-access
/// (<see cref="HostFile.ShareModesAreMandatory"/>), so the connector's own handle carries most of the table, and the
/// OUTPUT probe the truncation; the columns carry the rest: what a share mode cannot say, that an INPUT opener owes writes
/// (<see cref="FileLockPosture.WhenOwedWritesAreAdmitted"/>).</item>
/// </list>
/// <para>⛔ THE CONTRACT EVERY HOST KEEPS: the column is published BEFORE the others are tested, and a holder never
/// reports ITSELF as another, so two OPENs at the same instant cannot both pass — whichever completes its publication
/// second finds the first. A refusal or an unavailable lock leaves nothing published. The five columns' wire numbers
/// are a contract between run units (<see cref="RunUnitFileLock.WireNumber"/>), so two versions of the runtime agree.</para>
/// </summary>
internal interface IColumnLockHost
{
    /// <summary>Publish <paramref name="own"/> on the physical file at <paramref name="hostPath"/>, then test every
    /// column in <paramref name="refused"/> for another holder. <paramref name="hold"/> is non-null exactly when the
    /// outcome is <see cref="RunUnitFileLock.Outcome.Held"/>; disposing it gives the lock back (the CLOSE's half of
    /// §9.1.15). Never throws: a host failure to publish or test is <see cref="RunUnitFileLock.Outcome.Unavailable"/>.
    /// <para>⛔ <paramref name="supersedes"/> is a hold THIS holder took from this host on the same file earlier
    /// (null: none) and the request REPLACES it with no instant in which the file is unlocked: the new column is
    /// published while the old stays published, the test ignores both (they are the asker's own), and only when the
    /// outcome is <see cref="RunUnitFileLock.Outcome.Held"/> is the old one given back — <paramref name="hold"/> is then
    /// the replacement, and <paramref name="supersedes"/> must not be disposed by the caller (the host may have reused
    /// it as <paramref name="hold"/>). On a refusal or an unavailable lock <paramref name="supersedes"/> is exactly as it
    /// was, still held (kb/Work PB3483: a connector's re-OPEN after a CLOSE under commit control must keep the file lock
    /// the CLOSE kept until the COMMIT or ROLLBACK, §14.9.7.4 GR4, whatever the re-OPEN meets).</para></summary>
    RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own,
        IReadOnlyList<ExistingSharingColumn> refused, IDisposable? supersedes, out IDisposable? hold);
}

/// <summary>A published Table 19 column that can also be asked who else holds one (kb/Work PB3482). Only a host whose share
/// modes are mandatory needs the question — <see cref="FileConnector.Open"/> asks it on its second attempt — so only that
/// host's hold (<see cref="HandleColumnLockHost"/>) is one; the other hosts' holds are plain <see cref="IDisposable"/>s and
/// <see cref="RunUnitFileLock.AnotherInputOpenerHolds"/> answers false for them.</summary>
internal interface IPublishedColumn : IDisposable
{
    /// <summary>Does a holder other than this one hold <paramref name="column"/>? False once the column was given back.</summary>
    bool AnotherHolds(ExistingSharingColumn column);

    /// <summary>Is the hold itself an open handle on the physical file? Such a handle is the connector's OWN, and a probe
    /// that every outstanding handle refuses (<see cref="HostFile.IsHeldByAnother"/>) meets it as if it were another's
    /// (<c>FileConnector.HeldByAnotherHandle</c>).</summary>
    bool HoldsAHandle { get; }
}
