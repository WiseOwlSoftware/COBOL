// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE RECORDS OF ONE EXECUTING SORT/MERGE STATEMENT, IN BOUNDED MEMORY — an external merge sort (kb/Work PB3339;
/// COBOLNET_DESIGN §8.2 "The sort-merge store is an EXTERNAL MERGE SORT", DESIGN-runtime-library §2.2; docs/CONFORMANCE.md
/// §3 D-SWK). The owner's ruling on the keyed stores (2026-10-17, "Reading the entire file into memory as a design
/// requirement is a fatal flaw") is the rule here too: this class never holds more than <see cref="SortMemory.LimitBytes"/>
/// of records (by <see cref="ResidentBytes"/>' estimate), or one record when a single record is larger.
/// <list type="bullet">
/// <item><b>Release phase.</b> <see cref="Add"/> appends to ONE in-memory chunk. A record that would take the chunk past
/// the limit first SEALS it: a SORT sorts the chunk (<see cref="SortKeyColumns"/> decoded once per record; a tie keeps
/// release order) and writes it as a RUN; a MERGE appends each USING stream's part of the chunk to that stream's run —
/// a USING file is already ordered (§14.9.24.4 GR6). F runs of one level (<see cref="SortMemory.FanIn"/>) merge into
/// one run of the next level, always the LAST F, so only consecutive runs are ever merged and the run list stays in
/// release order.</item>
/// <item><b>Sequence phase</b> (<see cref="Sequence"/>). When nothing was written the chunk is sorted (SORT) or its
/// streams merged (MERGE) IN MEMORY, so a file that fits pays no disk cost. Otherwise the last chunk is written too and
/// runs are merged until at most F remain.</item>
/// <item><b>Return phase.</b> <see cref="Next"/> reads a <see cref="SortMergeCursor"/> over the runs; <see cref="Rewind"/>
/// restarts it, so each GIVING file receives the whole result (§14.9.40.4 GR15, §14.9.24.4 GR12).</item>
/// </list>
/// ⛔ ONE ORDERING RULE for every merge, in memory or on disk: the lowest key, and on equal keys the EARLIEST run. The
/// runs are in release order (SORT) or input-file order (MERGE) and each is stable, so equal keys come back in the
/// order §14.9.40.4 GR3 a)/b) (DUPLICATES IN ORDER) and §14.9.24.4 GR4 a)/b) require — whatever the limit.
/// </summary>
internal sealed class SortWork : IDisposable
{
    /// <summary>The bytes a record costs in memory beside its characters, extent table and keys: its
    /// <see cref="StoredFrame"/> slot in the chunk (counted twice, for a list's growth), its string object, and the
    /// sort's index entry.</summary>
    private const long FrameOverheadBytes = (2 * 24) + SortKeyColumns.StringObjectBytes + sizeof(int);

    private readonly SortMemory _memory;
    private readonly CobolSort.Key[] _keys;
    private readonly CobolCollation? _collation;
    private readonly CobolCollation? _national;
    /// <summary>What one record's decoded keys cost (<see cref="SortKeyColumns.ResidentBytesPerRecord"/>).</summary>
    private readonly long _keyBytes;
    private readonly SortLayouts _layouts = new();

    /// <summary>⛔ THE ONE IN-MEMORY RECORD LIST — bounded by <see cref="Add"/>'s seal (<c>SortWorkBoundDriftTests</c>).</summary>
    private readonly List<StoredFrame> _chunk = [];
    private long _chunkBytes;

    /// <summary>SORT: the sealed runs on disk, in release order, each with its merge level.</summary>
    private readonly List<(SortRunFile Run, int Level)> _runs = [];

    /// <summary>MERGE: one entry per USING stream (file-name-2, -3, … in statement order) — empty for a SORT.</summary>
    private readonly List<MergeStream> _streams = [];

