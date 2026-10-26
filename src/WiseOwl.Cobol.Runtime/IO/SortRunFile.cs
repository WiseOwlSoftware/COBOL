// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>The record-type layouts one SORT/MERGE statement's run files refer to, interned by identity (kb/Work
/// PB3339). <see cref="RecordExtents.Layout"/> is an object — the fixed form of a GIVING file of fixed-length records
/// is asked of it (<see cref="CobolSort.FillTo"/>) — so a run file records the layout's index here and the record
/// comes back with the very object it was released with. A statement has as many layouts as its SD has record
/// types, so the table stays small.</summary>
internal sealed class SortLayouts
{
    private readonly List<CobolContiguousLayout> _layouts = [];

    /// <summary>The index of <paramref name="layout"/>, interned on first sight; -1 for none.</summary>
    public int IndexOf(CobolContiguousLayout? layout)
    {
        if (layout is null) return -1;
        for (int i = 0; i < _layouts.Count; i++)
            if (ReferenceEquals(_layouts[i], layout)) return i;
        _layouts.Add(layout);
        return _layouts.Count - 1;
    }

    /// <summary>The layout at <paramref name="index"/>; null for -1.</summary>
    public CobolContiguousLayout? At(int index) => index < 0 ? null : _layouts[index];
}

/// <summary>
/// ⛔ ONE TEMPORARY RUN of a SORT/MERGE statement (kb/Work PB3339; docs/CONFORMANCE.md §3 D-SWK): records appended in
/// run order, then read back in that order, through ONE handle (<see cref="HostFile.CreateSortWork"/>), which deletes
/// the file when it closes. The file is private to this process and this statement, so its record format is this
/// class's alone and no file coded character set applies:
/// <code>
///   int32   character count n
///   int32   extent component count k, or -1 when the record carries no extent table
///   int32   layout index (<see cref="SortLayouts"/>), -1 for none          — only when k ≥ 0
///   k × 8   per component: fixed-run offset(4), length in characters(4)    — only when k ≥ 0
///   2n      the image's UTF-16 code units, unconverted
/// </code>
/// The code units are copied as they are, so an image round-trips exactly — HIGH-VALUE U+FFFF, a national item's
/// byte pairs and a lone surrogate included, which no <see cref="System.Text.Encoding"/> would keep.
/// <para>An I-O failure (the work directory missing or not writable, the volume full, a short read) is an
/// implementor-defined fatal stop, <see cref="CobolImplementorFatalException"/>, naming the directory and the host's
/// reason: the run unit ends rather than lose a record silently (D-SWK (d)).</para>
/// </summary>
internal sealed class SortRunFile : IDisposable
{
    /// <summary>The handle's buffer: what writing a run, and the return phase per merged run, spend.</summary>
    public const int BufferBytes = 64 * 1024;

    private readonly FileStream _fs;
    private readonly string _directory;
    private readonly SortLayouts _layouts;
    private byte[] _head = new byte[64];
    private long _written;
    private long _read;
    private bool _reading;

    private SortRunFile(FileStream fs, string directory, SortLayouts layouts)
    {
        _fs = fs;
        _directory = directory;
        _layouts = layouts;
    }

    /// <summary>A new, empty run in <paramref name="directory"/>.</summary>
    public static SortRunFile Create(string directory, SortLayouts layouts)
    {
        try { return new SortRunFile(HostFile.CreateSortWork(directory, BufferBytes), directory, layouts); }
        catch (Exception e) when (IsHostFailure(e)) { throw Failure(directory, "created", e); }
    }

    /// <summary>How many records the run holds.</summary>
    public long Count => _written;

