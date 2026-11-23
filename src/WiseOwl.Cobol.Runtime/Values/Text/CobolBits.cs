// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The <c>USAGE BIT</c> record-image codec (ISO §2023:13.18.60.4 GR5 "bits shall be used"; §2023:8.5.1.6.3 alignment;
/// design D19, fix-queue PB43).
///
/// <para><b>Two representations, one value.</b> A boolean item's VALUE carrier is a <c>'0'</c>/<c>'1'</c> string —
/// one character per boolean position — for BOTH usages, because every MOVE / compare / ref-mod / fill path is
/// character-shaped and §2023:13.18.40.4 GR14 lets a boolean character be represented as an alphanumeric character. What
/// differs is what the item OCCUPIES in a record image:</para>
/// <list type="bullet">
///   <item><b>USAGE DISPLAY</b> (implied when no USAGE clause is written, §2023:13.18.60.3 SR13(b)) — the carrier IS the
///         image, one character per position, per §2023:13.18.60.4 GR7's "alphanumeric coded character set". These
///         helpers are never called for it.</item>
///   <item><b>USAGE BIT</b> — the image is the PACKED bits, <c>ceil(n / 8)</c> characters. That is what these
///         helpers do.</item>
/// </list>
///
/// <para>⛔ <b>THE BIT ORDER IS HIGH-ORDER-FIRST, AND IT IS NOT ARBITRARY.</b> §2023:8.5.1.6.3 speaks throughout of
/// "the first bit position of the first available byte" and lays successive items at "the next bit position", so
/// boolean position 1 is the most significant bit of the first byte and the run fills toward the low-order end.
/// A trailing partial byte is zero-filled — those are §2023:8.5.1.6.3's implicit filler bits, which §2023:15.50.4 RV5
/// requires be counted in the length (and they are: the width is the ceiling).</para>
///
/// <para>⚠ <b>A run may span several ITEMS.</b> Two bit items of the SAME LEVEL share a byte (§2023:8.5.1.6.3's
/// next-bit-position rule), so the emitter concatenates their carriers and packs ONCE — which is why these take a
/// whole run's bits rather than one item's.</para>
/// </summary>
public static class CobolBits
{
    private const int BitsPerByte = 8;

    /// <summary>Pack EVERY boolean position the carrier holds — the count is <paramref name="bits"/>.Length.
    /// The shape a REFERENCE-MODIFIED usage-bit operand needs: ISO/IEC 1989:2023 §2023:8.4.3.3.4 GR5c gives the slice
    /// its own position count, which may be a runtime expression, so the caller never has to re-derive it (and
    /// a second evaluation of the slice expression is exactly what this overload avoids).</summary>
    public static string Pack(string bits) => Pack(bits, bits.Length);

    /// <summary>Pack a run's <c>'0'</c>/<c>'1'</c> carrier into its record image — <c>ceil(count / 8)</c>
    /// characters, high-order bit first, trailing filler bits zero. Any character other than <c>'1'</c> reads as
    /// zero, which makes a space-padded or uninitialized carrier decode as the all-zero boolean value §2023:13.18.63
    /// gives it rather than throwing.</summary>
    public static string Pack(string bits, int count)
    {
        // ⛔ ONE bit-order law, and no extra allocation to get it: the fresh image is all-zero, so blitting only
        // the '1' positions reproduces Pack's contract exactly ("any character other than '1' reads as zero",
        // trailing filler zero) while sharing the high-order-first arithmetic with WriteWindow. Going through
        // WriteWindow itself would have cost a throwaway zero STRING plus its ToCharArray on a path every
        // whole-group image of a bit run takes.
        var image = new char[(count + BitsPerByte - 1) / BitsPerByte];
        Blit(image, 0, bits, System.Math.Min(count, bits.Length));
        return new string(image);
    }

    /// <summary>Unpack <paramref name="count"/> boolean positions from a record image back into the
    /// <c>'0'</c>/<c>'1'</c> carrier — the exact inverse of <see cref="Pack"/>. A short image (the pad a short
    /// record legitimately deposits) yields <c>'0'</c> for the missing positions.</summary>
    public static string Unpack(string image, int count) => ReadWindow(image, 0, count);

    /// <summary>
    /// Read <paramref name="count"/> boolean positions starting at ABSOLUTE bit position
    /// <paramref name="startBit"/> of a byte image — the §2023:13.18.44.4 GR1 storage association read for a bit
    /// item that shares its storage area with other data descriptions ("Storage association for the subject of
    /// the entry starts at the first BIT of the data item referenced by data-name-2 and continues over an area
    /// sufficient to contain the number of BITS required"). <see cref="Unpack"/> is the
    /// <paramref name="startBit"/> = 0 case.
    /// <para>A position past the end of the image yields <c>'0'</c>, the same benign decode a short record's
    /// pad gets — §2023:13.18.63's all-zero boolean initial state rather than a throw.</para>
    /// </summary>
    public static string ReadWindow(string image, int startBit, int count)
    {
        var bits = new char[count];
        for (int i = 0; i < count; i++)
        {
            int p = startBit + i, b = p / BitsPerByte;
            bits[i] = b >= 0 && b < image.Length
                      && (StorageByte.ImageByte(image[b]) & (1 << (BitsPerByte - 1 - p % BitsPerByte))) != 0 ? '1' : '0';
        }
        return new string(bits);
    }

