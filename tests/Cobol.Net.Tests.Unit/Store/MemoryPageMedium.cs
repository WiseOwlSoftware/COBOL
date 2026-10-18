// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Tests.Unit.Store;

/// <summary>A process death injected by <see cref="MemoryPageMedium"/>: the run unit stops at that instant, keeping every
/// write it handed over (2.2.1.16: "a process death keeps every write handed to the operating environment, in order").
/// No production code catches it — it is not an <see cref="IOException"/> — so the store's memory dies with it.</summary>
internal sealed class SimulatedDeath : Exception
{
    public SimulatedDeath() : base("simulated process death") { }
}

/// <summary>One write the medium received.</summary>
internal sealed record MediumWrite(long Seq, string Unit, uint Page, ulong Generation, PageKind Kind)
{
    /// <summary>Whether a sync after it made it durable.</summary>
    public bool Synced { get; set; }
}

/// <summary>One sync: every write up to <see cref="UpToSeq"/> is durable.</summary>
internal sealed record MediumSync(long UpToSeq, string Unit);

/// <summary>⛔ THE DETERMINISTIC FAULT-INJECTING PAGE MEDIUM of docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12
/// (kb/Work PB3331): an in-memory page array that logs every write and sync, and can LOSE every unsynced write or any
/// chosen subset of them (a power loss), KEEP them (a process death: nothing to do), TEAR one named page, or SERVE AN
/// OLDER VERSION of one named page (a lost write over a reused page). Each run unit reaches it through its own
/// <see cref="Handle"/>, whose fault plan kills or refuses that run unit at its n-th write or sync — so two run units
/// interleave at every step deterministically, on every host, with no VM.</summary>
internal sealed class MemoryPageMedium
{
    private sealed class PageVersions
    {
        /// <summary>Every version the page was written as, oldest first; the last is what a read serves.</summary>
        public readonly List<(byte[] Bytes, long Seq)> Versions = [];

        /// <summary>The newest version a sync made durable (-1: none; the page reads as zeros after a power loss).</summary>
        public int Durable = -1;
    }

    private readonly Dictionary<uint, PageVersions> _pages = [];
    private readonly List<MediumWrite> _writes = [];
    private readonly List<MediumSync> _syncs = [];
    private long _length;   // in pages
    private long _durableLength;
    private long _seq;

    /// <summary>Every write, in order.</summary>
    public IReadOnlyList<MediumWrite> Writes => _writes;

    /// <summary>Every sync, in order.</summary>
    public IReadOnlyList<MediumSync> Syncs => _syncs;

    /// <summary>The medium's length in pages.</summary>
    public long PageCount => _length;

    /// <summary>A run unit's handle onto the medium.</summary>
    public Handle Open(string unit) => new(this, unit);

    /// <summary>The bytes a read of <paramref name="page"/> serves now.</summary>
    public byte[] Snapshot(uint page)
    {
        var bytes = new byte[PageHeader.PageSize];
        if (_pages.TryGetValue(page, out var v) && v.Versions.Count > 0) v.Versions[^1].Bytes.CopyTo(bytes, 0);
        return bytes;
    }

    /// <summary>The bytes <paramref name="page"/> held just after write <paramref name="seq"/> (zeros before its first
    /// surviving write) — what an auditor reconstructs to ask which pages the slots referenced at an instant.</summary>
    public byte[] SnapshotAt(uint page, long seq)
    {
        var bytes = new byte[PageHeader.PageSize];
        if (_pages.TryGetValue(page, out var v))
            for (int i = v.Versions.Count - 1; i >= 0; i--)
                if (v.Versions[i].Seq <= seq)
                {
                    v.Versions[i].Bytes.CopyTo(bytes, 0);
                    break;
                }
        return bytes;
    }

    /// <summary>The sequence number of the newest write so far.</summary>
    public long LastSeq => _seq;

