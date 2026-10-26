// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// ⛔ THE WINDOWS HOST OF <see cref="RunUnitFileLock"/> (kb/Work PB3482): Table 19 as a lock between run units on the one
/// host whose share modes are mandatory and per access. ISO §9.1.15's file lock <i>"prevent[s] other run units from
/// opening that file with incompatible sharing rules"</i>, and a Windows share mode can say that only as far as an
/// access can: <see cref="FileShare.Read"/> refuses every writer, so a connector under SHARING WITH READ ONLY that
/// meets an INPUT opener which still owes its transaction's writes (a handle open for writing, 2.2.1.10 in
/// docs/rearchitecture/DESIGN-runtime-library.md) is refused by the HOST although Table 19 prints <i>Normal open</i> — the
/// wrong answer '61' this class exists to correct. With the columns published it is the table that judges, and the share
/// mode yields exactly where the table says it must (<see cref="FileLockPosture.WhenOwedWritesAreAdmitted"/>).
/// <para><b>The mechanism.</b> A holder opens a read handle of its own (<see cref="HostFile.OpenColumnHolder"/>) and
/// PUBLISHES its column by holding exclusively one byte of that column's range
/// (<see cref="HostRegionLocks.ColumnRangeStart"/>), chosen at random: Windows locks belong to a handle and conflict with
/// every OTHER handle, so "does any holder but me hold this column" is a test of the whole range with the holder's own
/// byte left out — two shared requests, one each side of it — and a shared request conflicts only with an exclusive hold,
/// so two testers never exclude each other. It publishes first and tests second, exactly as the Linux host does
/// (<see cref="OfdColumnLockHost"/>): two OPENs at the same instant cannot both pass, because whichever publishes second
/// finds the first (both may be refused, which is a refused OPEN and no violated lock). The lock dies with the handle, so
/// a run unit that is killed releases it.</para>
/// <para><b>The owed-writes byte</b> (kb/Work PB3499). A holder whose connector is an INPUT opener with a handle open for
/// WRITING (<see cref="IColumnLockHost.Establish"/>'s <c>owesWrites</c>) holds exclusively one byte more, of the range
/// <see cref="HostRegionLocks.OwedWritesRangeStart"/>, chosen and tested exactly as a column's is. It is the one fact that
/// lets a share mode yield to the right writer: <see cref="FileConnector.Open"/>'s second attempt asks
/// <see cref="IPublishedColumn.AnotherOwesWrites"/>, a test of that range with the asker's own byte left out, and a reader
/// that merely holds an INPUT column publishes nothing there, so it is no reason to admit a program outside the protocol.</para>
/// <para><b>SHARING WITH NO OTHER publishes nothing</b> (<see cref="RunUnitFileLock.Outcome.Unavailable"/>): its own handle
/// is <see cref="FileShare.None"/>, which refuses this host's read handle, and refuses every other open — a column
/// would add nothing the share mode does not already say. A holder that meets such a connector is refused: its read
/// handle is refused by the share mode, which is Table 19's NO OTHER column (every request row refuses it).</para>
/// <para><b>A superseding request</b> (<see cref="IColumnLockHost.Establish"/>'s <c>supersedes</c>, a kept lock's
/// re-OPEN: kb/Work PB3483) publishes through the superseded hold's OWN handle, as the Linux host reuses its description:
/// the new byte is held beside the old one, the test leaves both out, and the old byte is unlocked only once the new
/// column stands — no instant in which the file is unlocked. ⚠ The one exception is a re-OPEN under SHARING WITH NO
/// OTHER, whose own <see cref="FileShare.None"/> handle the holder's handle would refuse: after the test passes the holder
/// gives way (<see cref="RunUnitFileLock.Outcome.Held"/>, a hold that publishes nothing), and the instant between that and the connector's
/// own handle is the share-mode window <c>KeyedConnector.RetakeKeptHandle</c> names for the kept handle, which a host
/// whose share modes are per handle cannot close.</para>
/// </summary>
internal sealed class HandleColumnLockHost : IColumnLockHost
{
    internal static readonly HandleColumnLockHost Instance = new();

