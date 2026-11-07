// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE ONE RULE FOR "WHICH DIVISION IS THIS TEXT IN" in the §2023:6.5 logical conversion (kb/Work PB1494, PB2739, PB4241): the
/// obsolete COBOL-85 comment-entry paragraphs (AUTHOR … SECURITY, REMARKS) exist only in the IDENTIFICATION DIVISION, so
/// the converter must know whether it stands in one — otherwise a PROCEDURE DIVISION paragraph named REMARKS is read as a
/// comment-entry and its lines are lost.
/// <para>The rule is read off TEXT-WORDS of LOGICAL text, wherever they begin on a line: §2023:6.3.1 "The program-text area
/// begins in character position 8 and terminates with the character position immediately to the left of margin R" draws
/// no Area A / Area B line around a division header, and a header continued onto a second line (<c>PROCEDURE DIVI</c> /
/// <c>-SION.</c>) is one word once §2023:6.5 has joined it. A <c>DIVISION</c> word preceded by <c>IDENTIFICATION</c> or
/// <c>ID</c> enters the IDENTIFICATION DIVISION, any other <c>… DIVISION</c> leaves it, and — the header being optional
/// (§2023:11.2.1 prints <c>[ IDENTIFICATION DIVISION. ]</c>) — the first paragraph of an identification division
/// (<see cref="IdentificationParagraphNames"/>) enters it. A word merely ENDING in <c>-ID</c> (<c>CUSTOMER-ID</c>) is a
/// user's name, never a paragraph header.</para>
/// <para>A COPY or REPLACE statement is not text that stands in a division: its operands (<c>REPLACING ==PROCEDURE
/// DIVISION== BY …</c>) are words to match or to write, so the cursor passes over the whole statement, from its COPY or
/// REPLACE word to its separator period outside pseudo-text (§2023:8.3.5 3) and 6)). The library text a COPY brings in is read
/// by its own cursor (<see cref="Adopt"/>).</para>
/// <para>Three readers feed the one rule through <see cref="Feed"/>: the fixed-form and free-form converters over the logical
/// lines they have settled (<see cref="Read"/>) — source and library text alike —, the COPY driver over the text-words
/// of a block of logical text (<see cref="InIdentificationDivisionAt"/>), so the library text of a COPY starts in the
/// division the COPY statement stands in, and <see cref="CommentEntryResolver"/> over the finished text. The cursor is per
/// TEXT, not per block: the merged driver hands the COPY engine one block per directive boundary, and the cursor carries
/// across them (<see cref="Finish"/>). A division header inside a literal or a comment is never one.</para>
/// <para>⛔ THE DIVISION IS A THREE-VALUED FACT (<see cref="InIdentificationDivision"/>: yes, no, or NOT KNOWN). §2023:6.5 converts
/// "before the application of replacing and conditional compilation actions", and <c>&gt;&gt;COBOL-WORDS</c> respells
/// words after that, so a reader of the text AS WRITTEN (<paramref name="words"/> null: the converters and the COPY driver)
/// knows the division only until something it cannot see could have changed what its words mean: a conditional group whose
/// branches disagree on the division, a <c>&gt;&gt;COBOL-WORDS</c> directive that names a word the cursor reads (it may now be spelled
/// another way; <see cref="NoteCobolWordsDirective"/>), or a REPLACE statement or <c>COPY … REPLACING</c> that names a word the cursor reads (§2023:7.2.4.4 2): it
/// "specifies the text to be replaced"). What it does not know it says so, and the converter CARRIES the comment-entry candidate it cannot
/// place (<see cref="CommentEntryCarrier"/>) for <see cref="CommentEntryResolver"/> to decide on the text every
/// stage has finished with, read by a cursor that is given the group's word map (<paramref name="words"/>) and so reads
/// the words as the lexer will (docs/CONFORMANCE.md D-CE2; kb/Work PB4281).</para>
/// </summary>
internal sealed class DivisionCursor(bool? startsInIdentificationDivision, CobolWordsMap? words = null)
{
    /// <summary>The paragraphs that begin an identification division (§2023:11.2.1 General format: program-id-paragraph,
    /// function-id-paragraph, class-id-paragraph, method-id-paragraph, interface-id-paragraph):
    /// <see cref="CompilationUnitStart.UnitParagraphNames"/> and METHOD-ID. FACTORY and OBJECT begin definitions nested in
    /// a class and hold no comment-entry.</summary>
    private static readonly HashSet<string> IdentificationParagraphNames
        = new([.. CompilationUnitStart.UnitParagraphNames, "METHOD-ID"], CobolKeywords.Comparer);