    /// <summary>MERGE: the §14.9.24.4 GR6 order check over the records as they arrive — two slots, the previous
    /// record's and this one's, alternating.</summary>
    private SortKeyColumns? _orderCheck;
    private int _orderPrev = -1;

    private SortMergeCursor? _output;
    private bool _sequenced;

    /// <summary>A new, empty store for a statement with these keys and GR5 sequences (the §14.6.6 r5 snapshots).</summary>
    public SortWork(SortMemory memory, CobolSort.Key[] keys, CobolCollation? collation, CobolCollation? national)
    {
        _memory = memory;
        _keys = keys;
        _collation = collation;
        _national = national;
        _keyBytes = SortKeyColumns.ResidentBytesPerRecord(keys, collation, national);
    }

    /// <summary>The most bytes of records the chunk held at once — the measured bound (<c>SortWorkBoundTests</c>).</summary>
    public long PeakResidentBytes { get; private set; }

    /// <summary>How many runs the statement has written to temporary files, the merges' outputs included.</summary>
    public int RunsWritten { get; private set; }

    /// <summary>How many temporary files the statement holds open now.</summary>
    public int OpenRunFiles => _runs.Count + _streams.Count(s => s.Run is not null) + (_output?.OpenRunFiles ?? 0);

    /// <summary>The resident estimate of <paramref name="frame"/>: its characters (two bytes each), its frame and extent
    /// table, and its decoded keys.</summary>
    private long ResidentBytes(in StoredFrame frame) =>
        FrameOverheadBytes + (2L * (frame.Image?.Length ?? 0)) + _keyBytes
        + (frame.Extents is { } e ? 64 + (8L * e.Count) : 0);

    /// <summary>MERGE: the records released from here on belong to the next USING stream (§14.9.24.4 GR4 b)).</summary>
    public void NextStream()
    {
        _streams.Add(new MergeStream(_chunk.Count));
        _orderPrev = -1;
    }

    /// <summary>Release one record. A release after the sequence phase — possible only with EC-FLOW-RELEASE unchecked —
    /// releases to no phase and is not returned (D-SWK (e)).</summary>
    public void Add(in StoredFrame frame)
    {
        if (_sequenced) return;
        long bytes = ResidentBytes(frame);
        if (_chunk.Count > 0 && _chunkBytes + bytes > _memory.LimitBytes) Seal();
        if (_streams.Count > 0) CheckOrder(frame);
        _chunk.Add(frame);
        _chunkBytes += bytes;
        if (_chunkBytes > PeakResidentBytes) PeakResidentBytes = _chunkBytes;
    }

    /// <summary>The sequence phase (§14.9.40.4 GR9 b) for a SORT, the merge operation of §14.9.24.4 for a MERGE): from
    /// here on <see cref="Next"/> returns the records in key order.</summary>
    public void Sequence()
    {
        if (_sequenced) return;
        _sequenced = true;
        if (_streams.Count == 0) SequenceSort();
        else SequenceMerge();
    }

    /// <summary>The first record of each MERGE stream that is out of KEY order, as (stream ordinal, record number in the
    /// stream), both 1-based — the §14.9.24.4 GR6 findings <see cref="CobolSort.Merge"/> raises when checking is
    /// enabled.</summary>
    public IEnumerable<(int Stream, long Record)> OrderViolations()
    {
        for (int s = 0; s < _streams.Count; s++)
            if (_streams[s].FirstOutOfOrder is { } r) yield return (s + 1, r);
    }

    /// <summary>The next record in key order; false at end — and before the sequence phase, which only an unchecked
    /// EC-FLOW-RETURN reaches (D-SWK (e)).</summary>
    public bool Next(out StoredFrame frame)
    {
        if (_output is null)
        {
            frame = default;
            return false;
        }
        return _output.Next(out frame);
    }

    /// <summary>Restart the return phase at the first record (each GIVING file receives the whole result).</summary>
    public void Rewind() => _output?.Restart();