    /// <summary>How often a publication chooses another byte after a collision with a holder or a tester (a tester's
    /// shared request conflicts with an exclusive attempt for the instant it lasts). Exhausting them is a host that cannot
    /// be trusted here, not a refusal.</summary>
    private const int PublishAttempts = 64;

    /// <inheritdoc/>
    /// <remarks>⛔ <paramref name="owesWrites"/> is published as one more exclusive byte, of the OWED-WRITES range
    /// (<see cref="HostRegionLocks.OwedWritesRangeStart"/>), through the same handle and in the same request as the column: a
    /// request that cannot publish it is <see cref="RunUnitFileLock.Outcome.Unavailable"/> (<see cref="RunUnitFileLock.Outcome.Refused"/>
    /// when a column Table 19 refuses it is held: <see cref="Unpublishable"/>) and leaves nothing published. A
    /// superseding request keeps the superseded hold's byte when it owes writes too, publishes one when only it does, and
    /// gives the old one back, after the new state stands, when only the superseded hold did.</remarks>
    public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own, bool owesWrites,
        IReadOnlyList<ExistingSharingColumn> refused, IDisposable? supersedes, out IDisposable? hold)
    {
        hold = null;
        // A witness may force this host on a machine that is not Windows: it meets "this host cannot hold the lock here".
        if (!OperatingSystem.IsWindows()) return RunUnitFileLock.Outcome.Unavailable;
        var previous = supersedes is Published { Handle: not null } live ? live : null;
        if (own == ExistingSharingColumn.NoOtherAnyMode)
            return previous is null ? RunUnitFileLock.Outcome.Unavailable : GiveWay(previous, refused, out hold);

        SafeFileHandle handle;
        if (previous is not null) handle = previous.Handle!;
        else
            try { handle = HostFile.OpenColumnHolder(hostPath); }
            // A holder that excludes readers (SHARING WITH NO OTHER, or a foreign process that does): Table 19's first
            // column, which every request row refuses.
            catch (IOException e) when (HostFile.IsSharingRefusal(e)) { return RunUnitFileLock.Outcome.Refused; }
            // An OPEN has only I-O statuses as outcomes (SharedExtendOpenDriftTests): a file this host cannot reach for it
            // (absent, refused, a device name) is a lock it cannot hold here, never an escaping exception.
            catch (IOException) { return RunUnitFileLock.Outcome.Unavailable; }
            catch (UnauthorizedAccessException) { return RunUnitFileLock.Outcome.Unavailable; }
            catch (ArgumentException) { return RunUnitFileLock.Outcome.Unavailable; }
            catch (NotSupportedException) { return RunUnitFileLock.Outcome.Unavailable; }

        bool kept = false;
        long added = -1, addedOwed = -1;
        try
        {
            if (previous is null && !WindowsByteRangeLocks.IsDiskFile(handle)) return RunUnitFileLock.Outcome.Unavailable;   // only a disk file is locked
            long publishedByte;
            if (previous is not null && previous.Own == own) publishedByte = previous.Byte;   // the column stands as it is
            else
            {
                var columnTaken = Publish(handle, HostRegionLocks.ColumnRangeStart(own), out publishedByte);
                if (columnTaken != RegionLockResult.Ok) return Unpublishable(handle, columnTaken, refused, OwnBytes(previous, own, -1));
                added = publishedByte;
            }

            // The owed-writes byte: the superseded hold's when this request owes writes too, a new one when only this request
            // does. One the superseded hold had and this request does not owe goes back with the old column, below.
            long supersededOwed = previous?.OwedByte ?? -1;
            long owedByte = owesWrites ? supersededOwed : -1;
            if (owesWrites && owedByte < 0)
            {
                var owedTaken = Publish(handle, HostRegionLocks.OwedWritesRangeStart, out owedByte);
                if (owedTaken != RegionLockResult.Ok) return Unpublishable(handle, owedTaken, refused, OwnBytes(previous, own, added));
                addedOwed = owedByte;
            }

            if (AnotherHoldsARefusedColumn(handle, refused, OwnBytes(previous, own, added))) return RunUnitFileLock.Outcome.Refused;

            if (previous is null) hold = new Published(handle, own, publishedByte, owedByte);
            else
            {
                // The replacement stands; only now does what it replaced go.
                if (previous.Own != own) WindowsByteRangeLocks.Unlock(handle, previous.Byte, 1);
                if (!owesWrites && supersededOwed >= 0) WindowsByteRangeLocks.Unlock(handle, supersededOwed, 1);
                previous.Move(own, publishedByte, owedByte);
                hold = previous;
            }
            kept = true;
            return RunUnitFileLock.Outcome.Held;
        }
        finally
        {
            if (!kept)
            {
                // A superseded hold stays as it was: only the bytes this request added are given back — unlocked
                // explicitly before a handle of this request's own is closed (kb/Work PB3549: never left to the close).
                if (added >= 0) WindowsByteRangeLocks.Unlock(handle, added, 1);
                if (addedOwed >= 0) WindowsByteRangeLocks.Unlock(handle, addedOwed, 1);
                if (previous is null) handle.Dispose();
            }
        }
    }

    /// <summary>The bytes a request holds in the columns' ranges: the superseded hold's column byte (a re-OPEN's old column
    /// stands beside the new one until the new one does) and the byte this request added to its own column
    /// (<paramref name="added"/>, -1 when it has added none). The test of the refused columns leaves out exactly these.</summary>
    private static (ExistingSharingColumn, long)[] OwnBytes(Published? previous, ExistingSharingColumn own, long added)
    {
        var bytes = new List<(ExistingSharingColumn, long)>(2);
        if (previous is not null) bytes.Add((previous.Own, previous.Byte));
        if (added >= 0) bytes.Add((own, added));
        return [.. bytes];
    }

    /// <summary>Does a holder other than the asker hold a column Table 19 refuses the request? The one test an established
    /// lock and a publication that could not be made are both judged by.</summary>
    [SupportedOSPlatform("windows")]
    private static bool AnotherHoldsARefusedColumn(SafeFileHandle handle, IReadOnlyList<ExistingSharingColumn> refused,
        ReadOnlySpan<(ExistingSharingColumn Column, long Byte)> ownBytes)
    {
        foreach (var column in refused)
            if (OthersHold(handle, column, ownBytes, whenUnobservable: true)) return true;
        return false;
    }

    /// <summary>⛔ A PUBLICATION THAT RAN OUT OF TRIES IS A LOCK THIS HOST CANNOT HOLD, NOT A REASON TO ADMIT WHAT TABLE 19
    /// REFUSES (kb/Work PB3518): the request cannot be held (<see cref="RunUnitFileLock.Outcome.Unavailable"/>, and the OPEN
    /// goes on with the share modes alone), yet a column another run unit has already published and Table 19 refuses this
    /// request is a conflict this host CAN see, so it is <see cref="RunUnitFileLock.Outcome.Refused"/> exactly as after a
    /// publication (§9.1.13.9 1)'s '61'). The converse never holds: no request is refused for failing to publish. The
    /// same two answers as <see cref="SidecarColumnLockHost"/> gives a lock directory this user may not write.
    /// <para><paramref name="failed"/> says WHY the publication failed. <see cref="RegionLockResult.Held"/> is an exhausted
    /// publication on a handle that carries locks, so the columns are there to be seen. <see cref="RegionLockResult.Unavailable"/>
    /// is a handle or filesystem that carries no byte-range lock (<see cref="IColumnLockHost.Establish"/>: a host failure to
    /// publish or test is Unavailable): the test of the refused columns fails the same way, and its "an unobservable
    /// holder is a holder" answer would refuse an OPEN no other connector contradicts, so nothing is tested.</para></summary>
    [SupportedOSPlatform("windows")]
    internal static RunUnitFileLock.Outcome Unpublishable(SafeFileHandle handle, RegionLockResult failed,
        IReadOnlyList<ExistingSharingColumn> refused, ReadOnlySpan<(ExistingSharingColumn Column, long Byte)> ownBytes) =>
        failed == RegionLockResult.Held && AnotherHoldsARefusedColumn(handle, refused, ownBytes)
            ? RunUnitFileLock.Outcome.Refused : RunUnitFileLock.Outcome.Unavailable;

    /// <summary>Hold exclusively one byte of the range starting at <paramref name="rangeStart"/> (a column's, or the
    /// owed-writes range) through <paramref name="handle"/>, chosen at random: <see cref="RegionLockResult.Ok"/> with the
    /// byte in <paramref name="published"/>; <see cref="RegionLockResult.Held"/> when every try collided with a holder or a
    /// tester; <see cref="RegionLockResult.Unavailable"/> when the handle carries no lock. The last two leave
    /// <paramref name="published"/> -1.</summary>
    [SupportedOSPlatform("windows")]
    private static RegionLockResult Publish(SafeFileHandle handle, long rangeStart, out long published) =>
        Publish(rangeStart, candidate => WindowsByteRangeLocks.Lock(handle, candidate, 1, exclusive: true, wait: false), out published);

    /// <summary>The publication loop over <paramref name="tryLock"/> (one exclusive byte at a candidate): see
    /// <see cref="Publish(SafeFileHandle, long, out long)"/>.</summary>
    internal static RegionLockResult Publish(long rangeStart, Func<long, RegionLockResult> tryLock, out long published)
    {
        published = -1;
        for (int attempt = 0; attempt < PublishAttempts; attempt++)
        {
            // A tester's shared request over the range lasts microseconds: the first retries yield, the later ones wait
            // a millisecond, so a run of tries cannot all fall inside one tester's instant.
            if (attempt > 0) { if (attempt < 8) Thread.Yield(); else Thread.Sleep(1); }
            long candidate = rangeStart + Random.Shared.NextInt64(HostRegionLocks.ColumnRangeWidth);
            switch (tryLock(candidate))
            {
                case RegionLockResult.Ok: published = candidate; return RegionLockResult.Ok;
                case RegionLockResult.Unavailable: return RegionLockResult.Unavailable;
            }
        }
        return RegionLockResult.Held;
    }

    /// <summary>A superseding request under SHARING WITH NO OTHER (see the class remarks): test every column it refuses
    /// through the superseded hold, its own byte left out, and only when no other holder stands give that hold back — its
    /// handle would refuse the connector's own <see cref="FileShare.None"/> one — answering <see cref="RunUnitFileLock.Outcome.Held"/>
    /// with a hold that publishes nothing (<see cref="RunUnitFileLock.Revert"/> publishes the old column again through a
    /// handle of its own). A refusal leaves the superseded hold exactly as it was.</summary>
    [SupportedOSPlatform("windows")]
    private static RunUnitFileLock.Outcome GiveWay(Published previous, IReadOnlyList<ExistingSharingColumn> refused, out IDisposable? hold)
    {
        hold = null;
        if (AnotherHoldsARefusedColumn(previous.Handle!, refused, OwnBytes(previous, ExistingSharingColumn.NoOtherAnyMode, -1)))
            return RunUnitFileLock.Outcome.Refused;
        previous.Dispose();
        hold = new Published(null, ExistingSharingColumn.NoOtherAnyMode, -1, -1);
        return RunUnitFileLock.Outcome.Held;
    }

    /// <summary>Does a holder OTHER than the one whose bytes are <paramref name="ownBytes"/> hold <paramref name="column"/>?
    /// The holder's own bytes in the column's range are left out: shared requests over the stretches between them.
    /// <paramref name="whenUnobservable"/> is the answer for a range this host cannot carry the request over: the
    /// establishment refuses (a holder this host cannot observe is a holder), and the question that would let a share mode
    /// yield does not (an unknown is never a reason to admit).</summary>
    [SupportedOSPlatform("windows")]
    private static bool OthersHold(SafeFileHandle handle, ExistingSharingColumn column,
        ReadOnlySpan<(ExistingSharingColumn Column, long Byte)> ownBytes, bool whenUnobservable)
    {
        Span<long> skipped = stackalloc long[ownBytes.Length];
        int count = 0;
        foreach (var (ownColumn, ownByte) in ownBytes)
            if (ownColumn == column) skipped[count++] = ownByte;
        return OthersHoldRange(handle, HostRegionLocks.ColumnRangeStart(column), skipped[..count], whenUnobservable);
    }

    /// <summary>Does a handle other than the asker's hold a byte of the range starting at <paramref name="rangeStart"/>
    /// (<see cref="HostRegionLocks.ColumnRangeWidth"/> wide), the asker's own bytes (<paramref name="ownBytes"/>, in any order)
    /// left out? The one question both a column and the owed-writes range are asked by.</summary>
    [SupportedOSPlatform("windows")]
    private static bool OthersHoldRange(SafeFileHandle handle, long rangeStart, Span<long> ownBytes, bool whenUnobservable)
    {
        long end = rangeStart + HostRegionLocks.ColumnRangeWidth;
        ownBytes.Sort();
        long from = rangeStart;
        foreach (long ownByte in ownBytes)
        {
            if (RangeIsHeld(handle, from, ownByte - from, whenUnobservable)) return true;
            from = ownByte + 1;
        }
        return RangeIsHeld(handle, from, end - from, whenUnobservable);
    }

    /// <summary>Does another handle hold an exclusive byte of the range? A shared request for the whole range, released at
    /// once; an empty range holds nothing; a host that cannot carry the request answers <paramref name="whenUnobservable"/>.</summary>
    [SupportedOSPlatform("windows")]
    private static bool RangeIsHeld(SafeFileHandle handle, long start, long length, bool whenUnobservable)
    {
        if (length <= 0) return false;
        var taken = WindowsByteRangeLocks.Lock(handle, start, length, exclusive: false, wait: false);
        if (taken == RegionLockResult.Ok) WindowsByteRangeLocks.Unlock(handle, start, length);
        return taken == RegionLockResult.Held || (taken == RegionLockResult.Unavailable && whenUnobservable);
    }

    /// <summary>The published column: disposing it unlocks its bytes (the column's and the owed-writes one) and closes the handle. Idempotent. A superseding request
    /// moves it to its own column (<see cref="Move"/>); a hold with no handle publishes nothing (a re-OPEN under SHARING
    /// WITH NO OTHER, <see cref="GiveWay"/>).</summary>
    private sealed class Published(SafeFileHandle? handle, ExistingSharingColumn own, long publishedByte, long owedByte) : IPublishedColumn
    {
        /// <summary>The holder's handle, or null once given back (or when it publishes nothing).</summary>
        internal SafeFileHandle? Handle { get; private set; } = handle;

        /// <summary>The column published now.</summary>
        internal ExistingSharingColumn Own { get; private set; } = own;

        /// <summary>The byte of <see cref="Own"/>'s range the handle holds exclusively.</summary>
        internal long Byte { get; private set; } = publishedByte;

        /// <summary>The byte of the owed-writes range the handle holds exclusively (<see cref="IColumnLockHost.Establish"/>'s
        /// <c>owesWrites</c>), or -1 when its connector owes none.</summary>
        internal long OwedByte { get; private set; } = owedByte;

        /// <summary>A superseding request published <paramref name="column"/> at <paramref name="at"/> and the owed-writes state
        /// <paramref name="owed"/> (a byte, or -1), and gave back what it replaced.</summary>
        internal void Move(ExistingSharingColumn column, long at, long owed) => (Own, Byte, OwedByte) = (column, at, owed);

        public bool HoldsAHandle => Handle is { IsClosed: false };

        public bool AnotherOwesWrites()
        {
            if (Handle is not { IsClosed: false } held || !OperatingSystem.IsWindows()) return false;
            Span<long> own = stackalloc long[1];
            int count = 0;
            if (OwedByte >= 0) own[count++] = OwedByte;
            return OthersHoldRange(held, HostRegionLocks.OwedWritesRangeStart, own[..count], whenUnobservable: false);
        }

        /// <summary>Unlock the published bytes, THEN close the handle (kb/Work PB3549): Windows documents that the time it
        /// takes to unlock a closed handle's locks "depends upon available system resources", so a successor must not be
        /// refused for a column already given back.</summary>
        public void Dispose()
        {
            if (Handle is { IsClosed: false } held && OperatingSystem.IsWindows())
            {
                WindowsByteRangeLocks.Unlock(held, Byte, 1);
                if (OwedByte >= 0) WindowsByteRangeLocks.Unlock(held, OwedByte, 1);
            }
            Handle?.Dispose();
            Handle = null;
        }
    }
}
