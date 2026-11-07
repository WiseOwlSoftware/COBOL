// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// HOW A CANDIDATE COBOL-85 COMMENT-ENTRY LINE TRAVELS from the §2023:6.5 logical conversion to the stage that decides it
/// (<see cref="CommentEntryResolver"/>; docs/CONFORMANCE.md D-CE2; kb/Work PB4281).
/// <para>Whether a line such as <c>AUTHOR. Pat O'Brien.</c> is a comment-entry paragraph or the program's own paragraph
/// depends on the division the TEXT stands in, and "the text" is what the stages between the conversion and the parse
/// leave: §2023:6.5 converts "before the application of replacing and conditional compilation actions", so the conversion has
/// not seen which branch of a <c>&gt;&gt;IF</c> is compiled, what a <c>COPY … REPLACING</c> or a <c>REPLACE</c> wrote, or
/// the synonyms a <c>&gt;&gt;COBOL-WORDS</c> directive gave <c>DIVISION</c>. The conversion therefore asks the one question
/// it CAN answer — does the line begin, in the Area-A sense of COBOL-85, with a comment-entry paragraph's word? — and
/// CARRIES the candidate: each physical line of the entry becomes one line of the logical text that every later stage reads
/// as a COMMENT (the <c>*&gt;</c> floating comment indicator and a Unicode noncharacter, so no comment a programmer writes
/// is one), holding the physical line itself. A comment-entry is arbitrary text (an apostrophe in a name, an unclosed
/// quotation mark), so it must stay out of every scanner until the decision is made.</para>
/// <para>The carrier is the same device as <see cref="ReferenceFormatProcessor.DebugLineCarrier"/> (a fixed-form
/// debugging line, whose reading the unit's WITH DEBUGGING MODE decides further down the text): a line the conversion
/// cannot yet classify, kept as an inert slot of the logical text. Layout after <see cref="Prefix"/>:
/// <c>H|B</c> (the header line of the paragraph, or a line of its entry), <c>F|R</c> (fixed or free reference format),
/// <c>1|0</c> (the NIST CCVS indicator dialect), the inclusion of the text the line was read in
/// (<see cref="CommentEntryPin.Inclusion"/>), <c>:</c>, the file's own line number, <c>;</c>, the column map of a line that
/// held a tab or a supplementary-plane character (comma-separated, else empty), <c>;</c>, and the line's text as the
/// line-entry stage settled it.</para>
/// </summary>
internal static class CommentEntryCarrier
{
    /// <summary>The start of every carrier line. U+FDD1 is a Unicode NONCHARACTER, reserved for process-internal use and
    /// never in interchanged text (as <see cref="ReferenceFormatProcessor.DebugLineCarrier"/>'s U+FDD0 is).</summary>
    public static readonly string Prefix = CobolSourceText.CommentIndicator + (char)0xFDD1 + "ENTRY ";

    /// <summary>One carried physical line, read back.</summary>
    /// <param name="IsHeader">The line that opens the paragraph (its first word is a comment-entry paragraph's); otherwise
    /// a line of the entry.</param>
    /// <param name="IsFixed">The reference format the line was read in.</param>
    /// <param name="CcvsIndicators">Whether the NIST CCVS column-7 conventions were honored reading it.</param>
    /// <param name="Inclusion">The inclusion of the text the line was read in (<see cref="CommentEntryPin.Inclusion"/>).</param>
    /// <param name="Line">The physical line, as the line-entry stage settled it.</param>
    public readonly record struct Carried(bool IsHeader, bool IsFixed, bool CcvsIndicators, int Inclusion, PhysicalLine Line);

    /// <summary>Whether <paramref name="logicalLine"/> is a carrier.</summary>
    public static bool Is(string logicalLine) => logicalLine.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>The carrier of <paramref name="line"/>, read in inclusion <paramref name="inclusion"/> of its text.</summary>
    public static string Encode(bool isHeader, bool isFixed, bool ccvsIndicators, int inclusion, in PhysicalLine line)
    {
        var text = new StringBuilder(Prefix)
            .Append(isHeader ? 'H' : 'B').Append(isFixed ? 'F' : 'R').Append(ccvsIndicators ? '1' : '0')
            .Append(inclusion.ToString(CultureInfo.InvariantCulture)).Append(':')
            .Append(line.Number.ToString(CultureInfo.InvariantCulture)).Append(';');
        if (line.Columns is { } columns) text.Append(string.Join(',', columns.ToArray()));
        return text.Append(';').Append(line.Text).ToString();
    }

    /// <summary>The physical line a carrier holds. Throws on a line that is not a well-formed carrier: a stage that
    /// damaged one has broken the conversion's contract, and guessing would compile the wrong text.</summary>
    public static Carried Decode(string logicalLine)
    {
        if (!Is(logicalLine)) throw Damaged(logicalLine);
        var body = logicalLine.AsSpan(Prefix.Length);
        int inclusionEnd = body.IndexOf(':');
        int numberEnd = body.IndexOf(';');
        if (body.Length < 4 || inclusionEnd < 3 || numberEnd <= inclusionEnd
            || body[0] is not ('H' or 'B') || body[1] is not ('F' or 'R') || body[2] is not ('1' or '0')
            || !int.TryParse(body[3..inclusionEnd], NumberStyles.None, CultureInfo.InvariantCulture, out int inclusion)
            || !int.TryParse(body[(inclusionEnd + 1)..numberEnd], NumberStyles.None, CultureInfo.InvariantCulture, out int number))
            throw Damaged(logicalLine);
        var rest = body[(numberEnd + 1)..];
        int columnsEnd = rest.IndexOf(';');
        if (columnsEnd < 0) throw Damaged(logicalLine);
        ColumnMap? columns = null;
        if (columnsEnd > 0)
        {
            var positions = new List<int>();
            foreach (var range in rest[..columnsEnd].Split(','))
            {
                if (!int.TryParse(rest[..columnsEnd][range], NumberStyles.None, CultureInfo.InvariantCulture, out int position))
                    throw Damaged(logicalLine);
                positions.Add(position);
            }
            columns = new ColumnMap([.. positions]);
        }
        return new Carried(body[0] == 'H', body[1] == 'F', body[2] == '1', inclusion,
            new PhysicalLine(number, rest[(columnsEnd + 1)..].ToString(), columns));
    }

    private static InvalidOperationException Damaged(string logicalLine)
        => new($"a comment-entry carrier was damaged between the logical conversion and the comment-entry decision (docs/CONFORMANCE.md D-CE2): '{logicalLine}'");
}