    /// <summary>Close every temporary file (each deletes itself) and drop the records.</summary>
    public void Dispose()
    {
        _output?.Dispose();
        _output = null;
        foreach (var (run, _) in _runs) run.Dispose();
        _runs.Clear();
        foreach (var stream in _streams) stream.Run?.Dispose();
        _streams.Clear();
        _chunk.Clear();
        _chunkBytes = 0;
    }

    // ── The release phase ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Seal the chunk: write it out and start an empty one.</summary>
    private void Seal()
    {
        if (_streams.Count == 0) WriteSortedChunk();
        else WriteStreamChunks();
        _chunk.Clear();
        _chunkBytes = 0;
    }

    /// <summary>SORT: the chunk, stably sorted, as a new level-0 run — then the cascade.</summary>
    private void WriteSortedChunk()
    {
        if (_chunk.Count == 0) return;
        int[] order = SortedOrder();
        var run = NewRun();
        foreach (int i in order) run.Append(_chunk[i]);
        _runs.Add((run, 0));
        Cascade();
    }

    /// <summary>The chunk's records in key order: an index sort over keys decoded once per record, a tie broken by
    /// release order — the stable sort §14.9.40.4 GR3 needs (and GR4's undefined order safely refined).</summary>
    private int[] SortedOrder()
    {
        int n = _chunk.Count;
        var order = new int[n];
        for (int i = 0; i < n; i++) order[i] = i;
        if (n < 2) return order;
        var columns = new SortKeyColumns(_keys, _collation, _national, n);
        for (int i = 0; i < n; i++) columns.Set(i, _chunk[i]);
        Array.Sort(order, (x, y) =>
        {
            int c = columns.Compare(x, y);
            return c != 0 ? c : x - y;   // tie → release order: the stable sort
        });
        return order;
    }

    /// <summary>F runs of the lowest level merge into one of the next level, for as long as the last F share a level.
    /// Levels never increase along the list, so the last F are consecutive and release order is kept.</summary>
    private void Cascade()
    {
        int f = _memory.FanIn;
        while (_runs.Count >= f)
        {
            int level = _runs[^1].Level;
            for (int i = _runs.Count - f; i < _runs.Count; i++)
                if (_runs[i].Level != level) return;
            MergeLastRuns(f, level + 1);
        }
    }

    /// <summary>Merge the last <paramref name="count"/> runs into one run of <paramref name="level"/>, in their place.</summary>
    private void MergeLastRuns(int count, int level)
    {
        int first = _runs.Count - count;
        var inputs = new SortRunFile[count];
        for (int i = 0; i < count; i++) inputs[i] = _runs[first + i].Run;
        var merged = MergeInto(inputs);
        _runs.RemoveRange(first, count);
        _runs.Add((merged, level));
    }

    /// <summary>The k-way merge of <paramref name="inputs"/> (in run order) written as one new run; the inputs are closed.</summary>
    private SortRunFile MergeInto(SortRunFile[] inputs)
    {
        var output = NewRun();
        using (var cursor = new SortMergeCursor([.. inputs.Select(SortRun.OnDisk)], _keys, _collation, _national))
            while (cursor.Next(out StoredFrame frame)) output.Append(frame);
        return output;
    }

    /// <summary>MERGE: each stream's part of the chunk, appended to that stream's run in arrival order.</summary>
    private void WriteStreamChunks()
    {
        for (int s = 0; s < _streams.Count; s++)
        {
            var stream = _streams[s];
            int start = Math.Max(stream.ChunkStart, 0);
            int end = s + 1 < _streams.Count ? Math.Max(_streams[s + 1].ChunkStart, 0) : _chunk.Count;
            if (end > start)
            {
                stream.Run ??= NewRun();
                for (int i = start; i < end; i++) stream.Run.Append(_chunk[i]);
            }
            stream.ChunkStart = s + 1 == _streams.Count ? 0 : -1;   // only the last stream receives more records
        }
    }