    /// <summary>
    /// Splice <paramref name="bits"/> into <paramref name="image"/> starting at ABSOLUTE bit position
    /// <paramref name="startBit"/>, returning the new image and leaving EVERY OTHER BIT of it untouched — the
    /// receiving twin of <see cref="ReadWindow"/>, and the reason a sub-byte bit item can share a byte with its
    /// siblings (§2023:8.5.1.6.3: "an elementary bit data item immediately following an elementary bit data item or
    /// bit group item of the same level" is placed "at the next bit position in storage"). A write that would
    /// run past the image stops at its end — the same truncation a short window gets everywhere else.
    /// </summary>
    public static string WriteWindow(string image, int startBit, string bits)
    {
        var buf = image.ToCharArray();
        Blit(buf, startBit, bits, bits.Length);
        return new string(buf);
    }

    /// <summary>
    /// <see cref="WriteWindow"/> into a record image held in a mutable <paramref name="buffer"/>: splice
    /// <paramref name="bits"/> at ABSOLUTE bit position <paramref name="startBit"/>, leaving every other bit untouched
    /// (§2023:8.5.1.6.3's shared byte). A .NET host entry's published record composes its image member by member in one
    /// buffer (DESIGN-external-repository §4.3), so a bit member is written in place rather than through a new string
    /// per member; the bit-order law is the same <see cref="Blit"/>.
    /// </summary>
    public static void WriteWindowInto(char[] buffer, int startBit, string bits) => Blit(buffer, startBit, bits, bits.Length);

    /// <summary>⛔ THE ONE WRITE-SIDE bit-order law: set/clear <paramref name="count"/> boolean positions of
    /// <paramref name="buf"/> starting at bit <paramref name="startBit"/>, high-order bit of each byte first.
    /// Shared by <see cref="Pack"/> (into a fresh all-zero image) and <see cref="WriteWindow"/> (into a copy of
    /// an existing one) so the two cannot disagree about which bit is position 1. A position past the end of the
    /// buffer is dropped — the same truncation a short window gets everywhere else.</summary>
    private static void Blit(char[] buf, int startBit, string bits, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int p = startBit + i, b = p / BitsPerByte;
            if (b < 0) continue;
            if (b >= buf.Length) break;
            int mask = 1 << (BitsPerByte - 1 - p % BitsPerByte);
            int bite = StorageByte.ImageByte(buf[b]);
            buf[b] = StorageByte.ToChar((byte)(bits[i] == '1' ? bite | mask : bite & ~mask));
        }
    }

    /// <summary>The slice of a run's unpacked carrier belonging to one item — the emitter distributes a shared
    /// byte back to the several fields that occupy it.</summary>
    public static string Slice(string carrier, int offset, int count) =>
        offset >= carrier.Length ? new string('0', count)
        : carrier.Substring(offset, System.Math.Min(count, carrier.Length - offset)).PadRight(count, '0');

    /// <summary>The carrier of a TABLE of bit occurrences whose occurrences sit <paramref name="stride"/> bits apart:
    /// each occurrence's own carrier, padded with zero filler bits to <paramref name="stride"/> except the last —
    /// §2023:13.18.1.4 GR2 ("An ALIGNED clause specified for a multiple-occurrence data item applies to each occurrence of
    /// that item") puts the subject, or an ALIGNED member, of every occurrence at the first bit of a byte, so the
    /// occurrences are a whole number of bytes apart (<c>BitLayout.StrideBits</c>, kb/Work PB3167), and §2023:8.5.1.6.3
    /// generates filler only "as needed to advance alignment to a required natural boundary for the next item", so the
    /// last occurrence has none after it (<c>BitLayout.TableBits</c>). With every occurrence exactly
    /// <paramref name="stride"/> bits wide (a table packed bit by bit) it is the plain concatenation.
    /// <para>The padding is the table's STORED implicit filler (§2023:8.5.1.6.3 "implicitly described as a filler elementary bit
    /// data item"; kb/Work PB3564): <paramref name="pads"/> holds the <c>stride − width</c> positions that follow each
    /// occurrence but the last, one gap after another, as <see cref="StridePads"/> reads them back — so what a group move
    /// wrote there is what the next read of the group gives. A pad string shorter than a gap reads zero beyond its end.</para></summary>
    public static string Strided(string[] occurrences, int stride, string pads)
    {
        var sb = new System.Text.StringBuilder();
        int pad = occurrences.Length > 0 ? System.Math.Max(0, stride - occurrences[0].Length) : 0;
        for (int i = 0; i < occurrences.Length; i++)
        {
            sb.Append(occurrences[i]);
            if (i < occurrences.Length - 1 && pad > 0) sb.Append(Slice(pads, i * pad, pad));
        }
        return sb.ToString();
    }

    /// <summary>The inverse of <see cref="Strided"/>'s padding: the filler positions between the <paramref name="count"/>
    /// occurrences of a <paramref name="width"/>-bit table laid <paramref name="stride"/> bits apart, read from the table's
    /// unpacked <paramref name="carrier"/> starting at <paramref name="at"/>, one gap after another — the occurrences'
    /// own positions are <see cref="Slice"/>d out by the caller; this is what lies between them (kb/Work PB3564).</summary>
    public static string StridePads(string carrier, int at, int width, int stride, int count)
    {
        int pad = stride - width;
        if (pad <= 0 || count < 2) return "";
        var sb = new System.Text.StringBuilder((count - 1) * pad);
        for (int i = 0; i < count - 1; i++) sb.Append(Slice(carrier, at + i * stride + width, pad));
        return sb.ToString();
    }
}
