// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The <c>USAGE BIT</c> record-image codec (ISO §13.18.60.4 GR5 "bits shall be used"; §8.5.1.6.3 alignment;
/// design D19, fix-queue PB43).
///
/// <para><b>Two representations, one value.</b> A boolean item's VALUE carrier is a <c>'0'</c>/<c>'1'</c> string —
/// one character per boolean position — for BOTH usages, because every MOVE / compare / ref-mod / fill path is
/// character-shaped and §13.18.40.4 GR14 lets a boolean character be represented as an alphanumeric character. What
/// differs is what the item OCCUPIES in a record image:</para>
/// <list type="bullet">
///   <item><b>USAGE DISPLAY</b> (implied when no USAGE clause is written, §13.18.60.3 SR13(b)) — the carrier IS the
///         image, one character per position, per §13.18.60.4 GR7's "alphanumeric coded character set". These
///         helpers are never called for it.</item>
///   <item><b>USAGE BIT</b> — the image is the PACKED bits, <c>ceil(n / 8)</c> characters. That is what these
///         helpers do.</item>
/// </list>
///
/// <para>⛔ <b>THE BIT ORDER IS HIGH-ORDER-FIRST, AND IT IS NOT ARBITRARY.</b> §8.5.1.6.3 speaks throughout of
/// "the first bit position of the first available byte" and lays successive items at "the next bit position", so
/// boolean position 1 is the most significant bit of the first byte and the run fills toward the low-order end.
/// A trailing partial byte is zero-filled — those are §8.5.1.6.3's implicit filler bits, which §15.50.4 r5
/// requires be counted in the length (and they are: the width is the ceiling).</para>
///
/// <para>⚠ <b>A run may span several ITEMS.</b> Two bit items of the SAME LEVEL share a byte (§8.5.1.6.3's
/// next-bit-position rule), so the emitter concatenates their carriers and packs ONCE — which is why these take a
/// whole run's bits rather than one item's.</para>
/// </summary>
public static class CobolBits
{
    private const int BitsPerByte = 8;

    /// <summary>Pack a run's <c>'0'</c>/<c>'1'</c> carrier into its record image — <c>ceil(count / 8)</c>
    /// characters, high-order bit first, trailing filler bits zero. Any character other than <c>'1'</c> reads as
    /// zero, which makes a space-padded or uninitialized carrier decode as the all-zero boolean value §13.18.63
    /// gives it rather than throwing.</summary>
    /// <summary>Pack EVERY boolean position the carrier holds — the count is <paramref name="bits"/>.Length.
    /// The shape a REFERENCE-MODIFIED usage-bit operand needs: ISO/IEC 1989:2023 §8.4.3.3.4 GR5c gives the slice
    /// its own position count, which may be a runtime expression, so the caller never has to re-derive it (and
    /// a second evaluation of the slice expression is exactly what this overload avoids).</summary>
    public static string Pack(string bits) => Pack(bits, bits.Length);

    public static string Pack(string bits, int count)
    {
        int bytes = (count + BitsPerByte - 1) / BitsPerByte;
        var image = new char[bytes];
        for (int i = 0; i < count; i++)
        {
            if (i >= bits.Length || bits[i] != '1') continue;
            image[i / BitsPerByte] |= (char)(1 << (BitsPerByte - 1 - i % BitsPerByte));
        }
        return new string(image);
    }

    /// <summary>Unpack <paramref name="count"/> boolean positions from a record image back into the
    /// <c>'0'</c>/<c>'1'</c> carrier — the exact inverse of <see cref="Pack"/>. A short image (the pad a short
    /// record legitimately deposits) yields <c>'0'</c> for the missing positions.</summary>
    public static string Unpack(string image, int count)
    {
        var bits = new char[count];
        for (int i = 0; i < count; i++)
        {
            int b = i / BitsPerByte;
            bits[i] = b < image.Length && (image[b] & (1 << (BitsPerByte - 1 - i % BitsPerByte))) != 0 ? '1' : '0';
        }
        return new string(bits);
    }

    /// <summary>The slice of a run's unpacked carrier belonging to one item — the emitter distributes a shared
    /// byte back to the several fields that occupy it.</summary>
    public static string Slice(string carrier, int offset, int count) =>
        offset >= carrier.Length ? new string('0', count)
        : carrier.Substring(offset, System.Math.Min(count, carrier.Length - offset)).PadRight(count, '0');

    /// <summary>The UTF-16BE byte serialization of a NATIONAL string — one char per BYTE, two bytes (high-order
    /// first) per national position (D-N1: national is UTF-16, one code unit per character position). ⛔ THE ONE
    /// national→bytes reduction: <c>CobolIntrinsics.Convert</c>'s NAT source arm and the compiler-emitted CONVERT
    /// ANY raw-storage channel (a national leaf's storage bits, §15.19.3 r7) both ride it — never a second
    /// serializer (fix-queue PB59 family 5b). Not a BIT codec, but it lives here because this class is where the
    /// storage-byte reductions are: the char==byte string convention is <see cref="Pack"/>'s.</summary>
    public static string NatBytes(string s)
    {
        var image = new char[s.Length * 2];
        for (int i = 0; i < s.Length; i++) { image[2 * i] = (char)(s[i] >> 8); image[2 * i + 1] = (char)(s[i] & 0xFF); }
        return new string(image);
    }
}