    /// <summary>MERGE, §14.9.24.4 GR6: is the arriving record out of KEY order against the stream's previous record?
    /// The first such record of each stream is remembered and raised by <see cref="CobolSort.Merge"/> when checking is
    /// enabled; the scan runs only while it is, as the merge's own check did.</summary>
    private void CheckOrder(in StoredFrame frame)
    {
        var stream = _streams[^1];
        stream.Count++;
        if (!ExceptionState.SortMergeSequenceChecking || stream.FirstOutOfOrder is not null) return;
        _orderCheck ??= new SortKeyColumns(_keys, _collation, _national, 2);
        int slot = _orderPrev == 0 ? 1 : 0;
        _orderCheck.Set(slot, frame);
        if (_orderPrev >= 0 && _orderCheck.Compare(_orderPrev, slot) > 0) stream.FirstOutOfOrder = stream.Count;
        _orderPrev = slot;
    }

    // ── The sequence phase ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>SORT: in memory when nothing was written; else the last chunk as a run and the runs merged down to F.</summary>
    private void SequenceSort()
    {
        if (_runs.Count == 0)
        {
            _output = new SortMergeCursor([SortRun.InMemory(_chunk, SortedOrder(), 0, _chunk.Count)], _keys, _collation, _national);
            return;
        }
        Seal();
        while (_runs.Count > _memory.FanIn) MergeLastRuns(_memory.FanIn, _runs[^1].Level + 1);
        var runs = _runs.Select(r => SortRun.OnDisk(r.Run)).ToArray();
        _runs.Clear();   // the cursor owns them now
        _output = new SortMergeCursor(runs, _keys, _collation, _national);
    }

    /// <summary>MERGE: the streams merged from memory when nothing was written; else every stream's run on disk, merged
    /// down to F consecutive streams.</summary>
    private void SequenceMerge()
    {
        if (_streams.All(s => s.Run is null))
        {
            var slices = new SortRun[_streams.Count];
            for (int s = 0; s < _streams.Count; s++)
                slices[s] = SortRun.InMemory(_chunk, null, _streams[s].ChunkStart,
                    s + 1 < _streams.Count ? _streams[s + 1].ChunkStart : _chunk.Count);
            _output = new SortMergeCursor(slices, _keys, _collation, _national);
            return;
        }
        Seal();
        var runs = new List<SortRunFile>();
        foreach (var stream in _streams)
            if (stream.Run is { } run) runs.Add(run);
        _streams.Clear();   // the runs move to the merge
        // More streams than one merge reads: merge the last F, consecutive, into one — in-order streams merge exactly;
        // an out-of-order one (GR6 unchecked) merges within "undefined".
        while (runs.Count > _memory.FanIn)
        {
            int first = runs.Count - _memory.FanIn;
            var merged = MergeInto([.. runs.GetRange(first, _memory.FanIn)]);
            runs.RemoveRange(first, _memory.FanIn);
            runs.Add(merged);
        }
        _output = new SortMergeCursor([.. runs.Select(SortRun.OnDisk)], _keys, _collation, _national);
    }

    private SortRunFile NewRun()
    {
        RunsWritten++;
        return SortRunFile.Create(_memory.Directory, _layouts);
    }

    /// <summary>One MERGE USING stream: where its records begin in the current chunk (-1 once they have all left it),
    /// its run on disk once any were written, its record count and its first out-of-order record (GR6).</summary>
    private sealed class MergeStream(int chunkStart)
    {
        public int ChunkStart = chunkStart;
        public SortRunFile? Run;
        public long Count;
        public long? FirstOutOfOrder;
    }
}

