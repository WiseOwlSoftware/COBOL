// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>One record as it travels with its EXTENT TABLE (determination D-FRA (v); kb/Work PB1053) — between a
/// keyed store and its connector, and through the sort store: the record's characters and, when the
/// variable-length group that sent it had them, where each variable-length component ended. The table is framing
/// information, never part of the record (§12.4.5.11.4 GR1).
/// <para><paramref name="Ordinals"/> is an INDEXED record's release-ordinal vector (the prime key's slot first, then
/// one per alternate key, as the prime tree of <c>IndexedStore</c> holds it): the §14.9.30.4 GR26 order of its duplicates
/// under every key, carried with the record (kb/Work PB2693, PB3334). Null for every other frame.</para></summary>
internal readonly record struct StoredFrame(string Image, RecordExtents? Extents, long[]? Ordinals = null);

/// <summary>A decoded frame head: the payload's length in bytes, the EXTENT TABLE the frame carried (null when it
/// carried none) and an indexed store frame's release ordinals (null for every other frame) — see
/// <see cref="RecordFraming"/>'s class summary for the layout.</summary>
internal readonly record struct FrameHead(int PayloadLength, RecordExtents? Extents, long[]? Ordinals)
{
    /// <summary>The bytes the head occupies ahead of the payload — the ONE head-size formula
    /// (<see cref="RecordFraming.FrameHeadBytes"/>).</summary>
    public long HeadBytes => RecordFraming.FrameHeadBytes(Extents, Ordinals?.Length ?? 0);
}

/// <summary>Where <see cref="RecordFraming.ReadFrame"/> takes a frame's bytes from — the ONE frame decoder reads every
/// medium through this, so the media differ only in how they hold bytes and never in what a frame is (kb/Work
/// PB2754): a keyed store engine's leaf value, a stream-level file walked by its prefixes, and the sequential
/// connector's Latin-1 read channel.</summary>
internal interface IFrameMedium
{
    /// <summary>Fill <paramref name="into"/> from the current position and advance past it; false when the data ends
    /// first.</summary>
    bool Fill(Span<byte> into);

    /// <summary>Take the <paramref name="length"/>-byte payload that follows the head and advance past it (a medium
    /// that needs no payload seeks over it); false when the data ends first. ⛔ The decoder calls it only after
    /// <see cref="Holds"/>(<paramref name="length"/>) answered true, so a medium that can answer Holds does not ask
    /// it again here: on a stream every ask was a file-length query (kb/Work PB2754, review R1-1).</summary>
    bool TakePayload(int length);

    /// <summary>Can <paramref name="bytes"/> more bytes still follow the current position? Asked BEFORE storage sized
    /// by a count read from the medium is allocated, so a corrupt length word or table count ends the data instead of
    /// allocating what no file holds. A medium that cannot say answers true and relies on the short read.</summary>
    bool Holds(long bytes);
}