    /// <summary>A POWER LOSS: every write no sync made durable is lost, except those <paramref name="keep"/> keeps (an
    /// unordered subset, 2.2.1.16's power-loss variant). With no predicate, every unsynced write is lost.</summary>
    public void PowerLoss(Func<MediumWrite, bool>? keep = null)
    {
        var kept = _writes.Where(w => !w.Synced && keep is not null && keep(w)).Select(w => w.Seq).ToHashSet();
        foreach (var (_, v) in _pages)
        {
            var survivors = new List<(byte[] Bytes, long Seq)>();
            for (int i = 0; i < v.Versions.Count; i++)
                if (i <= v.Durable || kept.Contains(v.Versions[i].Seq)) survivors.Add(v.Versions[i]);
            v.Versions.Clear();
            v.Versions.AddRange(survivors);
            v.Durable = v.Versions.Count - 1;
        }
        long keptLength = _pages.Where(p => p.Value.Versions.Count > 0).Select(p => (long)p.Key + 1).DefaultIfEmpty(0).Max();
        _length = Math.Max(_durableLength, keptLength);
        _durableLength = _length;
        foreach (var w in _writes) w.Synced = true;   // what survived is now simply the medium's content
    }

    /// <summary>TEAR <paramref name="page"/>: its second half reverts to the previous version (or zeros), as a write
    /// interrupted half-way leaves it; the checksum no longer holds.</summary>
    public void Tear(uint page)
    {
        var v = _pages[page];
        var torn = (byte[])v.Versions[^1].Bytes.Clone();
        var older = v.Versions.Count > 1 ? v.Versions[^2].Bytes : new byte[PageHeader.PageSize];
        Array.Copy(older, PageHeader.PageSize / 2, torn, PageHeader.PageSize / 2, PageHeader.PageSize / 2);
        if (torn.AsSpan().SequenceEqual(v.Versions[^1].Bytes)) torn[PageHeader.PageSize - 1] ^= 0xFF;
        v.Versions[^1] = (torn, v.Versions[^1].Seq);
    }

    /// <summary>SERVE AN OLDER VERSION of <paramref name="page"/>: its newest write is lost (a lost write over a reused
    /// page), so a read serves the version before it — whole, with its own checksum, and an older generation.</summary>
    public void ServeOlder(uint page)
    {
        var v = _pages[page];
        v.Versions.RemoveAt(v.Versions.Count - 1);
        v.Durable = Math.Min(v.Durable, v.Versions.Count - 1);
    }

    /// <summary>Write raw bytes as <paramref name="unit"/> would, bypassing every fault: a test composing a state the
    /// design names (a page 2 carrying a claim with <c>committing</c> set, say).</summary>
    public void Poke(uint page, byte[] bytes) => Apply("poke", page, bytes);