    /// <summary>Append <paramref name="frame"/> after the run's last record.</summary>
    public void Append(in StoredFrame frame)
    {
        try
        {
            if (_reading)
            {
                _fs.Seek(0, SeekOrigin.End);
                _reading = false;
            }
            string image = frame.Image ?? "";
            RecordExtents? extents = frame.Extents;
            int headBytes = 8 + (extents is null ? 0 : 4 + (8 * extents.Count));
            if (headBytes > _head.Length) _head = new byte[headBytes];
            Span<byte> head = _head.AsSpan(0, headBytes);
            BinaryPrimitives.WriteInt32LittleEndian(head, image.Length);
            BinaryPrimitives.WriteInt32LittleEndian(head[4..], extents?.Count ?? -1);
            if (extents is not null)
            {
                BinaryPrimitives.WriteInt32LittleEndian(head[8..], _layouts.IndexOf(extents.Layout));
                for (int k = 0; k < extents.Count; k++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(head[(12 + (8 * k))..], extents.FixedAt[k]);
                    BinaryPrimitives.WriteInt32LittleEndian(head[(16 + (8 * k))..], extents.Lengths[k]);
                }
            }
            _fs.Write(head);
            _fs.Write(MemoryMarshal.AsBytes(image.AsSpan()));
            _written++;
        }
        catch (Exception e) when (IsHostFailure(e)) { throw Failure(_directory, "written", e); }
    }

    /// <summary>Position the run at its first record for reading (again).</summary>
    public void Restart()
    {
        try
        {
            _fs.Flush();
            _fs.Seek(0, SeekOrigin.Begin);
        }
        catch (Exception e) when (IsHostFailure(e)) { throw Failure(_directory, "read", e); }
        _read = 0;
        _reading = true;
    }

    /// <summary>The run's next record; false after its last.</summary>
    public bool Next(out StoredFrame frame)
    {
        frame = default;
        if (!_reading || _read >= _written) return false;
        try
        {
            Span<byte> head = _head.AsSpan(0, 8);
            _fs.ReadExactly(head);
            int chars = BinaryPrimitives.ReadInt32LittleEndian(head);
            int components = BinaryPrimitives.ReadInt32LittleEndian(head[4..]);
            RecordExtents? extents = null;
            if (components >= 0)
            {
                int tableBytes = 4 + (8 * components);
                if (tableBytes > _head.Length) _head = new byte[tableBytes];
                Span<byte> table = _head.AsSpan(0, tableBytes);
                _fs.ReadExactly(table);
                var fixedAt = new int[components];
                var lengths = new int[components];
                for (int k = 0; k < components; k++)
                {
                    fixedAt[k] = BinaryPrimitives.ReadInt32LittleEndian(table[(4 + (8 * k))..]);
                    lengths[k] = BinaryPrimitives.ReadInt32LittleEndian(table[(8 + (8 * k))..]);
                }
                extents = new RecordExtents(fixedAt, lengths, _layouts.At(BinaryPrimitives.ReadInt32LittleEndian(table)));
            }
            string image = string.Create(chars, _fs, static (span, fs) => fs.ReadExactly(MemoryMarshal.AsBytes(span)));
            frame = new StoredFrame(image, extents);
            _read++;
            return true;
        }
        catch (Exception e) when (IsHostFailure(e)) { throw Failure(_directory, "read", e); }
    }

    /// <summary>Close the handle, which deletes the file (<see cref="HostFile.CreateSortWork"/>). A run being written
    /// when its statement ended still holds unflushed bytes; the close discards the run, so a failure to flush them (a
    /// full volume — the very failure that may have ended the statement) loses nothing and must not replace the
    /// exception the statement is ending with. The handle is released either way.</summary>
    public void Dispose()
    {
        try { _fs.Dispose(); }
        catch (IOException) { }   // the run's bytes are being discarded with the file: nothing is lost
    }

    /// <summary>The host failures a work file can meet — every one becomes <see cref="Failure"/>.</summary>
    private static bool IsHostFailure(Exception e) =>
        e is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException;

    /// <summary>The fatal stop for a work file that could not be <paramref name="what"/> (D-SWK (d)).</summary>
    private static CobolImplementorFatalException Failure(string directory, string what, Exception e) =>
        new($"SORT/MERGE: a temporary sort work file in '{directory}' could not be {what}: {e.Message} "
            + $"(set {SortMemory.DirectoryVariable} to a writable directory with room for the sort; docs/CONFORMANCE.md §3 D-SWK)");
}