/// <summary>
/// The ONE record FRAME shared by every organization (DESIGN-runtime-library §2.2): a 4-byte little-endian length word
/// followed by that many Latin-1 payload bytes. Explicit length framing carries a record soundly — the legacy's
/// fixed-format all-0x00/0xFF heuristic could vanish a legitimate all-zero binary record — and the physical format is
/// implementor-defined by the spec, so only self-consistency matters: producer and consumer both run on this framing.
/// <para>⛔ THE LICENCE IS §9.1.7.2, NOT §9.1.13.6, and the correction matters (kb/Work PB292). This comment
/// cited §9.1.13.6's "externally-defined boundaries" until 2026-09-05 — a real sentence about a DIFFERENT
/// question: it is the I-O status 34 rule for writing past a physical FILE's boundary, not a statement about
/// record framing (feedback_a_real_clause_can_answer_a_different_question). The clause that actually grants
/// this is §9.1.7.2: "In record sequential files the length of each record is determined by any information the
/// implementor may add to the record on the physical storage medium (such as record length headers)" — this
/// prefix IS that header. §12.4.5.11.4 GR5 makes the same grant for the variable-length case where no RECORD
/// DELIMITER clause governs, which for WiseOwl COBOL is EVERY case (the clause is declined whole — COBOL1778,
/// docs/CONFORMANCE.md §2 row 26), and the determination is filed as Annex A.1 item 151 in
/// docs/CONFORMANCE.md §7. §12.4.5.11.4 GR1 is what this framing must not violate — "Any method used shall not
/// be reflected in the record area or the record size used within the function, method, or program" — so the
/// prefix is stripped before the record area is filled and never enters a record size a program can read.</para>
/// The frame travels on two media, with ONE head encoder and ONE head decoder for both:
/// <list type="bullet">
/// <item><b>A keyed store engine's leaf value</b> (<see cref="EncodeFrame"/>/<see cref="DecodeFrame"/>) — one record's
/// frame is the value a RELATIVE record-number tree or an INDEXED prime tree holds under the record's key
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4), with an indexed record's release ordinals in its head. The
/// store's own fixed file attributes, roots and coherence stamp live in its META PAGES (<c>Store.MetaPage</c>), never in a
/// header of frames: the whole-store image that once preceded the frames with a header, and was composed and loaded whole
/// at OPEN and CLOSE, is gone (kb/Work PB3335), and no reader of it is kept.</item>
/// <item><b>A stream-level file</b> (<see cref="ReadStreamFrame"/>/<see cref="CountFrames"/>/<see cref="StreamFramingParses"/>)
/// — the incremental prefix-per-record shape the sequential connector streams through its Latin-1 reader/writer
/// (chars 0–255 map 1:1 to bytes under Latin-1, so the head it writes and reads is the value's head bytes, converted once
/// at the channel). It has NO header, deliberately: a header would stop a record sequential file being plain bytes and
/// a line sequential file being plain text, which is the interchange property those shapes exist for.</item>
/// </list>
/// <para>⛔ ONE ENCODER AND ONE DECODER OF A FRAME HEAD, for both media (kb/Work PB2754): <see cref="EncodeFrameHead"/>
/// writes it and <see cref="ReadFrame"/> reads it, through an <see cref="IFrameMedium"/>. The decoder alone says that a
/// torn tail ends the data, so the record count, the backward-READ index, the forward READ and the OPEN's framing check
/// cannot disagree about which records a file holds (<c>FrameDecoderDriftTests</c>).</para>
/// <para>⛔ A FRAME MAY CARRY ITS RECORD'S EXTENT TABLE (determination D-FRA (v); kb/Work PB1053), on both media and
/// in the same way. A variable-length group record is sent as its contiguous image (§8.5.1.11.2), and with two or
/// more variable-length components the characters alone cannot say where each ended, so the frame carries that
/// beside the payload — the same §9.1.7.2 licence as the length itself. Bit 31 of the length word says a table
/// follows it (a payload of 2^31 characters is not representable), and the table precedes the payload:</para>
/// <code>
///   word    4  payload length | 0x80000000
///   count   4  component count n
///   n × 8      per component: fixed-run offset(4) length in characters(4)
///   ordinals   an indexed store value's release ordinals, 8 each (none on a stream)
///   payload    the record, exactly as before
/// </code>
/// <para>Like the prefix, the table is stripped before the record area is filled and never counts in a record size
/// (§12.4.5.11.4 GR1): it reaches the program only as <c>FileConnector.CurrentRecordExtents</c>, which the
/// generated decomposition of a CORRESPONDING variable-length group reads (<c>CobolContiguousLayout.Decompose</c>).
/// A line sequential line and a fixed-length block have no frame and therefore carry no table.</para>
/// <para>⛔ AN INDEXED VALUE CARRIES ITS RECORD'S RELEASE ORDINALS (kb/Work PB2693): key count × 8 bytes, the prime
/// key's slot first, after the length word and the extent table and before the payload. §14.9.30.4 GR26 orders an
/// alternate key's duplicates by release, and §14.9.35.4 GR24 b) moves a rewritten record last under each key it
/// changed, independently per key — so the order is one number per record PER KEY, which no single physical order of
/// the records can carry.</para>
/// </summary>
internal static class RecordFraming
{
    /// <summary>Bit 31 of a frame's length word: an EXTENT TABLE follows the word (see the class summary).</summary>
    private const uint ExtentFlag = 0x80000000;

