// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>What <see cref="RecordFraming.ReadHeader"/> found at the head of a keyed store.</summary>
internal enum StoreFormat
{
    /// <summary>The file holds no bytes (or the host would not let this process read them). It records no
    /// records and therefore no attribute, so it can misread nothing and contradicts nothing — the ONE
    /// tolerated shape that is not a §14.9.27.4 GR10 conflict.</summary>
    Empty,

    /// <summary>A header this build understands; its <see cref="FixedFileAttributes"/> are the physical file's
    /// §9.1.6 fixed file attributes.</summary>
    Described,

    /// <summary>Bytes that are not a header this build understands — a file some other tool wrote, a plain
    /// sequential file opened through a keyed description, or a store from a build before the header existed.
    /// It is not a store this processor can interpret, so GR10's answer is '39': LOUD, where reading it as
    /// frames would be a silent misread.</summary>
    Foreign,
}

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
/// Two access shapes over the SAME record layout, differing in ONE thing — whether a STORE HEADER precedes the
/// frames:
/// <list type="bullet">
/// <item><b>Store-level</b> (<see cref="WriteStore"/>/<see cref="ReadStore"/>/<see cref="ReadHeader"/>) — the
/// whole-file rewrite/load the keyed connectors use (relative slots + the indexed persist order —
/// IndexedConnector.PersistOrder). It opens with the header described below.</item>
/// <item><b>Stream-level</b> (<see cref="WritePrefix"/>/<see cref="PrefixLength"/>/<see cref="FrameStarts"/>) —
/// the incremental prefix-per-record shape the sequential connector streams through its Latin-1 reader/writer
/// (chars 0–255 map 1:1 to bytes under Latin-1, so the char-shaped prefix is byte-identical to the store-level
/// one). It has NO header, deliberately: a header would stop a record sequential file being plain bytes and a
/// line sequential file being plain text, which is the interchange property those shapes exist for.</item>
/// </list>
/// <para>⛔ THE STORE HEADER IS THE §9.1.6 FIXED FILE ATTRIBUTES OF A RELATIVE OR INDEXED FILE, and it is IN
/// THE FILE — owner decision 2026-09-07 (kb/Work PB802): <i>"Let's match GNUCobol's implementation in spirit.
/// No sidecar of any type."</i> GnuCOBOL keeps an indexed file's key definitions in the ISAM file's own header;
/// this is the same answer for the same reason. Nothing is written beside a data file. Layout, all
/// little-endian, immediately followed by the frames:</para>
/// <code>
///   0   8  magic "CBNFSTR" + format version (currently 1)
///   8   1  organization    'R' relative · 'I' indexed
///   9   1  record type     'F' fixed · 'V' varying
///  10   4  minimum logical record size (bytes)
///  14   4  maximum logical record size (bytes)
///  18   2  key count (0 for a relative store)
///  20  ..  per key: offset(4) length(4) flags(1: bit0 DUPLICATES) suppress-length(2) suppress(Latin-1)
///          collation-length(1) collation(ASCII fingerprint)
/// </code>
/// <para>A format version this build does not know is <see cref="StoreFormat.Foreign"/> and NOT "attributes not
/// recorded": a store whose layout is unknown cannot have its frames located either, so the only safe answer is
/// to refuse it rather than to read it as if the header were absent.</para>
/// </summary>
internal static class RecordFraming
{
    /// <summary>The empty-slot (gap) tag — a length prefix of 0xFFFFFFFF with no payload.</summary>
    private const uint GapTag = 0xFFFFFFFF;

    /// <summary>The store header's magic: seven ASCII bytes, so a headerless file's first four bytes could
    /// only collide by naming a frame of 0x464E4243 (1.18 GB) and then spelling the rest exactly.</summary>
    private static ReadOnlySpan<byte> Magic => "CBNFSTR"u8;

    /// <summary>The store format's version, written as the byte immediately after <see cref="Magic"/> and
    /// CHECKED on every decode. A version this build does not know is <see cref="StoreFormat.Foreign"/>
    /// rather than "attributes not recorded": a layout this build cannot read cannot have its frames
    /// located either, so the only safe answer is to refuse the store instead of reading past it.</summary>
    private const byte FormatVersion = 1;

    /// <summary>The fixed part of the header — magic, organization, record type, the two record sizes and the
    /// key count. Per-key descriptors follow it.</summary>
    private const int FixedHeaderBytes = 20;

    // ── Store-level (byte) shape — the keyed connectors' whole-store persist/load ───────────────────────────

