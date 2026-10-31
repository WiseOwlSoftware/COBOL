// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE ONE RULE FOR "WHICH DIVISION IS THIS TEXT IN" in the §6.5 logical conversion (kb/Work PB1494, PB2739, PB4241): the
/// obsolete COBOL-85 comment-entry paragraphs (AUTHOR … SECURITY, REMARKS) exist only in the IDENTIFICATION DIVISION, so
/// the converter must know whether it stands in one — otherwise a PROCEDURE DIVISION paragraph named REMARKS is read as a
/// comment-entry and its lines are lost.
/// <para>The rule is read off TEXT-WORDS of LOGICAL text, wherever they begin on a line: §6.3.1 "The program-text area
/// begins in character position 8 and terminates with the character position immediately to the left of margin R" draws
/// no Area A / Area B line around a division header, and a header continued onto a second line (<c>PROCEDURE DIVI</c> /
/// <c>-SION.</c>) is one word once §6.5 has joined it. A <c>DIVISION</c> word preceded by <c>IDENTIFICATION</c> or
/// <c>ID</c> enters the IDENTIFICATION DIVISION, any other <c>… DIVISION</c> leaves it, and — the header being optional
/// (§11.2.1 prints <c>[ IDENTIFICATION DIVISION. ]</c>) — the first paragraph of an identification division
/// (<see cref="IdentificationParagraphNames"/>) enters it. A word merely ENDING in <c>-ID</c> (<c>CUSTOMER-ID</c>) is a
/// user's name, never a paragraph header.</para>
/// <para>A COPY or REPLACE statement is not text that stands in a division: its operands (<c>REPLACING ==PROCEDURE
/// DIVISION== BY …</c>) are words to match or to write, so the cursor passes over the whole statement, from its COPY or
/// REPLACE word to its separator period outside pseudo-text (§8.3.5 3) and 6)). The library text a COPY brings in is read
/// by its own cursor (<see cref="Adopt"/>).</para>
/// <para>Two readers feed the one rule through <see cref="Feed"/>: the fixed-form and free-form converters over the logical
/// lines they have settled (<see cref="Read"/>) — source and library text alike — and the COPY driver over the text-words
/// of a block of logical text (<see cref="InIdentificationDivisionAt"/>), so the library text of a COPY starts in the
/// division the COPY statement stands in. The cursor is per TEXT, not per block: the merged driver hands the COPY engine
/// one block per directive boundary, and the cursor carries across them (<see cref="Finish"/>). A division header inside
/// a literal or a comment is never one.</para>
/// </summary>
internal sealed class DivisionCursor(bool startsInIdentificationDivision)
{
    /// <summary>The paragraphs that begin an identification division (§11.2.1 General format: program-id-paragraph,
    /// function-id-paragraph, class-id-paragraph, method-id-paragraph, interface-id-paragraph):
    /// <see cref="CompilationUnitStart.UnitParagraphNames"/> and METHOD-ID. FACTORY and OBJECT begin definitions nested in
    /// a class and hold no comment-entry.</summary>
    private static readonly HashSet<string> IdentificationParagraphNames
        = new([.. CompilationUnitStart.UnitParagraphNames, "METHOD-ID"], CobolNames.Comparer);

    private bool _afterIdentification;   // the previous word was IDENTIFICATION or ID
    private bool _inStatement;           // between a COPY / REPLACE word and its separator period
    private bool _inPseudoText;          // inside the ==…== of that statement
    private string? _text;               // the block of logical text the scan position belongs to
    private int _scanned;

    /// <summary>Whether the text read so far ends in an IDENTIFICATION DIVISION.</summary>
    public bool InIdentificationDivision { get; private set; } = startsInIdentificationDivision;

    /// <summary>The conditional-compilation groups open at the scan position, innermost last; null until the first one.
    /// Only the §6.5 converters ever meet a <c>&gt;&gt;IF</c> here: the merged driver consumes these directive lines
    /// before the COPY driver's blocks are cut, and has already omitted the branches not compiled.</summary>
    private List<ConditionalGroup>? _groups;

    /// <summary>One <c>&gt;&gt;IF</c> … <c>&gt;&gt;END-IF</c> or <c>&gt;&gt;EVALUATE</c> … <c>&gt;&gt;END-EVALUATE</c> as the
    /// §6.5 converter can read it: BEFORE it is known which branch is compiled (§6.5: text is "logically converted to
    /// free-form reference format before the application of replacing and conditional compilation actions"). Division
    /// words in the branches are evidence of the division the text is in AFTER the group only if every branch — the one
    /// no condition selects included, when there is no <c>&gt;&gt;ELSE</c> or <c>&gt;&gt;WHEN OTHER</c> — ends in the same
    /// division; otherwise which division the text stands in depends on a condition only the conditional-compilation
    /// stage evaluates, and the converter keeps the division it was in before the group (kb/Work PB4241).</summary>
    private sealed class ConditionalGroup(bool entry, bool branchOpen)
    {
        /// <summary>The division the group began in — where every branch starts.</summary>
        public bool Entry { get; } = entry;

        /// <summary>Whether a branch is being read: an <c>&gt;&gt;IF</c> opens its first at once, an <c>&gt;&gt;EVALUATE</c>
        /// at its first <c>&gt;&gt;WHEN</c> (no text lies between the two, §7.3.13.2).</summary>
        private bool _branchOpen = branchOpen;
        private bool _ended;      // some branch has ended …
        private bool _end;        // … in this division
        private bool _agrees = true;
        private bool _hasDefault;