    /// <summary>The bytes one release ordinal occupies in an indexed frame.</summary>
    private const int OrdinalBytes = 8;

    /// <summary>The bytes a frame's HEAD occupies ahead of its payload — its length word, its extent table and its
    /// release ordinals. The ONE head-size formula: the encoder sizes the head with it, and the sequential connector
    /// sizes a record's frame on its channel with it.</summary>
    public static long FrameHeadBytes(RecordExtents? extents, int ordinalSlots) =>
        4L + ExtentTableBytes(extents) + (long)OrdinalBytes * ordinalSlots;

    // ── A keyed store engine's leaf value ─────────────────────────────────────────────────────────────────────

    /// <summary>⛔ ONE RECORD'S FRAME AS BYTES — its head (<see cref="EncodeFrameHead"/>) and its payload in the file's
    /// channel character set — the value a keyed store engine's leaf entry holds (docs/rearchitecture/DESIGN-runtime-library.md
    /// section 2.2.1.3, decision 3: <i>"The leaf entry's value is the ONE frame shape"</i>). The payload is converted by the
    /// file's §13.18.13 CODE-SET (<paramref name="codeSet"/>, null for the native character set); the head is the §9.1.7.2
    /// information this processor adds and is never converted (2.2.1.5).
    /// <para>The file coded character set's STRICT encoding (kb/Work PB690): the keyed WRITE/REWRITE refused any record
    /// holding a character with no byte image ('91'), so an exception here is a missed refusal — loud, raised before the
    /// file is touched — never a silent '?'.</para></summary>
    /// <exception cref="InvalidOperationException">The frame carries a number of release ordinals other than
    /// <paramref name="ordinalSlots"/>.</exception>
    public static byte[] EncodeFrame(StoredFrame frame, int ordinalSlots, CodeSetConversion? codeSet)
    {
        if ((frame.Ordinals?.Length ?? 0) != ordinalSlots)
            throw new InvalidOperationException($"a frame carries {frame.Ordinals?.Length ?? 0} release ordinals where "
                + $"its store's key count makes it {ordinalSlots}");
        byte[] payload = FileCharacterSet.Medium.GetBytes(FileCharacterSet.ToChannel(frame.Image, codeSet));
        int headBytes = (int)FrameHeadBytes(frame.Extents, ordinalSlots);
        var value = new byte[headBytes + payload.Length];
        EncodeFrameHead(value, payload.Length, frame.Extents, frame.Ordinals);
        payload.CopyTo(value, headBytes);
        return value;
    }

    /// <summary>The record a keyed store engine's leaf value holds (<see cref="EncodeFrame"/>'s inverse), decoded by the
    /// ONE frame decoder (<see cref="ReadFrame"/>) and returned in the NATIVE character set (§13.18.13.4 GR6 a)); null
    /// when the value is not exactly one record's frame — a value this writer cannot have produced, which its reader
    /// answers as the torn page it is ('30', §9.1.13.6 item 1).</summary>
    public static StoredFrame? DecodeFrame(byte[] value, int ordinalSlots, CodeSetConversion? codeSet)
    {
        var medium = new ValueMedium(value);
        return ReadFrame(ref medium, ordinalSlots, out FrameHead head) && medium.At == value.Length
            ? new StoredFrame(FileCharacterSet.FromChannel(medium.Payload!, codeSet), head.Extents, head.Ordinals)
            : null;
    }

    /// <summary>A leaf value in memory as a frame medium, read from <see cref="At"/>. The payload is taken as its Latin-1
    /// characters (one per byte), the channel shape the record is converted from.</summary>
    private struct ValueMedium(byte[] value) : IFrameMedium
    {
        /// <summary>The next byte to read.</summary>
        public int At;

        /// <summary>The payload the last <see cref="TakePayload"/> took.</summary>
        public string? Payload;

        public bool Fill(Span<byte> into)
        {
            if (!Holds(into.Length)) return false;
            value.AsSpan(At, into.Length).CopyTo(into);
            At += into.Length;
            return true;
        }

        public bool TakePayload(int length)
        {
            Payload = Encoding.Latin1.GetString(value, At, length);   // ReadFrame asked Holds(length) first
            At += length;
            return true;
        }