    private bool _afterIdentification;   // the previous word was IDENTIFICATION or ID
    private bool _respelled;             // a >>COBOL-WORDS directive or a replacing action may have rewritten the words: none read AS WRITTEN is certain any more
    private bool _statementNamesAWordRead;   // the COPY / REPLACE statement in progress names a word this cursor reads
    private bool _inStatement;           // between a COPY / REPLACE word and its separator period
    private bool _inPseudoText;          // inside the ==…== of that statement
    private string? _text;               // the block of logical text the scan position belongs to
    private int _scanned;

    /// <summary>Whether the text read so far ends in an IDENTIFICATION DIVISION: true, false, or null when the reader
    /// cannot know (see the class remarks) — never null for a cursor given the group's word map over the finished text.</summary>
    public bool? InIdentificationDivision { get; private set; } = startsInIdentificationDivision;

    /// <summary>The conditional-compilation groups open at the scan position, innermost last; null until the first one.
    /// Only the §2023:6.5 converters ever meet a <c>&gt;&gt;IF</c> here: the merged driver consumes these directive lines
    /// before the COPY driver's blocks are cut, and has already omitted the branches not compiled.</summary>
    private List<ConditionalGroup>? _groups;

    /// <summary>One <c>&gt;&gt;IF</c> … <c>&gt;&gt;END-IF</c> or <c>&gt;&gt;EVALUATE</c> … <c>&gt;&gt;END-EVALUATE</c> as the
    /// §2023:6.5 converter can read it: BEFORE it is known which branch is compiled (§2023:6.5: text is "logically converted to
    /// free-form reference format before the application of replacing and conditional compilation actions"). Division
    /// words in the branches are evidence of the division the text is in AFTER the group only if every branch — the one
    /// no condition selects included, when there is no <c>&gt;&gt;ELSE</c> or <c>&gt;&gt;WHEN OTHER</c> — ends in the same
    /// division; otherwise which division the text stands in depends on a condition only the conditional-compilation
    /// stage evaluates, and the reader says NOT KNOWN: a comment-entry paragraph after such a group is a candidate the
    /// converter carries and <see cref="CommentEntryResolver"/> decides on the compiled text (docs/CONFORMANCE.md D-CE2;
    /// kb/Work PB4241, PB4281).</summary>
    private sealed class ConditionalGroup(bool? entry, bool branchOpen)
    {
        /// <summary>The division the group began in — where every branch starts.</summary>
        public bool? Entry { get; } = entry;

        /// <summary>Whether a branch is being read: an <c>&gt;&gt;IF</c> opens its first at once, an <c>&gt;&gt;EVALUATE</c>
        /// at its first <c>&gt;&gt;WHEN</c> (no text lies between the two, §2023:7.3.13.2).</summary>
        private bool _branchOpen = branchOpen;
        private bool _ended;      // some branch has ended …
        private bool? _end;       // … in this division
        private bool _agrees = true;
        private bool _hasDefault;

        /// <summary>The current branch ends in <paramref name="inIdentification"/>; the next one (if
        /// <paramref name="isDefault"/>, the <c>&gt;&gt;ELSE</c> or <c>&gt;&gt;WHEN OTHER</c> one) starts.</summary>
        public void NextBranch(bool? inIdentification, bool isDefault)
        {
            EndBranch(inIdentification);
            _branchOpen = true;
            _hasDefault |= isDefault;
        }

        /// <summary>The group ends after a branch that ended in <paramref name="inIdentification"/>: the division the text
        /// is in now — the one every branch ended in, else not known.</summary>
        public bool? After(bool? inIdentification)
        {
            EndBranch(inIdentification);
            if (!_hasDefault) Record(Entry);   // no branch selected: the text goes on as it was
            return _agrees ? _end : null;
        }

        private void EndBranch(bool? inIdentification)
        {
            if (_branchOpen) Record(inIdentification);
            _branchOpen = false;
        }