/// <summary>One run a <see cref="SortMergeCursor"/> reads: a slice of the in-memory chunk (in an index order, or in
/// place), or a run file (kb/Work PB3339).</summary>
internal sealed class SortRun : IDisposable
{
    private readonly List<StoredFrame>? _records;
    private readonly int[]? _order;
    private readonly int _start;
    private readonly int _end;
    private int _at;
    private readonly SortRunFile? _file;

    private SortRun(List<StoredFrame>? records, int[]? order, int start, int end, SortRunFile? file)
    {
        _records = records;
        _order = order;
        _start = start;
        _end = end;
        _file = file;
        Restart();
    }

    /// <summary>Records <paramref name="start"/> to <paramref name="end"/> of <paramref name="records"/>, through
    /// <paramref name="order"/> when it is given (a sorted chunk's index order spans the whole chunk).</summary>
    public static SortRun InMemory(List<StoredFrame> records, int[]? order, int start, int end) =>
        new(records, order, start, end, null);

    /// <summary>A run file — owned, and closed, by the run.</summary>
    public static SortRun OnDisk(SortRunFile file) => new(null, null, 0, 0, file);

    /// <summary>Is this run a temporary file?</summary>
    public bool OnDiskFile => _file is not null;

    public void Restart()
    {
        _at = _start;
        _file?.Restart();
    }

    public bool Next(out StoredFrame frame)
    {
        if (_file is not null) return _file.Next(out frame);
        if (_at >= _end)
        {
            frame = default;
            return false;
        }
        int i = _at++;
        frame = _records![_order is null ? i : _order[i]];
        return true;
    }

    public void Dispose() => _file?.Dispose();
}

/// <summary>⛔ THE ONE MERGE of a SORT/MERGE statement's runs (kb/Work PB3339) — the cascade, the consolidation and the
/// return phase all read through it. Each run's head record holds a slot of one <see cref="SortKeyColumns"/>, decoded
/// once; <see cref="Next"/> takes the lowest head, and a STRICT less-than keeps an equal head from displacing an
/// earlier run's (§14.9.24.4 GR4 a), and §14.9.40.4 GR3 for runs in release order). One run is read straight through,
/// with no key decoded at all.</summary>
internal sealed class SortMergeCursor : IDisposable
{
    private readonly SortRun[] _runs;
    private readonly SortKeyColumns? _heads;
    private readonly StoredFrame[] _head;
    private readonly bool[] _live;

    public SortMergeCursor(SortRun[] runs, CobolSort.Key[] keys, CobolCollation? collation, CobolCollation? national)
    {
        _runs = runs;
        _head = new StoredFrame[runs.Length];
        _live = new bool[runs.Length];
        if (runs.Length > 1) _heads = new SortKeyColumns(keys, collation, national, runs.Length);
        Prime();
    }

    /// <summary>How many temporary files the cursor's runs hold open.</summary>
    public int OpenRunFiles => _runs.Count(r => r.OnDiskFile);

    /// <summary>Start again at every run's first record.</summary>
    public void Restart()
    {
        foreach (var run in _runs) run.Restart();
        Prime();
    }

    private void Prime()
    {
        if (_heads is null) return;
        for (int s = 0; s < _runs.Length; s++) Advance(s);
    }

    private void Advance(int s)
    {
        _live[s] = _runs[s].Next(out _head[s]);
        if (_live[s]) _heads!.Set(s, _head[s]);
    }

    public bool Next(out StoredFrame frame)
    {
        if (_heads is null)
        {
            if (_runs.Length == 1) return _runs[0].Next(out frame);
            frame = default;
            return false;
        }
        int best = -1;
        for (int s = 0; s < _runs.Length; s++)
            if (_live[s] && (best < 0 || _heads.Compare(s, best) < 0))   // STRICT: an equal head never displaces an earlier run's
                best = s;
        if (best < 0)
        {
            frame = default;
            return false;
        }
        frame = _head[best];
        Advance(best);
        return true;
    }

    public void Dispose()
    {
        foreach (var run in _runs) run.Dispose();
    }
}
