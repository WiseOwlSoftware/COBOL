// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>⛔ THE NATIVE COLLATING SEQUENCE'S EXTREMES — the ONE answer to "which character is HIGH-VALUE / LOW-VALUE
/// in the native sequence", read by every site that answers it (the compiler's figurative fill, the SPECIAL-NAMES
/// HIGH-VALUE, the collating-table extremes, the runtime collation defaults, the no-PCS initial fill, the storage
/// byte law below). ISO/IEC 1989:2023 §8.3.3.6.4 GR6: <i>"the high-value format represents the character, or
/// multiple-character combination, that has the highest ordinal position in the runtime collating sequence"</i>;
/// GR7 the same for LOW-VALUE at the lowest. The native alphanumeric and national character sets are the 65,536
/// UTF-16 code units in code-unit order (implementor items 31/188, D-N1/D-N3), so both classes answer
/// <b>U+FFFF / U+0000</b> — owner decision kb/Work R52 (PB1093 option A). Before it the alphanumeric arm answered
/// U+00FF, which left U+0100..U+FFFF ordered ABOVE "the highest character".</summary>
public static class NativeCollatingSequence
{
    /// <summary>HIGH-VALUE of the native alphanumeric AND national sequences — U+FFFF, the highest code unit.</summary>
    public const char HighValue = '￿';

    /// <summary>LOW-VALUE of the native alphanumeric AND national sequences — U+0000, the lowest code unit.</summary>
    public const char LowValue = '\0';
}

/// <summary>⛔ THE STORAGE-BYTE LAW — the ONE mapping between a BYTE and the character that stands for it, applied
/// wherever a byte becomes a character or a character becomes a byte: a byte-form leaf's image (binary, IEEE, pointer,
/// bit, national pair) in a record or REDEFINES image, and the one-byte record medium of a fixed-record file
/// (<see cref="IO.FileCharacterSet"/>). Design <c>FILES_DESIGN.md</c> D29, kb/Work PB1759.
/// <para><b>The law.</b> Byte <c>0xFF</c> is the character U+FFFF — the native HIGH-VALUE
/// (<see cref="NativeCollatingSequence.HighValue"/>) — and every other byte <c>b</c> is U+00bb. A character with a byte
/// image has that byte (U+00FF and U+FFFF share <c>0xFF</c>); any other code unit has none and reads as <c>?</c> with
/// EC-DATA-CONVERSION (<see cref="StorageByte.ImageByte"/>, kb/Work PB547).</para>
/// <para><b>Why one law and not a per-leaf medium codec.</b> The collision R52 creates is between two CHARACTERS
/// (U+00FF, U+FFFF) and one BYTE (0xFF) — not between text and binary. If a byte-form leaf imaged 0xFF as U+00FF,
/// <c>MOVE HIGH-VALUES TO REC</c> followed by <c>IF REC = HIGH-VALUES</c> would turn FALSE for any record with a
/// binary field: storing the group through the leaf would CHANGE its content (measured, kb/Work PB1759 step 0),
/// where §14.9.25.4 GR4 moves a group "without conversion of data from one form of internal representation to
/// another" and nothing alters the characters afterwards. With one law the image of every byte
/// is the character a byte medium reads back, so the medium needs no knowledge of the record layout — a record
/// struct, a Tier-B REDEFINES backing, a multi-01 FD area and an elementary record cross it identically, and a
/// numeric view decodes 0xFF from U+FFFF through <see cref="ToByte"/>.</para>
/// <para><b>Accepted cost</b> (owner R52, docs/CONFORMANCE.md DOC-A.1-31): a real <c>ÿ</c> (U+00FF) that passes
/// through a byte — the record medium or a byte-form storage position — returns as HIGH-VALUE. In an alphanumeric
/// leaf in memory it stays U+00FF (the literal <c>"ÿ"</c>, <c>FUNCTION CHAR(256)</c>).</para>
/// <para><b>A hexadecimal literal is a byte too</b> (kb/Work PB2919). <c>X"hh"</c> is the character whose bit
/// configuration is hh (§8.3.3.2.4 GR4), so <c>CobolLiteral.DecodeHex</c> images each pair through
/// <see cref="ToChar"/>: <c>X"FF"</c> is U+FFFF, the HIGH-VALUE character — the same character a binary item's byte
/// 0xFF has through a REDEFINES, which is what makes <c>IF GX = X"FFFFFFFE"</c> true for the view of a binary -2.</para></summary>
public static class StorageByte
{
    /// <summary>The character that stands for <paramref name="b"/>.</summary>
    public static char ToChar(byte b) => b == 0xFF ? NativeCollatingSequence.HighValue : (char)b;