    /// <summary>Write the whole store: the §9.1.6 header, then one frame per ordinal position; null = an empty
    /// (gap) slot.
    /// <para>The header is stamped on EVERY persist, from the writing connector's own declared attributes, so a
    /// store always describes itself. That is not a re-establishment of §9.1.6's "at the time it is created":
    /// a connector whose attributes disagreed with the file's could not have opened it (§14.9.27.4 GR10 answered
    /// '39' before <c>OpenCore</c>), so every persist writes back the attributes the file already had — except
    /// under the documented <c>COBOLNET_KEYCHECK</c> opt-out, where a connector admitted with a different key
    /// table writes ITS key table, which is what opting out of the key check means.</para>
    /// <para>The stream is <see cref="HostFile.OpenAuxiliary"/>, share <see cref="FileShare.ReadWrite"/>: this
    /// is a bookkeeping handle on a path another file connector of the SAME run unit may hold open under
    /// §9.1.15 sharing, and it used to be the three-argument <c>FileStream</c> whose default is
    /// <see cref="FileShare.None"/> — the strictest form of kb/Work PB713's defect, forbidding every other
    /// handle rather than merely being forbidden by one.</para></summary>
    /// <param name="path">The physical file.</param>
    /// <param name="attributes">The writing connector's §9.1.6 fixed file attributes — the header's content.</param>
    /// <param name="frames">One entry per ordinal position; null = an empty (gap) slot. The frames are the
    /// records in the NATIVE character set.</param>
    /// <param name="codeSet">The file's §13.18.13 CODE-SET conversion, or null for the native character set
    /// (GR7). ⛔ It converts the PAYLOAD and not the frame: the 4-byte length prefix, the gap tag and this
    /// header are the §9.1.7.2 framing this processor adds, not data of the record (see
    /// <see cref="CodeSetConversion"/>).</param>
    public static void WriteStore(string path, FixedFileAttributes attributes, IReadOnlyList<string?> frames,
        CodeSetConversion? codeSet = null)
    {
        using var fs = HostFile.OpenAuxiliary(path, FileMode.Create, FileAccess.Write);
        WriteHeader(fs, attributes);
        Span<byte> len = stackalloc byte[4];
        foreach (string? frame in frames)
        {
            if (frame is null)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(len, GapTag);
                fs.Write(len);
                continue;
            }
            byte[] payload = Encoding.Latin1.GetBytes(codeSet is null ? frame : codeSet.ToMedium(frame));
            BinaryPrimitives.WriteUInt32LittleEndian(len, (uint)payload.Length);
            fs.Write(len);
            fs.Write(payload, 0, payload.Length);
        }
    }

    /// <summary>Read the whole store back: one entry per frame, null for a gap. A torn tail ends the store.
    /// A file whose header this build does not understand yields NO frames — its layout is unknown, so the
    /// bytes after it are not frames this build can locate. (A keyed OPEN never reaches here on such a file:
    /// <see cref="ReadHeader"/> answered <see cref="StoreFormat.Foreign"/> and the OPEN was '39' before
    /// <c>OpenCore</c> ran. The arm exists because <c>OPEN OUTPUT</c> loads before it truncates, and it must
    /// load nothing rather than garbage.)
    /// <para>The stream is <see cref="HostFile.OpenAuxiliary"/> for the reason <see cref="WriteStore"/> gives;
    /// here the three-argument <c>FileStream</c>'s default was <see cref="FileShare.Read"/>, which is the exact
    /// share mode that refused kb/Work PB713's varying-framing arm against the connector's own EXTEND writer.
    /// </para></summary>
    /// <param name="path">The physical file.</param>
    /// <param name="codeSet">The file's §13.18.13 CODE-SET conversion, or null — see
    /// <see cref="WriteStore"/>. The frames come back in the NATIVE character set (§13.18.13.4 GR6 a).</param>
    public static List<string?> ReadStore(string path, CodeSetConversion? codeSet = null)
    {
        var frames = new List<string?>();
        using var fs = HostFile.OpenAuxiliary(path, FileMode.Open, FileAccess.Read);
        if (DecodeHeader(fs) is null) return frames;   // empty, or a layout this build cannot locate frames in
        var len = new byte[4];
        while (FillExactly(fs, len, 4))
        {
            uint n = BinaryPrimitives.ReadUInt32LittleEndian(len);
            if (n == GapTag) { frames.Add(null); continue; }
            var payload = new byte[n];
            if (!FillExactly(fs, payload, (int)n)) break;
            string image = Encoding.Latin1.GetString(payload);
            frames.Add(codeSet is null ? image : codeSet.ToNative(image));
        }
        return frames;
    }

    /// <summary>The §9.1.6 fixed file attributes the store at <paramref name="path"/> RECORDS — the read half of
    /// §14.9.27.4 GR10 for the RELATIVE and INDEXED organizations, called once per OPEN by
    /// <c>KeyedConnector.FixedAttributeConflict</c>.
    /// <para>An I/O failure answers <see cref="StoreFormat.Empty"/> and never a conflict: this bounded read is
    /// bookkeeping, and the authority questions (§14.9.27.4 GR3's '37', §9.1.13.6 item 1's '30') are answered in
    /// ONE place each — <c>HostFile.Probe</c>, <c>HostFile.PermitsWrite</c> and <c>FileConnector.Open</c>'s catch
    /// — by the connector's own stream, not by guessing here.</para></summary>
    public static StoreFormat ReadHeader(string path, out FixedFileAttributes? attributes)
    {
        attributes = null;
        try
        {
            using var fs = HostFile.OpenAuxiliary(path, FileMode.Open, FileAccess.Read);
            if (fs.Length == 0) return StoreFormat.Empty;
            attributes = DecodeHeader(fs);
            return attributes is null ? StoreFormat.Foreign : StoreFormat.Described;
        }
        catch (IOException) { return StoreFormat.Empty; }
        catch (UnauthorizedAccessException) { return StoreFormat.Empty; }
        catch (ArgumentException) { return StoreFormat.Empty; }
        catch (NotSupportedException) { return StoreFormat.Empty; }
    }

    /// <summary>Write the store header. Kept beside <see cref="DecodeHeader"/> so the two halves of one byte
    /// layout cannot drift apart.</summary>
    private static void WriteHeader(Stream fs, FixedFileAttributes a)
    {
        var head = new byte[FixedHeaderBytes];
        Magic.CopyTo(head);
        head[Magic.Length] = FormatVersion;
        head[8] = a.Organization == FixedFileAttributes.Indexed ? (byte)'I' : (byte)'R';
        head[9] = a.Varying ? (byte)'V' : (byte)'F';
        BinaryPrimitives.WriteInt32LittleEndian(head.AsSpan(10), a.MinRecordSize);
        BinaryPrimitives.WriteInt32LittleEndian(head.AsSpan(14), a.MaxRecordSize);
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(18), (ushort)a.Keys.Count);
        fs.Write(head, 0, head.Length);
        Span<byte> word = stackalloc byte[4];
        foreach (var k in a.Keys)
        {
            BinaryPrimitives.WriteInt32LittleEndian(word, k.Offset);
            fs.Write(word);
            BinaryPrimitives.WriteInt32LittleEndian(word, k.Length);
            fs.Write(word);
            fs.WriteByte(k.Duplicates ? (byte)1 : (byte)0);
            // §12.4.5.6.4 GR6 admits ANY figurative-constant or literal SUPPRESS WHEN value, so it is written
            // with an explicit length rather than a delimiter: a value carrying a NUL or a line ending is legal.
            byte[] suppress = k.Suppress is null ? [] : Encoding.Latin1.GetBytes(k.Suppress);
            BinaryPrimitives.WriteUInt16LittleEndian(word[..2], (ushort)(k.Suppress is null ? 0 : suppress.Length + 1));
            fs.Write(word[..2]);
            if (k.Suppress is not null) fs.Write(suppress, 0, suppress.Length);
            byte[] collation = Encoding.ASCII.GetBytes(k.Collation);
            fs.WriteByte((byte)collation.Length);
            fs.Write(collation, 0, collation.Length);
        }
    }

    /// <summary>Decode the store header from the CURRENT position, leaving the stream on the first frame; null
    /// when the bytes are not a header this build understands (an empty file included). The ONE decoder — both
    /// <see cref="ReadStore"/> and <see cref="ReadHeader"/> reach the frames through it, so "where do the frames
    /// start" has exactly one answer.</summary>
    private static FixedFileAttributes? DecodeHeader(Stream fs)
    {
        var head = new byte[FixedHeaderBytes];
        if (!FillExactly(fs, head, FixedHeaderBytes)) return null;
        if (!head.AsSpan(0, Magic.Length).SequenceEqual(Magic) || head[Magic.Length] != FormatVersion)
            return null;
        string org = head[8] switch
        {
            (byte)'R' => FixedFileAttributes.Relative,
            (byte)'I' => FixedFileAttributes.Indexed,
            _ => "",
        };
        if (org.Length == 0) return null;
        if (head[9] is not ((byte)'F' or (byte)'V')) return null;
        int min = BinaryPrimitives.ReadInt32LittleEndian(head.AsSpan(10));
        int max = BinaryPrimitives.ReadInt32LittleEndian(head.AsSpan(14));
        if (min < 0 || max < 0) return null;
        int count = BinaryPrimitives.ReadUInt16LittleEndian(head.AsSpan(18));
        var keys = new List<FixedFileAttributes.KeyDescriptor>(count);
        var word = new byte[4];
        for (int i = 0; i < count; i++)
        {
            if (!FillExactly(fs, word, 4)) return null;
            int off = BinaryPrimitives.ReadInt32LittleEndian(word);
            if (!FillExactly(fs, word, 4)) return null;
            int len = BinaryPrimitives.ReadInt32LittleEndian(word);
            int dup = fs.ReadByte();
            if (dup < 0) return null;
            if (!FillExactly(fs, word, 2)) return null;
            int suppressLen = BinaryPrimitives.ReadUInt16LittleEndian(word);
            string? suppress = null;
            if (suppressLen > 0)
            {
                var buf = new byte[suppressLen - 1];   // the length is stored biased by one so 0 means "absent"
                if (!FillExactly(fs, buf, buf.Length)) return null;
                suppress = Encoding.Latin1.GetString(buf);
            }
            int collLen = fs.ReadByte();
            if (collLen < 0) return null;
            var coll = new byte[collLen];
            if (!FillExactly(fs, coll, collLen)) return null;
            keys.Add(new FixedFileAttributes.KeyDescriptor(off, len, dup != 0, suppress, Encoding.ASCII.GetString(coll)));
        }
        return new FixedFileAttributes(org, head[9] == (byte)'V', min, max, keys);
    }

    /// <summary>The byte offset of every frame in a STREAM-LEVEL framed physical file, in order (index =
    /// ordinal − 1) — a RECORD VARYING record-sequential file, which carries no header.
    /// This is the positioning index a §14.9.30.4 GR21 BACKWARD sequential READ needs on a RECORD VARYING file,
    /// whose frames are not uniformly wide the way a fixed record-sequential file's blocks are. Only the length
    /// prefixes are read — each payload is SEEKED over, never materialized — so the index costs one pass and no
    /// record storage. A torn tail ends the store, the same rule <see cref="ReadStore"/> applies.
    /// <para>⛔ IT INDEXES THE CONNECTOR'S OWN OPEN STREAM, NOT A PATH. Only a backward READ on an ALREADY-OPEN
    /// varying file asks for this index, so the presence question is already answered and asking the host again
    /// would be a second answer to it: <see cref="HostFile.Probe"/> is the ONE place the runtime asks the
    /// operating environment about a physical file and it is asked once per OPEN by <c>FileConnector.Open</c>,
    /// never by an organization body (<c>HostFileProbeDriftTests</c>, kb/Work PB323). Re-opening by path would
    /// also take a SECOND handle on a file the connector may hold under a deny-share mode, and could index a
    /// different file than the one the connector is reading. The walk moves the stream position and leaves it
    /// where it stops; every caller reaches <c>SequentialConnector.SeekToRecord</c> immediately afterwards,
    /// which seeks and discards the reader's buffer.</para></summary>
    public static List<long> FrameStarts(Stream fs)
    {
        var starts = new List<long>();
        fs.Seek(0, SeekOrigin.Begin);
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

    /// <summary>Does the STREAM-LEVEL record-length framing at <paramref name="path"/> PARSE — the whole of
    /// §14.9.27.4 GR10 for a RECORD VARYING record-sequential file, and the only §9.1.6 attribute such a file's
    /// format encodes.
    /// <para>§9.1.7.2 makes the prefix the file's own statement of its record type: "In record sequential files
    /// the length of each record is determined by any information the implementor may add to the record on the
    /// physical storage medium (such as record length headers)". A first prefix naming more bytes than the file
    /// holds is not a framed file at all, so a connector declaring the VARIABLE record type contradicts the
    /// medium and the OPEN is unsuccessful with '39' (§9.1.13.6 item 7). ⛔ THE BOUND IS THE FILE, NOT THE
    /// RECORD CLAUSE: a prefix that parses but falls outside the connector's VaryMin/VaryMax is exactly
    /// §9.1.13.2 item 3's '04' — "A READ statement is successfully executed but the physical record from the
    /// file is shorter than or longer than the minimum or maximum length of records allowed for the fixed file
    /// attributes for that file" — a SUCCESSFUL completion that a '39' at the OPEN would make unreachable.</para>
    /// <para>Only the FIRST frame is tested, and that is the rule rather than a shortcut: a torn or ragged TAIL
    /// already ends the store for <see cref="FrameStarts"/> and <see cref="ReadStore"/>, so a full walk would
    /// answer a question the framing has already settled, at a cost proportional to the file. It is also what
    /// the surveyed implementation does. A file shorter than one prefix states nothing and is not a conflict.
    /// An I/O failure likewise answers "no conflict" — the authority statuses are the connector's own stream's
    /// to raise.</para></summary>
    public static bool StreamFramingParses(string path)
    {
        try
        {
            using var fs = HostFile.OpenAuxiliary(path, FileMode.Open, FileAccess.Read);
            var len = new byte[4];
            if (!FillExactly(fs, len, 4)) return true;   // fewer than four bytes: nothing is claimed
            uint n = BinaryPrimitives.ReadUInt32LittleEndian(len);
            return n != GapTag && 4L + n <= fs.Length;
        }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
        catch (ArgumentException) { return true; }
        catch (NotSupportedException) { return true; }
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