    private void Apply(string unit, uint page, ReadOnlySpan<byte> source)
    {
        var bytes = source[..PageHeader.PageSize].ToArray();
        long seq = ++_seq;
        if (!_pages.TryGetValue(page, out var v)) _pages[page] = v = new PageVersions();
        v.Versions.Add((bytes, seq));
        _length = Math.Max(_length, (long)page + 1);
        _writes.Add(new MediumWrite(seq, unit, page, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(8)), (PageKind)bytes[0]));
    }

    private void SyncAll(string unit)
    {
        foreach (var v in _pages.Values) v.Durable = v.Versions.Count - 1;
        foreach (var w in _writes) w.Synced = true;
        _durableLength = _length;
        _syncs.Add(new MediumSync(_seq, unit));
    }

    /// <summary>What a fault does when it fires.</summary>
    public enum Fault
    {
        /// <summary>The run unit dies at the operation: <see cref="SimulatedDeath"/>, the operation not performed.</summary>
        Death,

        /// <summary>The medium refuses the operation (a full medium, a revoked handle, a failed sync):
        /// <see cref="IOException"/>, the operation not performed.</summary>
        Refuse,

        /// <summary>The run unit dies just AFTER the operation was performed.</summary>
        DeathAfter,
    }

    /// <summary>One run unit's handle: an <see cref="IPageMedium"/> with a fault plan.</summary>
    public sealed class Handle(MemoryPageMedium medium, string unit) : IPageMedium
    {
        private int _writeCountdown = -1;
        private Fault _writeFault;
        private int _syncCountdown = -1;
        private Fault _syncFault;
        private int _opCountdown = -1;
        private Fault _opFault;
        private bool _dead;
        private bool _refuseAll;

        /// <summary>The run unit's name in the logs.</summary>
        public string Unit => unit;

        /// <summary>Every operation this handle performed, in order: <c>W&lt;page&gt;</c> for a write, <c>S</c> for a sync.</summary>
        public List<string> Operations { get; } = [];

        /// <summary>Fire <paramref name="fault"/> at this handle's <paramref name="nth"/> operation — write OR sync — from
        /// now (1 = the next): the loop that kills or refuses a path at every point of it.</summary>
        public void FaultAtOperation(int nth, Fault fault) => (_opCountdown, _opFault) = (nth, fault);

        /// <summary>The writes this handle performed.</summary>
        public int WritesPerformed { get; private set; }

        /// <summary>The syncs this handle performed.</summary>
        public int SyncsPerformed { get; private set; }

        /// <summary>Fire <paramref name="fault"/> at this handle's <paramref name="nth"/> write from now (1 = the next).</summary>
        public void FaultAtWrite(int nth, Fault fault) => (_writeCountdown, _writeFault) = (nth, fault);

        /// <summary>Fire <paramref name="fault"/> at this handle's <paramref name="nth"/> sync from now (1 = the next).</summary>
        public void FaultAtSync(int nth, Fault fault) => (_syncCountdown, _syncFault) = (nth, fault);

        /// <summary>Refuse every write and sync from now until <see cref="Heal"/>: a full medium.</summary>
        public void RefuseEverything() => _refuseAll = true;

        /// <summary>Clear the fault plan.</summary>
        public void Heal() => (_writeCountdown, _syncCountdown, _opCountdown, _refuseAll) = (-1, -1, -1, false);

        /// <inheritdoc/>
        public long PageCount => medium._length;

        /// <inheritdoc/>
        public void Read(uint page, Span<byte> destination)
        {
            ThrowIfDead();
            medium.Snapshot(page).CopyTo(destination);
        }

        /// <inheritdoc/>
        public void Write(uint page, ReadOnlySpan<byte> source)
        {
            ThrowIfDead();
            if (_refuseAll) throw new IOException("simulated full medium");
            bool dieAfter = Fire(ref _writeCountdown, _writeFault) | Fire(ref _opCountdown, _opFault);
            medium.Apply(unit, page, source);
            WritesPerformed++;
            Operations.Add($"W{page}");
            if (dieAfter) Die();
        }

        /// <inheritdoc/>
        public void Sync()
        {
            ThrowIfDead();
            if (_refuseAll) throw new IOException("simulated failed sync");
            bool dieAfter = Fire(ref _syncCountdown, _syncFault) | Fire(ref _opCountdown, _opFault);
            medium.SyncAll(unit);
            SyncsPerformed++;
            Operations.Add("S");
            if (dieAfter) Die();
        }

        /// <inheritdoc/>
        public void SetPageCount(long pages)
        {
            ThrowIfDead();
            medium._length = pages;
            foreach (var p in medium._pages.Keys.Where(p => p >= pages).ToList()) medium._pages.Remove(p);
        }

        /// <summary>Count down a fault; throw for a death or a refusal BEFORE the operation, answer true for a death
        /// AFTER it. Each planned fault fires once.</summary>
        private bool Fire(ref int countdown, Fault fault)
        {
            if (countdown < 0) return false;
            if (--countdown > 0) return false;
            switch (fault)
            {
                case Fault.Death:
                    countdown = -1;
                    Die();
                    return false;
                case Fault.Refuse:
                    countdown = -1;
                    throw new IOException("simulated medium refusal");
                case Fault.DeathAfter:
                    countdown = -1;
                    return true;
                default:
                    throw new InvalidOperationException(fault.ToString());
            }
        }

        private void Die()
        {
            _dead = true;
            throw new SimulatedDeath();
        }

        private void ThrowIfDead()
        {
            if (_dead) throw new SimulatedDeath();
        }
    }
}