        public readonly bool Holds(long bytes) => bytes <= value.Length - At;
    }

    // ── A stream-level file ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The number of frames that START in [<paramref name="from"/>, <paramref name="to"/>) of a STREAM-LEVEL
    /// framed physical file — a RECORD VARYING record-sequential file, which carries no header. A walk that STORES
    /// NOTHING: only the frame heads are read, each payload is SEEKED over, never materialized, and no offset is kept
    /// (kb/Work PB3350; docs/rearchitecture/DESIGN-runtime-library.md 2.2.3 — the count used to build a list of every
    /// frame's start only to take its length). Where the frames end is the one decoder's answer
    /// (<see cref="ReadStreamFrame"/>), so the count is exactly the records a forward READ delivers. Where a frame
    /// STARTS, for the backward READ, is <see cref="FrameIndex"/>'s answer, from the same walk.
    /// <para>⛔ IT WALKS THE STREAM IT IS GIVEN, NOT A PATH. Re-opening by path would be a second answer to the presence
    /// question (<see cref="HostFile.Probe"/> is the ONE place the runtime asks the operating environment about a physical
    /// file, once per OPEN by <c>FileConnector.Open</c>, never by an organization body — <c>HostFileProbeDriftTests</c>,
    /// kb/Work PB323), and could take a second handle on a file the connector holds under a deny-share mode. The walk
    /// moves the stream position and leaves it where it stops. <c>SequentialConnector.CountRecords</c> hands it an
    /// auxiliary stream, because it runs where the connector's own handle is a writer.</para>
    /// <para><paramref name="from"/> and <paramref name="to"/> bound the walk to the frames that START in
    /// [<paramref name="from"/>, <paramref name="to"/>): <paramref name="from"/> shall be a frame boundary (kb/Work PB2692
    /// counts only the frames another run unit appended since the last count).</para></summary>
    public static long CountFrames(Stream fs, long from = 0, long to = long.MaxValue)
    {
        fs.Seek(from, SeekOrigin.Begin);
        var medium = new StreamMedium(fs);
        long frames = 0;
        for (long at = from; at < to && ReadStreamFrame(ref medium, out _); at = fs.Position)
            frames++;
        return frames;
    }

    /// <summary>Does the STREAM-LEVEL record-length framing at <paramref name="path"/> PARSE — the whole of
    /// §14.9.27.4 GR10 for a RECORD VARYING record-sequential file, and the only §9.1.6 attribute such a file's
    /// format encodes.
    /// <para>§9.1.7.2 makes the prefix the file's own statement of its record type: "In record sequential files
    /// the length of each record is determined by any information the implementor may add to the record on the
    /// physical storage medium (such as record length headers)". A first frame that the one decoder cannot read
    /// (<see cref="ReadStreamFrame"/>: a head naming more bytes than the file holds, a table this writer cannot have
    /// produced) is not a framed file at all, so a connector declaring the VARIABLE record type contradicts the medium
    /// and the OPEN is unsuccessful with '39' (§9.1.13.6 item 7). ⛔ THE BOUND IS THE FILE, NOT THE RECORD CLAUSE: a
    /// prefix that parses but falls outside the connector's VaryMin/VaryMax is exactly §9.1.13.2 item 3's '04' — "A READ
    /// statement is successfully executed but the physical record from the file is shorter than or longer than the
    /// minimum or maximum length of records allowed for the fixed file attributes for that file" — a SUCCESSFUL
    /// completion that a '39' at the OPEN would make unreachable.</para>
    /// <para>Only the FIRST frame is tested, and that is the rule rather than a shortcut: a torn or ragged TAIL
    /// already ends the data for every reader, so a full walk would answer a question the framing has already settled,
    /// at a cost proportional to the file. It is also what the surveyed implementation does. A file shorter than one
    /// length word states nothing and is not a conflict. An I/O failure likewise answers "no conflict" — the authority
    /// statuses are the connector's own stream's to raise.</para></summary>
    public static bool StreamFramingParses(string path)
    {
        try
        {
            using var fs = HostFile.OpenAuxiliary(path, FileMode.Open, FileAccess.Read);
            var medium = new StreamMedium(fs);
            return !medium.Holds(4) || ReadStreamFrame(ref medium, out _);   // fewer than a length word: nothing is claimed
        }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
        catch (ArgumentException) { return true; }
        catch (NotSupportedException) { return true; }
    }

