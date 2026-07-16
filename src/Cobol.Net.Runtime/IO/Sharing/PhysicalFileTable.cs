// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The physical-file sharing/record-lock registry (ISO §9.1.15 sharing / §9.1.16 record locking; design D1 —
/// the sharing/locking statuses 51/52/61 are defined over <em>"another file connector"</em> (§9.1.13.9), and two
/// SELECTs bound to one resolved host path are two connectors over one physical file within one run unit). One
/// instance owned by the <see cref="FileRegistry"/>: the per-host open-connector table (sharing mode + open mode
/// — the Table-19 conflict inputs) and the record locks (owner connector per record id).
/// </summary>
internal sealed class PhysicalFileTable
{
    /// <summary>The live state of one physical file (keyed by resolved host path).</summary>
    internal sealed class State
    {
        public readonly Dictionary<string, (FileSharing Sharing, FileOpenMode Mode)> Open =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>record-id → the connector name that holds its lock.</summary>
        public readonly Dictionary<string, string> RecordLocks = new(StringComparer.Ordinal);
    }

    /// <summary>The per-file-connector record-lock ceiling (§12.4.5.9 GR7 — implementor max, ≥15) → status 54.</summary>
    private const int ConnectorLockMax = 15;
    /// <summary>The per-run-unit record-lock ceiling (§12.4.5.9 GR7 — implementor max, ≥255) → status 53.</summary>
    private const int RunUnitLockMax = 255;

    private readonly Dictionary<string, State> _byHost = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The state for <paramref name="host"/>, created on first reference.</summary>
    public State For(string host)
    {
        if (!_byHost.TryGetValue(host, out var st)) _byHost[host] = st = new State();
        return st;
    }

    /// <summary>The state for <paramref name="host"/> if one exists (no creation).</summary>
    public bool TryGet(string host, out State st) => _byHost.TryGetValue(host, out st!);

    /// <summary>Drop every physical-file state (run-unit start hygiene).</summary>
    public void Clear() => _byHost.Clear();

    /// <summary>Acquire a lock on <paramref name="recId"/> for connector <paramref name="name"/>; re-locking a
    /// record the connector already holds is idempotent (GR8 self-access). Enforces the connector ceiling (54)
    /// and the run-unit ceiling (53). Returns 00 on grant.</summary>
    public string LockRecord(State st, string name, string recId)
    {
        if (st.RecordLocks.TryGetValue(recId, out var owner))
            return string.Equals(owner, name, StringComparison.OrdinalIgnoreCase)
                ? FileStatusCode.Success                     // self re-lock (GR8) — idempotent
                : FileStatusCode.RecordLocked;               // 51 — another connector holds it
        int mine = 0;
        foreach (var o in st.RecordLocks.Values)
            if (string.Equals(o, name, StringComparison.OrdinalIgnoreCase)) mine++;
        if (mine >= ConnectorLockMax) return FileStatusCode.ConnectorLockLimit;   // 54 (GR7)
        if (TotalRunUnitLocks() >= RunUnitLockMax) return FileStatusCode.RunUnitLockLimit;   // 53 (GR7)
        st.RecordLocks[recId] = name;
        return FileStatusCode.Success;
    }

    /// <summary>True when <paramref name="recId"/> is locked by a connector OTHER than <paramref name="name"/>.</summary>
    public static bool IsLockedByOther(State st, string name, string recId) =>
        st.RecordLocks.TryGetValue(recId, out var owner)
        && !string.Equals(owner, name, StringComparison.OrdinalIgnoreCase);

    /// <summary>Release every record lock held by <paramref name="name"/> on <paramref name="st"/> (UNLOCK, CLOSE).</summary>
    public static void ReleaseAllForConnector(State st, string name)
    {
        var mine = st.RecordLocks.Where(kv => string.Equals(kv.Value, name, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key).ToList();
        foreach (var k in mine) st.RecordLocks.Remove(k);
    }

    /// <summary>Release a single record lock a connector holds (the LOCK MODE single-lock discipline, GR6).</summary>
    public static void ReleaseSingle(State st, string name, string recId)
    {
        if (st.RecordLocks.TryGetValue(recId, out var owner)
            && string.Equals(owner, name, StringComparison.OrdinalIgnoreCase))
            st.RecordLocks.Remove(recId);
    }

    private int TotalRunUnitLocks()
    {
        int n = 0;
        foreach (var st in _byHost.Values) n += st.RecordLocks.Count;
        return n;
    }
}
