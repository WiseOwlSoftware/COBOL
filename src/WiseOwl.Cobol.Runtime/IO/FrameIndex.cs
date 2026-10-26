// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// ⛔ WHERE A RECORD VARYING FILE'S FRAME N STARTS, IN MEMORY THAT DOES NOT GROW WITH THE FILE (kb/Work PB3350;
/// docs/rearchitecture/DESIGN-runtime-library.md 2.2.3). The sequential connector's backward READ — §14.9.30.4 GR21
/// rule c)'s decreasing arm, and the COBOL-85 <c>OPEN INPUT … REVERSED</c> standing reversal — asks for the START of an
/// ordinal, and the frames of a RECORD VARYING file are not uniformly wide the way a fixed record-sequential file's
/// blocks are, so the start of frame N is found by walking. The index it answers from holds
/// <list type="bullet">
/// <item>a <b>checkpoint</b> list: the start of every <see cref="Stride"/>-th frame — one <see cref="long"/> per
/// <see cref="Stride"/> records (2 KB per million records);</item>
/// <item>a <b>window</b>: the exact starts of the one checkpoint span the connector is reading backward through, at most
/// <see cref="Stride"/> longs, rebuilt by ONE forward walk of at most <see cref="Stride"/> frame heads from the span's
/// checkpoint when the position leaves the span.</item>
/// </list>
/// Memory is O(records / <see cref="Stride"/> + <see cref="Stride"/>); a whole-file backward pass costs one window build per
/// span, O(records) head reads in total — the order of the forward read. The index replaced one offset per record kept
/// for the life of the OPEN.
/// <para><b>One walk, one decoder.</b> Every frame is read by <see cref="RecordFraming.ReadStreamFrame"/>, so the index
/// holds exactly the records a forward READ delivers (kb/Work PB2754). The checkpoints are built FOR FREE by the forward
/// walk the connector already makes (<see cref="Observe"/>), and by one counting walk (<see cref="Extend"/>) for a target
/// beyond the frames walked so far — OPEN REVERSED's first READ targets the last record of a file nothing has read,
/// and the same walk measures the REVERSED count.</para>
/// <para><b>Never stale within an OPEN.</b> The layout cannot change while a connector is open — §14.9.35.4 GR16 makes a
/// record sequential REWRITE size-preserving — and a sibling's appends (ALL OTHER sharing) lie past every walked frame:
/// the walk resumes where it stopped, so a target that reaches them finds them. One index per OPEN; the connector drops
/// it at the next.</para>
/// </summary>
internal sealed class FrameIndex(int stride = FrameIndex.DefaultStride)
{
    /// <summary>The stride of the shipped index: one checkpoint per 4096 frames, a window of at most 4096 starts (32 KB).</summary>
    public const int DefaultStride = 4096;

    /// <summary>Frames between two checkpoints, and the most starts the window holds.</summary>
    public int Stride { get; } = stride > 0 ? stride : throw new ArgumentOutOfRangeException(nameof(stride));

    /// <summary>Checkpoint k is the start of the frame whose zero-based number is k × <see cref="Stride"/>.</summary>
    private readonly List<long> _checkpoints = [];

    /// <summary>The frames walked so far: frame numbers [0, <see cref="Frames"/>) are known to exist.</summary>
    private long _frames;

    /// <summary>The byte offset just past the last walked frame — a frame boundary, where the walk resumes.</summary>
    private long _walkedTo;

    /// <summary>The exact starts of the frames of one checkpoint span (null until a backward read asks).</summary>
    private long[]? _window;

    /// <summary>The checkpoint span the window holds, or −1; and how many of its frames it holds.</summary>
    private long _windowSpan = -1;
    private int _windowCount;

    /// <summary>The frames walked so far.</summary>
    public long Frames => _frames;

    /// <summary>The checkpoints held — ⌈<see cref="Frames"/> / <see cref="Stride"/>⌉.</summary>
    public int Checkpoints => _checkpoints.Count;

    /// <summary>The starts the window holds — never more than <see cref="Stride"/>, whatever the file's size.</summary>
    public int WindowCount => _windowCount;

    /// <summary>The forward walk passes a frame occupying [<paramref name="start"/>, <paramref name="end"/>). Takes it
    /// into the index when it is the next frame beyond those walked, so a file read forward is indexed for nothing; a frame
    /// anywhere else (a re-read after a backward step, a START's repositioned walk) is already known and is ignored.</summary>
    public void Observe(long start, long end)
    {
        if (start == _walkedTo) Record(start, end);
    }

    /// <summary>Walk on from the last walked frame boundary until frame number <paramref name="until"/> − 1 is known or
    /// the framed data ends, and return the frames now known. <see cref="long.MaxValue"/> walks to the end of the data —
    /// the REVERSED count, which is the same walk that builds every checkpoint. The walk moves
    /// <paramref name="fs"/>'s position.</summary>
    public long Extend(Stream fs, long until = long.MaxValue)
    {
        if (_frames >= until) return _frames;
        fs.Seek(_walkedTo, SeekOrigin.Begin);
        var medium = new RecordFraming.StreamMedium(fs);
        while (_frames < until && RecordFraming.ReadStreamFrame(ref medium, out _))
            Record(_walkedTo, fs.Position);
        return _frames;
    }

    /// <summary>The byte offset at which record <paramref name="ordinal"/> (1-based) starts, walking only as far as it
    /// takes; false when the framed data holds no such record — §14.9.30.4 GR21's "no record is found", which the caller
    /// answers as the end of the data. The walk moves <paramref name="fs"/>'s position; the caller repositions it.</summary>
    public bool TryStartOf(Stream fs, long ordinal, out long start)
    {
        start = 0;
        long n = ordinal - 1;
        if (n < 0 || Extend(fs, n + 1) <= n) return false;
        long span = n / Stride;
        if (span != _windowSpan || n - span * Stride >= _windowCount) BuildWindow(fs, span);
        int slot = (int)(n - span * Stride);
        if (slot >= _windowCount) return false;   // the file shrank under the index: the data ends before the checkpoint's span does
        start = _window![slot];
        return true;
    }

    private void Record(long start, long end)
    {
        if (_frames % Stride == 0) _checkpoints.Add(start);
        _frames++;
        _walkedTo = end;
    }

    /// <summary>The window of checkpoint span <paramref name="span"/>: one forward walk of at most
    /// <see cref="Stride"/> frame heads from the span's checkpoint, noting each start.</summary>
    private void BuildWindow(Stream fs, long span)
    {
        _window ??= new long[Stride];
        // Invalidate BEFORE overwriting: a host read that throws mid-walk (an IOException the connector answers as status 30)
        // must leave no window claiming a span whose slots now hold another span's starts.
        _windowSpan = -1;
        _windowCount = 0;
        int want = (int)Math.Min(Stride, _frames - span * Stride);
        fs.Seek(_checkpoints[(int)span], SeekOrigin.Begin);
        var medium = new RecordFraming.StreamMedium(fs);
        int held = 0;
        for (long at = fs.Position; held < want && RecordFraming.ReadStreamFrame(ref medium, out _); at = fs.Position)
            _window[held++] = at;
        _windowSpan = span;
        _windowCount = held;
    }
}
