// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>⛔ THE IMPLEMENTOR-DEFINED LINE SEQUENTIAL CHARACTER SET — ISO/IEC 1989:2023 Annex A.1 item 115
/// ("Line sequential character set", required + documented). The determination itself is published at
/// <c>docs/CONFORMANCE.md</c> <c>DOC-A.1-115</c>; this type is its ONE executable copy.
/// <para><b>THE SET:</b> every character whose code point is U+0020 (space) or above. The characters BELOW
/// U+0020 — the C0 controls U+0000–U+001F, which include the two line delimiters CR U+000D and LF U+000A and
/// the tab U+0009 — are outside it. DEL (U+007F) and everything above it are inside, so an ordinary 8-bit text
/// file reads without '09'.</para>
/// <para><b>WHY ONE TYPE:</b> the standard names this one set from three places and they must never disagree —
/// §14.9.30.4 GR16 (a successful READ whose record area holds one ⇒ I-O status '09', §9.1.13.2 item 7),
/// §14.9.51.4 GR23 (WRITE ⇒ unsuccessful, '71') and §14.9.35.4 GR17 d) (REWRITE ⇒ unsuccessful, '71';
/// §9.1.13.10 item 1 covers both write directions and adds that the record area remains unchanged). Before
/// kb/Work PB329 only the REWRITE arm existed and it carried its own ad-hoc CR/LF test, so the WRITE arm was
/// missing outright and the READ arm could not produce '09' at all.</para>
/// <para><b>WHY THIS BOUNDARY</b> (the derivation the A.1 row publishes):
/// (1) the delimiters are FORCED out — §9.1.7.2 makes a line sequential record's extent "the number of
/// characters between the preceding line delimiter and the following line delimiter", so an area holding CR or
/// LF cannot round-trip as one record;
/// (2) the set cannot be ONLY "everything but the delimiters" — the reader consumes CR/LF as the framing, so a
/// record area could never hold one and GR16's '09' would be unreachable by construction; a REQUIRED A.1
/// determination that makes its own rule vacuous is not a determination;
/// (3) the remaining C0 controls are the stream/device repertoire, not text — NUL terminates a host string,
/// SUB (0x1A) marks end-of-file on DOS-descended hosts, VT/FF are page controls, ESC introduces an escape
/// sequence — and a line sequential file exists to BE plain text and interchange with non-COBOL tools (the same
/// property that made <see cref="FixedFileAttributes"/> a sidecar rather than a header);
/// (4) ⚖ surveyed, not assumed (the owner's standing latitude rule): GnuCOBOL's <c>COB_LS_VALIDATE</c> defaults
/// to true "per COBOL 2022" and validates "that the data should be validated as it is read (status 09) /
/// written (status 71)", treating data below SPACE as invalid — the same boundary, and the same two statuses.
/// </para></summary>
public static class LineSequentialCharacterSet
{
    /// <summary>The lowest code point in the set — U+0020, the space character.</summary>
    public const int Lowest = ' ';

    /// <summary>True when <paramref name="codePoint"/> is a member of the line sequential character set.</summary>
    public static bool Contains(int codePoint) => codePoint >= Lowest;

    /// <summary>True when the record area holds at least one character OUTSIDE the set — the single predicate
    /// behind '09' (READ), '71' (WRITE) and '71' (REWRITE).
    /// <para>⛔ CHARACTERS, NOT BYTES. The connector's record channel carries one char per BYTE (Latin1), and
    /// §14.9.30.4 GR15 is explicit that a record area is "specified implicitly or explicitly" as alphanumeric
    /// OR as national. A national character occupies two bytes, UTF-16BE (§13.18.60.4 GR8 / determination D-N1),
    /// so <c>N"CD"</c> occupies the bytes <c>00 43 00 44</c> — a byte-level test would read the 0x00 halves as
    /// control characters and refuse EVERY national line sequential record (it would have broken the standing
    /// golden <c>2002/pb327_national_line_sequential_fill</c>). <paramref name="national"/> is the connector's
    /// <c>NationalRecordArea</c>, the same flag <c>FitRecord</c>/<c>TrimRecordEnd</c> read, so the three
    /// record-area rules agree on what a character is. A trailing ODD byte is half a national position, whose
    /// content §14.9.30.4 GR14/GR15 leave undefined; it forms no character and is not tested.</para></summary>
    public static bool HasCharacterOutside(ReadOnlySpan<char> recordArea, bool national)
    {
        if (!national)
        {
            foreach (char c in recordArea)
                if (!Contains(c)) return true;
            return false;
        }
        for (int i = 0; i + 1 < recordArea.Length; i += 2)
            if (!Contains((recordArea[i] << 8) | recordArea[i + 1])) return true;
        return false;
    }
}