    /// <summary>A stream-level framed file walked from its current position as a frame medium: the heads are read
    /// and each payload is SEEKED over, never materialized.
    /// <para>⛔ THE WALK READS THE FILE AS IT STOOD WHEN THE MEDIUM WAS MADE: the stream's length is asked ONCE, here,
    /// and every <see cref="Holds"/> and <see cref="Fill"/> is bounded by that snapshot. A stream's length is an
    /// uncached operating-environment query whenever the handle admits other writers, and every bookkeeping handle
    /// does (<c>HostFile.OpenAuxiliary</c> shares <see cref="FileShare.ReadWrite"/>), so asking it per frame made it
    /// the dominant cost of an O(file) walk (kb/Work PB2754, review R1-1). Bytes another run unit appends during the
    /// walk lie past the snapshot and end the data there, as an append that has not completed does; the frame
    /// index and the record count this walk produces are point-in-time answers already.</para></summary>
    internal readonly struct StreamMedium(Stream fs) : IFrameMedium
    {
        /// <summary>The stream's length when the walk began: the end of the data this medium reads.</summary>
        private readonly long _end = fs.Length;

        public bool Fill(Span<byte> into) => Holds(into.Length) && FillExactly(fs, into);

        public bool TakePayload(int length)
        {
            fs.Seek(length, SeekOrigin.Current);   // ReadFrame asked Holds(length) first
            return true;
        }

        public bool Holds(long bytes) => bytes <= _end - fs.Position;
    }

    private static bool FillExactly(Stream s, Span<byte> buf) =>
        s.ReadAtLeast(buf, buf.Length, throwOnEndOfStream: false) == buf.Length;

    // ── THE frame head — one encoder, one decoder, for both media (kb/Work PB2754) ────────────────────────────

    /// <summary>⛔ THE ONE FRAME DECODER. Reads the frame at <paramref name="medium"/>'s position — its length word,
    /// its EXTENT TABLE when bit 31 says one follows, <paramref name="ordinalSlots"/> release ordinals, and its
    /// payload — and answers true for a record's frame. It alone decides what a damaged file means, so no reader can
    /// answer it differently (kb/Work PB2754): a frame the data ends inside — or whose head this writer cannot have
    /// produced (a table component of negative length) — is the END of the data, false: a torn tail ends the data. A
    /// count or a length is checked against what the medium still holds (<see cref="IFrameMedium.Holds"/>) BEFORE
    /// anything is sized by it. ⛔ THE FRAMED DATA ENDS HERE, for every reader of every medium: the record count, the
    /// backward-READ index, the forward READ and the OPEN's framing check all stop at this frame.
    /// The framing is the §9.1.7.2 information "the implementor may add to the record on the physical storage medium";
    /// none of it reaches the record area (§12.4.5.11.4 GR1).</summary>
    public static bool ReadFrame<TMedium>(ref TMedium medium, int ordinalSlots, out FrameHead head)
        where TMedium : struct, IFrameMedium
    {
        head = default;
        Span<byte> word = stackalloc byte[OrdinalBytes];
        if (!medium.Fill(word[..4])) return false;
        uint lengthWord = BinaryPrimitives.ReadUInt32LittleEndian(word);
        RecordExtents? extents = null;
        if ((lengthWord & ExtentFlag) != 0 && (extents = ReadExtentTable(ref medium)) is null) return false;
        long[]? ordinals = null;
        if (ordinalSlots > 0)
        {
            ordinals = new long[ordinalSlots];
            for (int k = 0; k < ordinalSlots; k++)
            {
                if (!medium.Fill(word)) return false;
                ordinals[k] = BinaryPrimitives.ReadInt64LittleEndian(word);
            }
        }
        int payloadLength = (int)(lengthWord & ~ExtentFlag);
        if (!medium.Holds(payloadLength) || !medium.TakePayload(payloadLength)) return false;
        head = new FrameHead(payloadLength, extents, ordinals);
        return true;
    }

