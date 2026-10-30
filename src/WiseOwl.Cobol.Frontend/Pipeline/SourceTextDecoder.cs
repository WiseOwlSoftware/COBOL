// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace WiseOwl.Cobol.Frontend;

/// <summary>How the bytes of one source or library text file were turned into text (docs/CONFORMANCE.md §7, Annex A.1
/// items 25 and 26; kb/Work PB1397).</summary>
public enum SourceTextReading
{
    /// <summary>A byte-order mark named the encoding and the content is well-formed in it.</summary>
    Marked,

    /// <summary>No mark, and the content is well-formed UTF-8.</summary>
    Utf8,

    /// <summary>No mark, and the content is not well-formed UTF-8: every byte is the character with that code point
    /// (ISO/IEC 8859-1). The reading is reported, never silent.</summary>
    Latin1,

    /// <summary>A mark names an encoding the content is not well-formed in. No text is produced.</summary>
    Malformed,
}

/// <summary>The result of decoding one file: its text (null when <see cref="Reading"/> is
/// <see cref="SourceTextReading.Malformed"/>), the encoding that was read or refused, and the byte offset of the first
/// byte the encoding does not define.</summary>
/// <param name="Text">The decoded text without its byte-order mark; null only for a malformed marked file.</param>
/// <param name="Reading">How the file was read.</param>
/// <param name="EncodingName">The name of the encoding the mark named, or <c>UTF-8</c> / <c>ISO-8859-1</c> for an unmarked file.</param>
/// <param name="FaultOffset">The zero-based offset, within the FILE, of the first byte that is not well-formed in the
/// encoding it was read as (UTF-8 for an unmarked file read as Latin-1); -1 when the file was well-formed.</param>
public readonly record struct DecodedSourceText(string? Text, SourceTextReading Reading, string EncodingName, int FaultOffset);

/// <summary>
/// ⛔ THE ONE PLACE THE BYTES OF A SOURCE OR LIBRARY TEXT FILE BECOME CHARACTERS (docs/CONFORMANCE.md §7, Annex A.1
/// items 25 and 26; ISO §8.1.3.2 GR1 and GR2; kb/Work PB1397). The disposition, stated once:
/// <list type="bullet">
/// <item>A file beginning with a byte-order mark is read in the encoding the mark names — UTF-8 (<c>EF BB BF</c>),
/// UTF-16 little- or big-endian (<c>FF FE</c> / <c>FE FF</c>), UTF-32 little- or big-endian (<c>FF FE 00 00</c> /
/// <c>00 00 FE FF</c>) — and the mark is not source text.</item>
/// <item>A marked file whose content is not well-formed in that encoding (invalid UTF-8, a lone surrogate or a truncated
/// code unit in UTF-16, a UTF-32 unit that is not a Unicode scalar value) is <see cref="SourceTextReading.Malformed"/>:
/// the mark states the encoding, so there is no fallback.</item>
/// <item>A file with no mark is read as UTF-8, or, when it is not well-formed UTF-8, as ISO/IEC 8859-1
/// (<see cref="SourceTextReading.Latin1"/>), which keeps every byte as the character with that code point.</item>
/// </list>
/// No byte is ever replaced by U+FFFD: the strict decoders throw where <c>File.ReadAllText</c> substitutes.
/// </summary>
public static class SourceTextDecoder
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly UnicodeEncoding StrictUtf16Le = new(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
    private static readonly UnicodeEncoding StrictUtf16Be = new(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);
    private static readonly UTF32Encoding StrictUtf32Le = new(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true);
    private static readonly UTF32Encoding StrictUtf32Be = new(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true);

    /// <summary>Decode the bytes of one file.</summary>
    public static DecodedSourceText Decode(ReadOnlySpan<byte> file)
    {
        // The StreamReader detection order: UTF-32LE's mark begins with UTF-16LE's, so the longer mark is tried first.
        (Encoding Encoding, string Name, int MarkLength)? mark = file switch
        {
            [0xEF, 0xBB, 0xBF, ..] => (StrictUtf8, "UTF-8", 3),
            [0xFF, 0xFE, 0x00, 0x00, ..] => (StrictUtf32Le, "UTF-32LE", 4),
            [0xFF, 0xFE, ..] => (StrictUtf16Le, "UTF-16LE", 2),
            [0xFE, 0xFF, ..] => (StrictUtf16Be, "UTF-16BE", 2),
            [0x00, 0x00, 0xFE, 0xFF, ..] => (StrictUtf32Be, "UTF-32BE", 4),
            _ => null,
        };

        if (mark is var (encoding, name, markLength))
        {
            try
            {
                return new DecodedSourceText(encoding.GetString(file[markLength..]), SourceTextReading.Marked, name, -1);
            }
            catch (DecoderFallbackException fault)
            {
                // .NET reports a lone UTF-16 high surrogate one unit late (at the unit that failed to complete it), so the
                // exact byte is found by walking the units; the UTF-8 and UTF-32 indexes are exact.
                int at = encoding is UnicodeEncoding
                    ? FirstIllFormedUtf16Byte(file[markLength..], bigEndian: ReferenceEquals(encoding, StrictUtf16Be))
                    : fault.Index;
                return new DecodedSourceText(null, SourceTextReading.Malformed, name, markLength + at);
            }
        }

        try
        {
            return new DecodedSourceText(StrictUtf8.GetString(file), SourceTextReading.Utf8, "UTF-8", -1);
        }
        catch (DecoderFallbackException fault)
        {
            return new DecodedSourceText(Encoding.Latin1.GetString(file), SourceTextReading.Latin1, "ISO-8859-1", fault.Index);
        }
    }

    /// <summary>The offset of the first byte of the first ill-formed UTF-16 sequence: a high surrogate not followed by a
    /// low one, a low surrogate on its own, or a trailing odd byte.</summary>
    private static int FirstIllFormedUtf16Byte(ReadOnlySpan<byte> bytes, bool bigEndian)
    {
        for (int i = 0; i + 1 < bytes.Length; i += 2)
        {
            int unit = Unit(bytes, i, bigEndian);
            if (unit is >= 0xD800 and <= 0xDBFF)
            {
                if (i + 3 >= bytes.Length || Unit(bytes, i + 2, bigEndian) is not (>= 0xDC00 and <= 0xDFFF)) return i;
                i += 2;
            }
            else if (unit is >= 0xDC00 and <= 0xDFFF) return i;
        }

        return bytes.Length - 1;
    }

    private static int Unit(ReadOnlySpan<byte> bytes, int at, bool bigEndian) =>
        bigEndian ? (bytes[at] << 8) | bytes[at + 1] : (bytes[at + 1] << 8) | bytes[at];
}
