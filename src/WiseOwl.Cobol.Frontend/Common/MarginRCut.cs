// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>
/// WHERE A FIXED-FORM LINE'S PROGRAM TEXT WAS CUT (kb/Work PB2555; ISO §6.3.1 "Margin R is immediately to the right of the
/// rightmost character position of the program-text area", Annex A.1 item 158): the non-blank text a line carries past margin
/// R, which the reference format drops because it is not program text. The compiler is right to drop it, but a statement that
/// ran past the margin then ends early, and the parse fails at the NEXT token, or at a word the margin cut in two, with nothing
/// pointing at the truncation; the cut is the evidence a diagnostic of that syntax error cites (<c>CobolErrorListener</c>).
/// <para>Only a cut that MAY have been program text is a cut: a line whose text past margin R is blank has none, and neither does
/// a line whose program-text area ends in an inline comment (§6.3.7.3: "All characters following the floating comment
/// indicator up to margin R are comment-text", so what follows margin R is the rest of the comment) or a comment line. The
/// ordinary uses of the positions past margin R that remain are <see cref="IsSequenceNumber"/> and a file's identification
/// tags (<see cref="FitsIdentificationArea"/>).</para>
/// </summary>
/// <param name="Column">The 0-based PHYSICAL column — the character position the file holds it at (kb/Work PB2983) — of the
/// first character past the program-text area, where the program text was cut: the column an editor shows and a
/// <see cref="SourceLocation"/> names. It is NOT the position a message states (kb/Work PB4373): a tab of the line counts the
/// positions it fills (DOC-A.1-157) and one column, so on a line holding a tab the column is less than the position.</param>
/// <param name="LastProgramTextPosition">The 1-based POSITION of the last position of the program-text area, which the format
/// fixes (72: <c>FixedFormLine.MarginR</c>) wherever the file holds it, and which is how a message names the margin. A
/// position counts the spaces a tab expands to and one per character, a supplementary-plane one included
/// (<see cref="CharacterPositions"/>).</param>
/// <param name="Text">The text from the margin to the last non-space character of the line.</param>
/// <param name="HoldsProgramText">Whether the program-text area of the line holds any character that is not a space. False
/// for a line whose ONLY text lies past margin R: the reference format reads it as blank, so a statement written there
/// vanishes wholly and there is no token of the line to anchor a diagnostic to (kb/Work PB4372).</param>
public readonly record struct MarginRCut(int Column, int LastProgramTextPosition, string Text, bool HoldsProgramText)
{
    /// <summary>Positions in the COBOL-85 card-image IDENTIFICATION AREA, positions 73–80 — the area a program's name and a
    /// card's number were punched in. ISO/IEC 1989:2023 has no such area (§6.3.1 makes margin R the implementor's and says
    /// nothing of what follows it); it is the ordinary use of the positions past margin R that this stage must not mistake for
    /// a statement.</summary>
    public const int IdentificationAreaWidth = 8;

    /// <summary>How many lines of one file must carry a tag-shaped word (<see cref="FitsIdentificationArea"/>) past margin R
    /// for the positions to be read as a tag column and not as cut statements (kb/Work PB4372). A tag is a CONVENTION and
    /// repeats; a cut is an accident of one line, and a word one line alone carries is indistinguishable from the tail of a
    /// statement, so the note stays (it says "may"). Shape cannot tell a lone tag from a lone one-word tail — only
    /// recurrence can — and this is the smallest recurrence there is.</summary>
    public const int RecurrenceThatMakesATag = 2;

    /// <summary>The 1-based position of the first character that was cut.</summary>
    public int FirstCutPosition => LastProgramTextPosition + 1;

    /// <summary>The 1-based position of the last character that was cut (a position is a character, not a UTF-16 unit:
    /// <c>CharacterPositions</c>).</summary>
    public int LastCutPosition => LastProgramTextPosition + CharacterPositions.Count(Text);

    /// <summary>Whether the text begins AT margin R, with no space between it and the program-text area: a word the margin
    /// ends may then go on past it, so the cut can fall INSIDE a word.</summary>
    public bool BeginsAtMargin => Text.Length > 0 && !CobolSpace.IsBlank(Text.AsSpan(0, 1));

    /// <summary>Whether the text is a NUMBER — the sequence-number convention of those positions (a source line labelled by
    /// the number of its card), which is the ordinary use of the area past margin R and never part of a statement.</summary>
    public bool IsSequenceNumber
    {
        get
        {
            foreach (char c in Text)
                if (c is not ((>= '0' and <= '9') or ' ')) return false;
            return true;
        }
    }

    /// <summary>Whether the text fits the card-image identification area (<see cref="IdentificationAreaWidth"/>) as ONE
    /// word: the shape of an identification tag. A file in which such text recurs past margin R uses the positions that way
    /// (<see cref="RecurrenceThatMakesATag"/>, <c>ReferenceFormatMap.CutAt</c>).</summary>
    public bool FitsIdentificationArea =>
        Text.Length <= IdentificationAreaWidth && !CobolSpace.TrimSpaces(Text).AsSpan().ContainsAny(CobolSpace.Separators);
}

/// <summary>A <see cref="MarginRCut"/> together with the position, on the line it cuts, of the text that was asked about
/// (<see cref="IMarginRCutLocator.MarginRCutAt"/>).</summary>
/// <param name="At">The physical file, line and column of the text asked about (0-based line and column, as a
/// <see cref="SourceLocation"/> holds them).</param>
/// <param name="Cut">The cut of that physical line.</param>
public readonly record struct LocatedMarginRCut(SourceLocation At, MarginRCut Cut)
{
    /// <summary>Where the line's program text was cut: the file and line of <see cref="At"/> and the physical column of margin R.</summary>
    public SourceLocation CutLocation => new(At.FileName, 0, At.Line, Cut.Column);

    /// <summary>Whether the text asked about ENDS AT margin R and the text past it begins right there: its last character
    /// stands at the position just before margin R, with no space between it and what was cut, so the cut falls inside what
    /// is one word if the cut text goes on with word characters.</summary>
    public bool EndsAtMarginR => At.Column == Cut.Column - 1 && Cut.BeginsAtMargin;
}

/// <summary>The question a diagnostic asks of the reference-format stage about a position of the resultant text: was the
/// physical line that holds it cut at margin R (kb/Work PB2555)? Declared here, below both the parse and the stage that
/// knows (<c>FixedFormAreas</c>), so the syntax-error listener depends on neither.</summary>
public interface IMarginRCutLocator
{
    /// <summary>The cut of the physical line that holds the text at 0-based resultant <paramref name="column"/> of 1-based
    /// resultant <paramref name="line"/> (an ANTLR token's position), together with where that text stands in the file — null
    /// when the line was not cut at margin R. A text a continuation line joined stands on the physical line it was written
    /// on.</summary>
    LocatedMarginRCut? MarginRCutAt(int line, int column);
}