    /// <summary><see cref="ReadFrame"/> over a STREAM-LEVEL file, whose frames carry no release ordinals: true for a
    /// record's frame. The one answer for the forward READ (<c>SequentialConnector.NextFrame</c>), the record count and
    /// the backward-READ index (<see cref="FrameIndex"/>) and the OPEN's framing check (<see cref="StreamFramingParses"/>).</summary>
    public static bool ReadStreamFrame<TMedium>(ref TMedium medium, out FrameHead head)
        where TMedium : struct, IFrameMedium =>
        ReadFrame(ref medium, ordinalSlots: 0, out head);

    /// <summary>The extent table that follows a length word: its count, then (fixed-run offset, length) per
    /// component. Null when the data ends inside it or a length is negative (a table this writer cannot have
    /// produced) — <see cref="ReadFrame"/> then answers the end of the data.</summary>
    private static RecordExtents? ReadExtentTable<TMedium>(ref TMedium medium) where TMedium : struct, IFrameMedium
    {
        Span<byte> component = stackalloc byte[8];
        if (!medium.Fill(component[..4])) return null;
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(component);
        if (!medium.Holds(8L * count)) return null;
        var fixedAt = new int[count];
        var lengths = new int[count];
        for (int k = 0; k < count; k++)
        {
            if (!medium.Fill(component)) return null;
            fixedAt[k] = BinaryPrimitives.ReadInt32LittleEndian(component);
            lengths[k] = BinaryPrimitives.ReadInt32LittleEndian(component[4..]);
            if (lengths[k] < 0) return null;
        }
        return new RecordExtents(fixedAt, lengths);
    }

    /// <summary>⛔ THE ONE FRAME-HEAD ENCODER, for both media: into the first <see cref="FrameHeadBytes"/> bytes of
    /// <paramref name="into"/>, the length word (<paramref name="payloadLength"/>, with bit 31 when
    /// <paramref name="extents"/> is given), the extent table and the release ordinals. The payload follows it.
    /// The sequential connector writes these bytes through its Latin-1 channel, one character per byte.</summary>
    public static void EncodeFrameHead(Span<byte> into, int payloadLength, RecordExtents? extents, ReadOnlySpan<long> ordinals)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(into, (uint)payloadLength | (extents is null ? 0u : ExtentFlag));
        int at = 4;
        if (extents is not null) at += EncodeExtentTable(into[at..], extents);
        foreach (long o in ordinals)
        {
            BinaryPrimitives.WriteInt64LittleEndian(into[at..], o);
            at += OrdinalBytes;
        }
    }

    /// <summary>A VOID table of <paramref name="count"/> components: every fixed-run offset is -1, which no record
    /// layout has, so it corresponds to no receiving record (<see cref="RecordExtents.Describes"/>) and the record it
    /// frames reads back by the take step. What an in-place REWRITE leaves in a frame whose table cannot hold the
    /// replacing record's — the frame's size is fixed (§14.9.35.4 GR16), and keeping the replaced record's table
    /// would describe characters that are no longer there.</summary>
    public static RecordExtents VoidExtents(int count)
    {
        var fixedAt = new int[count];
        Array.Fill(fixedAt, -1);
        return new RecordExtents(fixedAt, new int[count]);
    }

    /// <summary>The bytes an extent table occupies on the medium (0 when there is none).</summary>
    public static int ExtentTableBytes(RecordExtents? extents) => extents is null ? 0 : 4 + (8 * extents.Count);

    /// <summary>The ONE encoder of an extent table — its count, then (fixed-run offset, length) per component — into
    /// the first <see cref="ExtentTableBytes"/> bytes of <paramref name="into"/>; returns that count of bytes. The
    /// sequential connector's in-place REWRITE replaces a frame's table with these bytes.</summary>
    public static int EncodeExtentTable(Span<byte> into, RecordExtents extents)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(into, (uint)extents.Count);
        for (int k = 0; k < extents.Count; k++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(into[(4 + (8 * k))..], extents.FixedAt[k]);
            BinaryPrimitives.WriteInt32LittleEndian(into[(8 + (8 * k))..], extents.Lengths[k]);
        }
        return ExtentTableBytes(extents);
    }
}