    /// <summary>The byte a character occupies — its low-order byte (U+00FF and U+FFFF are both <c>0xFF</c>), and for a
    /// character with NO byte image (<see cref="HasByte"/> false: a code unit above U+00FF other than HIGH-VALUE) the
    /// substitute byte <see cref="SubstituteByte"/> (<c>?</c>), never its truncated low-order byte: a character
    /// admitted to a DISPLAY item must not turn into an unrelated one at a byte channel (ISO/IEC 1989:2023 §8.1.2 1):
    /// every character of the alphanumeric coded character set occupies the same number of bytes; §13.18.44.4 GR1: a
    /// REDEFINES view is the same storage area; docs/CONFORMANCE.md DOC-A.1-31, -33, -46). Total and PURE, so a
    /// numeric view over any content decodes deterministically and a compile-time caller may ask it; a RUNTIME
    /// storage-image reader asks <see cref="ImageByte"/>, which also sets EC-DATA-CONVERSION.</summary>
    public static byte ToByte(char c) => HasByte(c) ? (byte)c : SubstituteByte;

    /// <summary>The byte standing for a character with no byte image: <c>?</c> (0x3F) — the disposition DOC-A.1-33
    /// decided for CONVERT's byte serialization (RV-15.19.4-2) and DOC-A.1-31 applies to every in-memory byte channel.</summary>
    private const byte SubstituteByte = (byte)'?';

    /// <summary>⛔ THE RUNTIME STORAGE-IMAGE READER — <see cref="ToByte"/> at a byte channel whose image is read AT RUN
    /// TIME (the REDEFINES / group storage image seen through a bit, national, binary, packed or floating-point view,
    /// and CONVERT's BYTE/HEX/ANY legs). A character with no byte image reads as <see cref="SubstituteByte"/> AND sets
    /// EC-DATA-CONVERSION (visible under checking, never silent): the one decided disposition (DOC-A.1-33), the same
    /// at every in-memory byte channel (DOC-A.1-46), and the file channel's own refusal is the I-O status '91'
    /// (<see cref="IO.FileCharacterSet"/>, owner decision kb/Work R47). The common case — a byte-image character — costs
    /// one comparison.</summary>
    internal static byte ImageByte(char c) => c <= 'ÿ' ? (byte)c : ImageByteBeyondLatin1(c);

    private static byte ImageByteBeyondLatin1(char c)
    {
        if (c == NativeCollatingSequence.HighValue) return 0xFF;
        Exceptions.ExceptionState.DataConversionError(
            "the code unit has no one-byte image in the 8-bit usage-DISPLAY storage image (§8.1.2 1; CONFORMANCE.md items 31, 46, 209)");
        return SubstituteByte;
    }

    /// <summary>True when <paramref name="c"/> has a byte image: U+0000–U+00FF and the HIGH-VALUE character.</summary>
    public static bool HasByte(char c) => c <= 'ÿ' || c == NativeCollatingSequence.HighValue;

    /// <summary>True when <paramref name="s"/> holds a character with no byte image (see <see cref="HasByte"/>).</summary>
    public static bool AnyWithoutByte(ReadOnlySpan<char> s) =>
        s.IndexOfAnyInRange('Ā', (char)(NativeCollatingSequence.HighValue - 1)) >= 0;

    /// <summary>The characters that stand for <paramref name="bytes"/>, one per byte.</summary>
    public static string ToChars(ReadOnlySpan<byte> bytes)
    {
        var chars = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++) chars[i] = ToChar(bytes[i]);
        return new string(chars);
    }
}