        private void Record(bool? inIdentification)
        {
            _agrees &= !_ended || _end == inIdentification;
            _ended = true;
            _end = inIdentification;
        }
    }

    /// <summary>A conditional-compilation directive line moves the groups (§2023:7.3.16.2 <c>&gt;&gt;IF … [ &gt;&gt;ELSE ]
    /// &gt;&gt;END-IF</c>; §2023:7.3.13.2 <c>&gt;&gt;EVALUATE … &gt;&gt;WHEN … [ &gt;&gt;WHEN OTHER ] &gt;&gt;END-EVALUATE</c>); a
    /// <c>&gt;&gt;COBOL-WORDS</c> line (§2023:7.3.10) that names a word this cursor reads may respell it, which it cannot learn as written,
    /// so from there on it knows no division; any other directive line is not text. A phrase with no group open is a
    /// structure error the conditional-compilation stage reports, and moves nothing here.</summary>
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
                    isDefault: directive.Word == "ELSE" || CompilerDirectiveLine.StartsWithWord(directive.Operand, "OTHER"));
                InIdentificationDivision = group.Entry;   // the next branch starts where the group did
                break;
            case "END-IF" or "END-EVALUATE" when group is not null:
                InIdentificationDivision = group.After(InIdentificationDivision);
                _groups!.RemoveAt(_groups.Count - 1);
                break;
            case "COBOL-WORDS":
                NoteCobolWordsDirective(directive.Operand);
                break;
        }
    }

    /// <summary>A <c>&gt;&gt;COBOL-WORDS</c> directive (§2023:7.3.10) with operand <paramref name="operand"/> is in the text. It
    /// respells words (§2023:7.3.10.4 GR2-GR4), and the cursor reads the words as written, so a directive that names a word this
    /// cursor reads — as the word to equate, undefine or substitute, or as the one to write; or by a compilation-variable-name
    /// whose value only the conditional-compilation stage knows (§2023:7.3.11.4 GR1) — leaves it knowing no division from here on,
    /// as a REPLACE statement that names one does. A directive that names none changes nothing this cursor reads, so the
    /// division stays known: <c>&gt;&gt;COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"</c> does not make a procedure division's
    /// REMARKS paragraph a candidate (kb/Work PB4281, review finding R1-1). The converters meet the directive line in the
    /// text they read; the merged driver, whose blocks end at a directive line, tells the cursor itself.</summary>
    public void NoteCobolWordsDirective(string operand)
    {
        if (words is not null || !NamesAWordRead(operand)) return;   // a reader of the finished text reads the words through the map
        _respelled = true;
        InIdentificationDivision = null;
    }

    /// <summary>Whether the operand of a <c>&gt;&gt;COBOL-WORDS</c> directive (§2023:7.3.10.2: an option keyword, then one literal,
    /// or two joined by WITH or BY) holds a literal that is a word the rule reads (<see cref="ReadsAsDivisionWord"/>), or a
    /// word that is not an option keyword — a compilation-variable-name standing in a literal slot, whose value cannot be
    /// told here. A word glued to a quotation symbol is a literal's prefix (<c>X"41"</c>), not a name.</summary>
    private static bool NamesAWordRead(string operand)
    {
        for (int i = 0; i < operand.Length; i++)
        {
            if (CobolSourceText.IsQuotationSymbol(operand[i]))
            {
                if (ReadsAsDivisionWord(CobolSourceText.LiteralContent(operand, i, out int closing))) return true;
                i = closing;   // the loop steps past the closing delimiter
            }
            else if (CobolCharacterRepertoire.IsWordCharacter(operand[i]))
            {
                int start = i;
                while (i < operand.Length && CobolCharacterRepertoire.IsWordCharacter(operand[i])) i++;
                bool prefix = i < operand.Length && CobolSourceText.IsQuotationSymbol(operand[i]);
                if (!prefix && !IsOptionWord(operand.AsSpan(start, i - start))) return true;
                i--;   // the loop steps to the next character
            }
        }
        return false;
    }

    /// <summary>A word the general format of the directive itself writes (§2023:7.3.10.2).</summary>
    private static bool IsOptionWord(ReadOnlySpan<char> word)
        => CobolKeywords.Same(word, "EQUATE") || CobolKeywords.Same(word, "WITH") || CobolKeywords.Same(word, "UNDEFINE")
           || CobolKeywords.Same(word, "SUBSTITUTE") || CobolKeywords.Same(word, "BY") || CobolKeywords.Same(word, "RESERVE");

    /// <summary>One text-word — THE rule.</summary>
    private void Feed(in TextWord word)
    {
        if (word.Kind == TextWordKind.DirectiveLine)
        {
            ReadDirective(word);   // not a text-word (§2023:7.3.4 1)), but it bounds the branches of a conditional group
            return;
        }
        if (_inStatement)
        {
            if (word.Kind == TextWordKind.PseudoTextDelimiter) _inPseudoText = !_inPseudoText;
            else if (!_inPseudoText && word.IsSeparatorPeriod)
            {
                _inStatement = false;
                _afterIdentification = false;
                // A replacing action rewrites the words of the text it governs (§2023:7.2.4.4 2): "Pseudo-text-1 specifies the text
                // to be replaced by pseudo-text-2"; §2023:7.2.4.4 1): the REPLACE statement's source text is "the
                // conditionally-processed compilation group"), and one that names a word this cursor reads — to match it, or
                // to write it — can change which division words the FINISHED text holds, which a reader of the words as
                // written cannot tell.
                if (_statementNamesAWordRead && words is null)
                {
                    _respelled = true;
                    InIdentificationDivision = null;
                }
                _statementNamesAWordRead = false;
            }
            else if (ReadsAsDivisionWord(word.Value.TrimEnd('.'))) _statementNamesAWordRead = true;
            return;
        }
        if (word.IsWord("COPY") || word.IsWord("REPLACE"))
        {
            _inStatement = true;
            _inPseudoText = false;
            _statementNamesAWordRead = false;
            return;
        }
        // The spelling as written: a separator period glued to the end of the text is not part of the word. Given the
        // group's word map the word is the one the directives make of it (§2023:7.3.10.4 GR2-GR4: a synonym is the word it
        // stands for, a de-reserved word is no keyword); without one, after a >>COBOL-WORDS directive no word can be told.
        string spelling = word.Value.TrimEnd('.');
        if (words is { IsEmpty: false }) spelling = words.Resolve(spelling) ?? "";
        if (_respelled) return;
        if (CobolKeywords.Same(spelling, "DIVISION"))
            InIdentificationDivision = _afterIdentification;
        else if (IdentificationParagraphNames.Contains(spelling))
            InIdentificationDivision = true;
        _afterIdentification = CobolKeywords.Same(spelling, "IDENTIFICATION") || CobolKeywords.Same(spelling, "ID");
    }

    /// <summary>Whether <paramref name="spelling"/> is a word the rule reads: <c>DIVISION</c>, <c>IDENTIFICATION</c>, <c>ID</c>
    /// or the paragraph that begins an identification division.</summary>
    private static bool ReadsAsDivisionWord(string spelling)
        => CobolKeywords.Same(spelling, "DIVISION") || CobolKeywords.Same(spelling, "IDENTIFICATION")
           || CobolKeywords.Same(spelling, "ID") || IdentificationParagraphNames.Contains(spelling);

    /// <summary>Read every text-word of <paramref name="text"/> — one logical line.</summary>
    public void Read(string text)
    {
        for (int next = 0; TextWordScanner.TryNext(text, ref next, out var word);)
            Feed(word);
    }

    /// <summary>Whether the logical <paramref name="text"/> is in an IDENTIFICATION DIVISION at
    /// <paramref name="position"/> (null: not known). Positions asked of one text must not decrease; a different text (the
    /// next block of the same library text) is read from its start, in the state the previous one left.</summary>
    public bool? InIdentificationDivisionAt(string text, int position)
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

    /// <summary>Take the division a spliced library text left the text in — and, when the library text held a
    /// <c>&gt;&gt;COBOL-WORDS</c> directive or a replacing action that names a word this cursor reads, the doubt it brings.</summary>
    public void Adopt(DivisionCursor libraryText)
    {
        InIdentificationDivision = libraryText.InIdentificationDivision;
        _respelled |= libraryText._respelled;
    }

    private void Begin(string text)
    {
        if (ReferenceEquals(_text, text)) return;
        _text = text;
        _scanned = 0;
    }
}
