// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// The ONE on-disk record framing shared by every organization (DESIGN-runtime-library §2.2): each framed record
/// is a 4-byte little-endian length prefix followed by that many Latin-1 payload bytes; an EMPTY slot (relative
/// gap) is the tag 0xFFFFFFFF with no payload. Explicit length framing carries occupancy soundly — the legacy's
/// fixed-format all-0x00/0xFF gap HEURISTIC could vanish a legitimate all-zero binary record — and the physical
/// format is implementor-defined by the spec, so only self-consistency matters: producer and consumer of a
/// corpus chain both run on this framing.
/// <para>⛔ THE LICENCE IS §9.1.7.2, NOT §9.1.13.6, and the correction matters (kb/Work PB292). This comment
/// cited §9.1.13.6's "externally-defined boundaries" until 2026-09-05 — a real sentence about a DIFFERENT
/// question: it is the I-O status 34 rule for writing past a physical FILE's boundary, not a statement about
/// record framing (feedback_a_real_clause_can_answer_a_different_question). The clause that actually grants
/// this is §9.1.7.2: "In record sequential files the length of each record is determined by any information the
/// implementor may add to the record on the physical storage medium (such as record length headers)" — this
/// prefix IS that header. §12.4.5.11.4 GR5 makes the same grant for the variable-length case where no RECORD
/// DELIMITER clause governs, which for COBOL.NET is EVERY case (the clause is declined whole — COBOLNET1778,
/// docs/CONFORMANCE.md §2 row 26), and the determination is filed as Annex A.1 item 151 in
/// docs/CONFORMANCE.md §7. §12.4.5.11.4 GR1 is what this framing must not violate — "Any method used shall not
/// be reflected in the record area or the record size used within the function, method, or program" — so the
/// prefix is stripped before the record area is filled and never enters a record size a program can read.</para>
/// Two access shapes over the SAME byte layout:
/// <list type="bullet">
/// <item><b>Store-level</b> (<see cref="WriteStore"/>/<see cref="ReadStore"/>) — the whole-file rewrite/load the
/// keyed connectors use (relative slots + the indexed persist order — IndexedConnector.PersistOrder).</item>
/// <item><b>Stream-level</b> (<see cref="WritePrefix"/>/<see cref="PrefixLength"/>) — the incremental
/// prefix-per-record shape the sequential connector streams through its Latin-1 reader/writer (chars 0–255 map
/// 1:1 to bytes under Latin-1, so the char-shaped prefix is byte-identical to the store-level one).</item>
/// </list>
/// </summary>
internal static class RecordFraming
{
    /// <summary>The empty-slot (gap) tag — a length prefix of 0xFFFFFFFF with no payload.</summary>
    private const uint GapTag = 0xFFFFFFFF;

    // ── Store-level (byte) shape — the keyed connectors' whole-store persist/load ───────────────────────────

    /// <summary>Write the whole store: one frame per ordinal position; null = an empty (gap) slot.</summary>
    public static void WriteStore(string path, IReadOnlyList<string?> frames)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        Span<byte> len = stackalloc byte[4];
        foreach (string? frame in frames)
        {
            if (frame is null)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(len, GapTag);
                fs.Write(len);
                continue;
            }
            byte[] payload = Encoding.Latin1.GetBytes(frame);
            BinaryPrimitives.WriteUInt32LittleEndian(len, (uint)payload.Length);
            fs.Write(len);
            fs.Write(payload, 0, payload.Length);
        }
    }

    /// <summary>Read the whole store back: one entry per frame, null for a gap. A torn tail ends the store.</summary>
    public static List<string?> ReadStore(string path)
    {
        var frames = new List<string?>();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var len = new byte[4];
        while (FillExactly(fs, len, 4))
        {
            uint n = BinaryPrimitives.ReadUInt32LittleEndian(len);
            if (n == GapTag) { frames.Add(null); continue; }
            var payload = new byte[n];
            if (!FillExactly(fs, payload, (int)n)) break;
            frames.Add(Encoding.Latin1.GetString(payload));
        }
        return frames;
    }

    /// <summary>The byte offset of every frame in a framed physical file, in order (index = ordinal − 1).
    /// This is the positioning index a §14.9.30.4 GR21 BACKWARD sequential READ needs on a RECORD VARYING file,
    /// whose frames are not uniformly wide the way a fixed record-sequential file's blocks are. Only the length
    /// prefixes are read — each payload is SEEKED over, never materialized — so the index costs one pass and no
    /// record storage. A torn tail ends the store, the same rule <see cref="ReadStore"/> applies.</summary>
    public static List<long> FrameStarts(string path)
    {
        var starts = new List<long>();
        if (!File.Exists(path)) return starts;
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var len = new byte[4];
        while (true)
        {
            long at = fs.Position;
            if (!FillExactly(fs, len, 4)) break;
            uint n = BinaryPrimitives.ReadUInt32LittleEndian(len);
            if (n != GapTag)
            {
                if (fs.Position + n > fs.Length) break;   // a torn tail ends the store
                fs.Seek(n, SeekOrigin.Current);
            }
            starts.Add(at);
        }
        return starts;
    }

    private static bool FillExactly(Stream s, byte[] buf, int count)
    {
        int got = 0;
        while (got < count)
        {
            int n = s.Read(buf, got, count - got);
            if (n == 0) return false;
            got += n;
        }
        return true;
    }

    // ── Stream-level (char) shape — the sequential connector's varying-record framing ───────────────────────

    /// <summary>Write a record's 4-byte little-endian length prefix through a Latin-1 text writer (chars 0–255
    /// map 1:1 to bytes, so the on-disk bytes equal the store-level prefix).</summary>
    public static void WritePrefix(TextWriter w, int len)
    {
        w.Write((char)(len & 0xFF));
        w.Write((char)((len >> 8) & 0xFF));
        w.Write((char)((len >> 16) & 0xFF));
        w.Write((char)((len >> 24) & 0xFF));
    }

    /// <summary>Decode a 4-byte little-endian length prefix read as Latin-1 chars.</summary>
    public static int PrefixLength(ReadOnlySpan<char> pre) =>
        pre[0] | (pre[1] << 8) | (pre[2] << 16) | (pre[3] << 24);
}
