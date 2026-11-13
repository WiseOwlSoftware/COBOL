// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⛔ THE ONE PLACE A FIXED-FORM DEBUGGING LINE BECOMES SOURCE OR COMMENT (kb/Work PB1705, owner decision R56).
/// <para>The COBOL-85 debug module compiles an indicator-<c>D</c> line only when the program's SOURCE-COMPUTER paragraph
/// says <c>WITH DEBUGGING MODE</c>; otherwise it is a comment line. The reference-format stage that reads the
/// indicator runs before any clause is bound — and before the clause is even in the text it has read — so it cannot
/// decide: it CARRIES the line (<see cref="WiseOwl.Cobol.Frontend.Preprocessor.ReferenceFormatProcessor.DebugLineCarrier"/>),
/// the lexer turns the carrier into a hidden <c>DEBUG_LINE</c> marker and lexes the line's text as ordinary tokens, and
/// this pass — which sees the whole token stream, the clause included — keeps those tokens (the line is source) or moves
/// them to the <see cref="CobolLexer.ABSENT_DEBUG_LINE"/> channel the parser, the §2023:8.3.5 separator rules and every
/// other reader of the default channel never see (the line is a comment).</para>
/// <para><b>Per source unit.</b> ISO §2023:12.3.5.4 GR1: "All clauses of the SOURCE-COMPUTER paragraph apply to the source
/// unit in which they are explicitly or implicitly specified and to any source unit contained within that source
/// unit" (<c>cite.py --check 12.3.5.4</c>), so a debugging line is source when its own unit — or a unit that contains
/// it (kb/Work PB1088, the binder's <c>ConfigurationStep</c> twin of this rule) — declares the mode. A unit is the
/// run of tokens from its <c>PROGRAM-ID</c>/<c>FUNCTION-ID</c>/<c>CLASS-ID</c>/<c>INTERFACE-ID</c>/<c>METHOD-ID</c>
/// paragraph header to its <c>END PROGRAM/FUNCTION/CLASS/INTERFACE/METHOD</c> header, nesting as the source nests.
/// The clause itself is read from the tokens of the unit's SOURCE-COMPUTER paragraph, never from a debugging line
/// (a mode switch written on a debugging line would decide its own line).</para>
/// <para><b>A debugging SECTION is a comment by the same switch</b> (kb/Work PB5291). §1985:XV.2.1.3 GR2: without WITH DEBUGGING MODE "any
/// USE FOR DEBUGGING statements and all associated debugging sections are compiled as if they were comment lines", so a
/// declarative section whose first sentence is a USE FOR DEBUGGING statement leaves the default channel with the rest of the
/// unit's comment text — a body line that is not COBOL syntax, which a comment line may hold, never reaches the parser. Asked where
/// the unit's mode is decided, only where the facility exists (<c>debuggingSectionsAreComments</c>: COBOL-85, and a later edition
/// under --permissive), and when every section of a DECLARATIVES portion is one the portion's own header and END DECLARATIVES leave
/// with them, since the format admits no empty portion.</para>
/// <para>A debugging line's tokens are those after its marker on the marker's own line; a fixed continuation line
/// is joined to its line by the logical conversion (§2023:6.5 6)), so it is part of the line.</para>
/// <para><b>A line inside an open PICTURE or subscript region</b> (kb/Work PB1913) is the one shape this pass cannot
/// settle by moving tokens. The lexer is in PICMODE or SUBSCRIPT there, and which of "source" and "comment" the line's
/// text is decides whether the text moves the lexer's own mode (a <c>)</c> that closes the subscript, the PICTURE
/// string that ends PICMODE), so the region modes skip the text and leave a
/// <see cref="CobolLexer.DEBUG_LINE_IN_REGION"/> marker. <see cref="Rewrite"/> then names the FIRST such line whose unit
/// declares the mode, and the frontend blanks that line's carrier (<see cref="WithCarrierBlanked"/>) and lexes the text
/// again: the line is plain source the second time. Only the first, because every later token of the first lex came
/// from a lexer that skipped a line which is in fact source, so a decision about a LATER line read off those tokens may
/// be wrong; the tokens up to and including the first kept line are exact (an earlier region line was a comment, and a
/// comment skipped in the region is what the lexer did). A region sits in the DATA or PROCEDURE DIVISION, after the
/// unit's SOURCE-COMPUTER paragraph, which is why the clause the decision reads is on the exact side.</para>
/// </summary>
public static class DebuggingLineRewriter
{
    /// <summary>Keep or hide every debugging line of the filled token stream per its source unit's
    /// <c>WITH DEBUGGING MODE</c> clause. Byte-identical (no token touched) when the text holds no debugging line.
    /// Must run before every other rewriter and every reader of the token list, and after
    /// <see cref="CommonTokenStream.Fill"/>.
    /// <para>Returns the first debugging line INSIDE A PICTURE OR SUBSCRIPT REGION that is source — the lexer skipped
    /// its text, so the caller must blank its carrier and lex again (<see cref="WithCarrierBlanked"/>), then call this
    /// again — or <see langword="null"/> when there is none, which is when the token stream is final.</para>
    /// <para>When <paramref name="sourceLines"/> is given, the marker of every other debugging line the stream keeps as
    /// SOURCE is added to it (kb/Work PB2490): the lines the caller may still have to diagnose, because a source line is
    /// not a comment line and the logical conversion passed it over as one. The list is that of THIS stream — the final
    /// one's is complete apart from the region lines the caller blanked on the way.</para></summary>
    public static IToken? Rewrite(CommonTokenStream tokenStream, List<IToken>? sourceLines = null, List<IToken>? misplacedLines = null,
        bool debuggingSectionsAreComments = false)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();
        if (tokens is null) return null;
        var lines = DebugLines(tokens);
        if (lines.Count == 0 && !(debuggingSectionsAreComments && tokens.Any(t => t.Type == CobolLexer.DEBUGGING))) return null;