        /// <summary>The current branch ends in <paramref name="inIdentification"/>; the next one (if
        /// <paramref name="isDefault"/>, the <c>&gt;&gt;ELSE</c> or <c>&gt;&gt;WHEN OTHER</c> one) starts.</summary>
        public void NextBranch(bool inIdentification, bool isDefault)
        {
            EndBranch(inIdentification);
            _branchOpen = true;
            _hasDefault |= isDefault;
        }

        /// <summary>The group ends after a branch that ended in <paramref name="inIdentification"/>: the division the text
        /// is in now — the one every branch agreed on, else the one before the group.</summary>
        public bool After(bool inIdentification)
        {
            EndBranch(inIdentification);
            if (!_hasDefault) Record(Entry);   // no branch selected: the text goes on as it was
            return _agrees ? _end : Entry;
        }

        private void EndBranch(bool inIdentification)
        {
            if (_branchOpen) Record(inIdentification);
            _branchOpen = false;
        }

        private void Record(bool inIdentification)
        {
            _agrees &= !_ended || _end == inIdentification;
            _ended = true;
            _end = inIdentification;
        }
    }

    /// <summary>A conditional-compilation directive line moves the groups (§7.3.16.2 <c>&gt;&gt;IF … [ &gt;&gt;ELSE ]
    /// &gt;&gt;END-IF</c>; §7.3.13.2 <c>&gt;&gt;EVALUATE … &gt;&gt;WHEN … [ &gt;&gt;WHEN OTHER ] &gt;&gt;END-EVALUATE</c>);
    /// any other directive line is not text. A phrase with no group open is a structure error the conditional-compilation
    /// stage reports, and moves nothing here.</summary>
    private void ReadDirective(in TextWord word)
    {
        _afterIdentification = false;
        if (!CompilerDirectiveLine.TryParse(word.Value, out var directive)) return;
        var group = _groups is { Count: > 0 } ? _groups[^1] : null;
        switch (directive.Word)
        {
            case "IF":
                (_groups ??= []).Add(new ConditionalGroup(InIdentificationDivision, branchOpen: true));
                break;
            case "EVALUATE":
                (_groups ??= []).Add(new ConditionalGroup(InIdentificationDivision, branchOpen: false));
                break;
            case "ELSE" or "WHEN" when group is not null:
                group.NextBranch(InIdentificationDivision,
                    isDefault: directive.Word == "ELSE" || CobolNames.StartsWith(directive.Operand, "OTHER"));
                InIdentificationDivision = group.Entry;   // the next branch starts where the group did
                break;
            case "END-IF" or "END-EVALUATE" when group is not null:
                InIdentificationDivision = group.After(InIdentificationDivision);
                _groups!.RemoveAt(_groups.Count - 1);
                break;
        }
    }

    /// <summary>One text-word — THE rule.</summary>
    private void Feed(in TextWord word)
    {
        if (word.Kind == TextWordKind.DirectiveLine)
        {
            ReadDirective(word);   // not a text-word (§7.3.4 1)), but it bounds the branches of a conditional group
            return;
        }
        if (_inStatement)
        {
            if (word.Kind == TextWordKind.PseudoTextDelimiter) _inPseudoText = !_inPseudoText;
            else if (!_inPseudoText && word.IsSeparatorPeriod)
            {
                _inStatement = false;
                _afterIdentification = false;
            }
            return;
        }
        if (word.IsWord("COPY") || word.IsWord("REPLACE"))
        {
            _inStatement = true;
            _inPseudoText = false;
            return;
        }
        // The spelling as written: a separator period glued to the end of the text is not part of the word.
        string spelling = word.Value.TrimEnd('.');
        if (CobolNames.Same(spelling, "DIVISION"))
            InIdentificationDivision = _afterIdentification;
        else if (IdentificationParagraphNames.Contains(spelling))
            InIdentificationDivision = true;
        _afterIdentification = CobolNames.Same(spelling, "IDENTIFICATION") || CobolNames.Same(spelling, "ID");
    }

    /// <summary>Read every text-word of <paramref name="text"/> — one logical line.</summary>
    public void Read(string text)
    {
        for (int next = 0; TextWordScanner.TryNext(text, ref next, out var word);)
            Feed(word);
    }

    /// <summary>Whether the logical <paramref name="text"/> is in an IDENTIFICATION DIVISION at
    /// <paramref name="position"/>. Positions asked of one text must not decrease; a different text (the next block of
    /// the same library text) is read from its start, in the state the previous one left.</summary>
    public bool InIdentificationDivisionAt(string text, int position)
    {
        Begin(text);
        while (true)
        {
            int next = _scanned;   // a word that reaches past the position is left for a later, greater one
            if (!TextWordScanner.TryNext(text, ref next, out var word) || word.End > position) break;
            _scanned = next;
            Feed(word);
        }
        return InIdentificationDivision;
    }

    /// <summary>Read the rest of <paramref name="text"/> — the block is done and the next one continues from here.</summary>
    public void Finish(string text) => InIdentificationDivisionAt(text, text.Length);

    /// <summary>Take the division a spliced library text left the text in.</summary>
    public void Adopt(DivisionCursor libraryText) => InIdentificationDivision = libraryText.InIdentificationDivision;

    private void Begin(string text)
    {
        if (ReferenceEquals(_text, text)) return;
        _text = text;
        _scanned = 0;
    }
}