        var visible = VisibleIndices(tokens, lines);
        var units = new Stack<UnitFrame>();
        var outside = new UnitFrame(inheritedMode: false);   // debugging lines before any unit header: no clause can govern them
        var kept = new List<int>();                          // the region lines that are source (token indices)
        var misplaced = new List<int>();                     // the lines that stand where no debugging line may (token indices)
        var keptSource = new List<int>();                    // the other lines that are source (token indices)
        int nextLine = 0;
        for (int v = 0; v < visible.Count; v++)
        {
            int at = visible[v];
            // Attach every debugging line that begins before this token to the unit it sits in.
            while (nextLine < lines.Count && lines[nextLine].Start < at)
                (units.Count > 0 ? units.Peek() : outside).Lines.Add(lines[nextLine++]);

            switch (tokens[at].Type)
            {
                case CobolLexer.PROGRAM_ID or CobolLexer.FUNCTION_ID or CobolLexer.CLASS_ID
                    or CobolLexer.INTERFACE_ID or CobolLexer.METHOD_ID:
                    units.Push(units.Count > 0
                        ? new UnitFrame(units.Peek().Mode) { PermittedFrom = units.Peek().PermittedFrom }   // a contained program is in the same separately compiled program (§1985:VI.7.3): the container's OBJECT-COMPUTER already precedes it
                        : new UnitFrame(inheritedMode: false));
                    break;
                case CobolLexer.END when v + 1 < visible.Count && IsUnitKind(tokens[visible[v + 1]].Type):
                    if (units.Count > 0) Resolve(tokens, units.Pop(), kept, keptSource, misplaced);
                    break;
                case CobolLexer.DECLARATIVES when debuggingSectionsAreComments && units.Count > 0
                    && v + 1 < visible.Count && tokens[visible[v + 1]].Type == CobolLexer.DOT
                    && (v == 0 || tokens[visible[v - 1]].Type != CobolLexer.END):
                    units.Peek().DebuggingSections.AddRange(DebuggingSections(tokens, visible, v));
                    break;
                case CobolLexer.SOURCE_COMPUTER when units.Count > 0 && DeclaresMode(tokens, visible, v):
                    units.Peek().DeclaresMode = true;
                    break;
                case CobolLexer.OBJECT_COMPUTER when units.Count > 0 && units.Peek().PermittedFrom < 0:
                    units.Peek().PermittedFrom = visible[ParagraphEnd(tokens, visible, v)];
                    break;
                case CobolLexer.DATA or CobolLexer.PROCEDURE
                    when units.Count > 0 && units.Peek().PermittedFrom < 0 && v + 1 < visible.Count
                        && tokens[visible[v + 1]].Type == CobolLexer.DIVISION:
                    units.Peek().PermittedFrom = at - 1;   // no OBJECT-COMPUTER paragraph in the Environment Division: the Data Division is the first place left
                    break;
            }
        }
        while (nextLine < lines.Count) (units.Count > 0 ? units.Peek() : outside).Lines.Add(lines[nextLine++]);
        while (units.Count > 0) Resolve(tokens, units.Pop(), kept, keptSource, misplaced);
        Resolve(tokens, outside, kept, keptSource, misplaced);
        tokenStream.Seek(0);
        sourceLines?.AddRange(keptSource.Select(i => tokens[i]));
        misplacedLines?.AddRange(misplaced.Order().Select(i => tokens[i]));
        return kept.Count == 0 ? null : tokens[kept.Min()];
    }

    /// <summary>The text with the carrier of <paramref name="regionLine"/> (a <see cref="CobolLexer.DEBUG_LINE_IN_REGION"/>
    /// marker <see cref="Rewrite"/> returned) replaced by as many spaces, so the line becomes plain source at the same
    /// columns and every line, column and offset after it is unchanged. The marker's text starts with the carrier.</summary>
    public static string WithCarrierBlanked(string text, IToken regionLine)
    {
        string carrier = WiseOwl.Cobol.Frontend.Preprocessor.ReferenceFormatProcessor.DebugLineCarrier;
        int at = regionLine.StartIndex;
        if (regionLine.Type != CobolLexer.DEBUG_LINE_IN_REGION
            || string.CompareOrdinal(text, at, carrier, 0, carrier.Length) != 0)
            throw new InvalidOperationException(
                $"DebuggingLineRewriter.WithCarrierBlanked: line {regionLine.Line} is not a skipped region debugging line whose carrier stands at offset {at}");
        return string.Concat(text.AsSpan(0, at), new string(' ', carrier.Length), text.AsSpan(at + carrier.Length));
    }

    /// <summary>Hide every debugging line, as a comment: the reading of a re-lexed FRAGMENT of source text
    /// (<see cref="FragmentParse"/>), whose text is a slice of the text the main lexer finally read. A debugging line that
    /// is SOURCE had its carrier blanked before that lex (<see cref="WithCarrierBlanked"/>), so the carriers that remain
    /// in the slice are exactly the lines that are comments — the one reading both lexes of the text agree on.</summary>
    public static void HideAll(CommonTokenStream tokenStream)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();
        if (tokens is null) return;
        foreach (var line in DebugLines(tokens)) Hide(tokens, line);
        tokenStream.Seek(0);
    }

    /// <summary>The tokens a debugging line owns: its marker at <see cref="Start"/> and every following token on the
    /// marker's line, to <see cref="End"/> exclusive. A region line (<see cref="Region"/>) owns the marker alone — its
    /// text was skipped, not lexed.</summary>
    private readonly record struct Extent(int Start, int End, bool Region);

    private static List<Extent> DebugLines(IList<IToken> tokens)
    {
        var lines = new List<Extent>();
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type == CobolLexer.DEBUG_LINE_IN_REGION) { lines.Add(new Extent(i, i + 1, Region: true)); continue; }
            if (tokens[i].Type != CobolLexer.DEBUG_LINE) continue;
            int line = tokens[i].Line, end = i + 1;
            while (end < tokens.Count && tokens[end].Type != TokenConstants.EOF && tokens[end].Line == line) end++;
            lines.Add(new Extent(i, end, Region: false));
            i = end - 1;
        }
        return lines;
    }

    /// <summary>The indices of the tokens the unit and clause scan reads: default channel, outside every debugging line.</summary>
    private static List<int> VisibleIndices(IList<IToken> tokens, List<Extent> lines)
    {
        var visible = new List<int>(tokens.Count);
        int line = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            while (line < lines.Count && lines[line].End <= i) line++;
            if (line < lines.Count && i >= lines[line].Start) continue;
            if (tokens[i].Channel == TokenConstants.DefaultChannel && tokens[i].Type != TokenConstants.EOF) visible.Add(i);
        }
        return visible;
    }

    private static bool IsUnitKind(int type)
        => type is CobolLexer.PROGRAM or CobolLexer.FUNCTION or CobolLexer.CLASS or CobolLexer.INTERFACE or CobolLexer.METHOD;

    /// <summary>Whether the SOURCE-COMPUTER paragraph whose header word is <paramref name="visible"/>[<paramref name="at"/>]
    /// holds <c>DEBUGGING MODE</c> (the <c>WITH</c> is optional in the clause, and the clause follows the computer-name;
    /// kb/Work PB830). The paragraph ends at its period; the empty form
    /// <c>SOURCE-COMPUTER. OBJECT-COMPUTER. …</c> (§2023:12.3.5.3 SR1 lets the second period go) ends at the next paragraph's
    /// header word.</summary>
    private static bool DeclaresMode(IList<IToken> tokens, List<int> visible, int at)
    {
        int i = at + 1;
        if (i >= visible.Count || tokens[visible[i]].Type != CobolLexer.DOT) return false;
        for (i++; i < visible.Count; i++)
        {
            int type = tokens[visible[i]].Type;
            if (type is CobolLexer.DOT or CobolLexer.OBJECT_COMPUTER or CobolLexer.SPECIAL_NAMES or CobolLexer.REPOSITORY
                or CobolLexer.PROCEDURE or CobolLexer.DATA or CobolLexer.PROGRAM_ID or CobolLexer.END)
                return false;
            if (type == CobolLexer.DEBUGGING && i + 1 < visible.Count && tokens[visible[i + 1]].Type == CobolLexer.MODE)
                return true;
        }
        return false;
    }

    /// <summary>The index in <paramref name="visible"/> of the last token of the paragraph whose header word is
    /// <paramref name="visible"/>[<paramref name="at"/>]: its period (the paragraph's own, after the header's), or, in the empty
    /// form <c>OBJECT-COMPUTER. SPECIAL-NAMES. …</c>, the header's period (the stops <see cref="DeclaresMode"/> uses).</summary>
    private static int ParagraphEnd(IList<IToken> tokens, List<int> visible, int at)
    {
        int i = at + 1;
        if (i >= visible.Count || tokens[visible[i]].Type != CobolLexer.DOT) return at;
        for (int j = i + 1; j < visible.Count; j++)
        {
            int type = tokens[visible[j]].Type;
            if (type == CobolLexer.DOT) return j;
            if (type is CobolLexer.OBJECT_COMPUTER or CobolLexer.SPECIAL_NAMES or CobolLexer.REPOSITORY
                or CobolLexer.PROCEDURE or CobolLexer.DATA or CobolLexer.PROGRAM_ID or CobolLexer.END)
                return j - 1;
        }
        return visible.Count - 1;
    }

    /// <summary>The token extents (first and last token index, hidden-channel text between them included) of the USE FOR DEBUGGING sections of the DECLARATIVES
    /// portion whose header word is <paramref name="visible"/>[<paramref name="at"/>]: a section runs from its header (<c>name SECTION [n] .</c>;
    /// the SECTION of an <c>EXIT SECTION</c> statement is no header) to the next header or the portion's <c>END DECLARATIVES</c>, and is a debugging
    /// section when its first sentence begins <c>USE FOR DEBUGGING</c>. When no section of the portion is any other, the portion's own header and
    /// its END DECLARATIVES are returned too.</summary>
    private static List<(int First, int Last)> DebuggingSections(IList<IToken> tokens, List<int> visible, int at)
    {
        bool IsHeader(int i) => i + 1 < visible.Count && tokens[visible[i + 1]].Type == CobolLexer.SECTION && tokens[visible[i]].Type != CobolLexer.EXIT;
        bool IsEnd(int i) => i + 1 < visible.Count && tokens[visible[i]].Type == CobolLexer.END && tokens[visible[i + 1]].Type == CobolLexer.DECLARATIVES;
        var found = new List<(int First, int Last)>();
        int sections = 0, i = at + 2;
        while (i < visible.Count && !IsEnd(i))
        {
            if (!IsHeader(i)) { i++; continue; }
            sections++;
            int next = i + 2;
            while (next < visible.Count && !IsEnd(next) && !IsHeader(next)) next++;
            int period = i + 2;
            while (period < next && tokens[visible[period]].Type != CobolLexer.DOT) period++;
            if (period + 3 < next
                && tokens[visible[period + 1]].Type == CobolLexer.USE && tokens[visible[period + 2]].Type == CobolLexer.FOR
                && tokens[visible[period + 3]].Type == CobolLexer.DEBUGGING)
                found.Add((visible[i], next < visible.Count ? visible[next] - 1 : tokens.Count - 1));
            i = next;
        }
        if (found.Count > 0 && found.Count == sections && i < visible.Count)
        {
            int last = i + 1 < visible.Count && i + 2 < visible.Count && tokens[visible[i + 2]].Type == CobolLexer.DOT ? i + 2 : i + 1;
            found.Add((visible[at], visible[at + 1]));
            found.Add((visible[i], visible[last]));
        }
        return found;
    }

    private static void Resolve(IList<IToken> tokens, UnitFrame unit, List<int> keptRegionLines, List<int> keptSourceLines,
        List<int> misplacedLines)
    {
        if (!unit.Mode)
            foreach (var (first, last) in unit.DebuggingSections)
                for (int i = first; i <= last; i++)
                    if (tokens[i].Channel == TokenConstants.DefaultChannel && tokens[i].Type != TokenConstants.EOF)
                        tokens[i] = new CommonToken(tokens[i]) { Channel = CobolLexer.ABSENT_DEBUG_LINE };
        // §1985:VI.7.3: "A debugging line is only permitted in the separately compiled program after the OBJECT-COMPUTER
        // paragraph." Mode or no mode, a line that begins before that paragraph's end is misplaced; the caller diagnoses it
        // where the edition has the rule.
        foreach (var line in unit.Lines)
            if (line.Start <= unit.PermittedFrom || unit.PermittedFrom < 0) misplacedLines.Add(line.Start);
        if (unit.Mode)                                  // the unit (or a container) declared the mode: its lines are source
        {
            foreach (var line in unit.Lines)
                (line.Region ? keptRegionLines : keptSourceLines).Add(line.Start);   // a region line's text was skipped: the caller lexes it again
            return;
        }
        foreach (var line in unit.Lines) Hide(tokens, line);
    }

    /// <summary>A debugging line that is a comment: its marker and its text tokens leave for the
    /// <see cref="CobolLexer.ABSENT_DEBUG_LINE"/> channel. The MARKER moves too, so a line with no text token (one whose
    /// program-text area is blank) is still found absent in the stream: the stream is where the unit's
    /// <c>WITH DEBUGGING MODE</c> is decided, and <see cref="AbsentLines"/> reads the decision back (kb/Work PB4412).</summary>
    private static void Hide(IList<IToken> tokens, Extent line)
    {
        for (int i = line.Start; i < line.End; i++)
            if (i == line.Start || tokens[i].Channel == TokenConstants.DefaultChannel)
                tokens[i] = new CommonToken(tokens[i]) { Channel = CobolLexer.ABSENT_DEBUG_LINE };
    }

    /// <summary>The lines strictly between tokens <paramref name="after"/> and <paramref name="before"/> (indices into
    /// <paramref name="tokenStream"/> as <see cref="Rewrite"/> left it) that are debugging lines the unit does not compile:
    /// comment lines, however little text they hold (kb/Work PB4412). A text stage that omits a line marks the line map
    /// (<see cref="WiseOwl.Cobol.Frontend.Common.LineOrigin.Omitted"/>); this stage cannot, because whether a debugging line
    /// is source is decided here, on the tokens, after the line map is built. A diagnostic that must not ask of an omitted
    /// line therefore asks of both.</summary>
    public static IReadOnlySet<int> AbsentLines(ITokenStream tokenStream, int after, int before)
    {
        var lines = new HashSet<int>();
        for (int i = after + 1; i < before; i++)
            if (tokenStream.Get(i) is { Channel: CobolLexer.ABSENT_DEBUG_LINE, Type: CobolLexer.DEBUG_LINE or CobolLexer.DEBUG_LINE_IN_REGION } marker)
                lines.Add(marker.Line);
        return lines;
    }

    /// <summary>One source unit being walked: whether a container or the unit itself declared the mode (so its lines are
    /// source), and the debugging lines inside it that are not inside a unit it contains.</summary>
    private sealed class UnitFrame(bool inheritedMode)
    {
        public bool DeclaresMode { get; set; }
        /// <summary>The token index at or before which a debugging line is misplaced: where the unit's OBJECT-COMPUTER paragraph ends
        /// or, with none, where its Data or Procedure Division begins (§1985:VI.7.3); -1 while neither has been met.</summary>
        public int PermittedFrom { get; set; } = -1;
        public bool Mode => inheritedMode || DeclaresMode;
        public List<Extent> Lines { get; } = [];
        /// <summary>The USE FOR DEBUGGING sections of the unit (token extents), comment text when the unit has no mode.</summary>
        public List<(int First, int Last)> DebuggingSections { get; } = [];
    }
}
