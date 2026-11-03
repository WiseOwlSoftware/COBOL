// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE COMMENT-ENTRY DECISION FOR THE CANDIDATES THE LOGICAL CONVERSION COULD NOT PLACE (docs/CONFORMANCE.md D-CE2;
/// kb/Work PB4281). §6.5: "Source text and library text in fixed-form reference format are logically converted to free-form
/// reference format before the application of replacing and conditional compilation actions" (cite-checked). The
/// conversion decides whether a line opens a COBOL-85 comment-entry paragraph from the division the text stands in, and
/// where that division depends on a stage that runs AFTER it — a <c>&gt;&gt;IF</c> group whose branches disagree on the
/// division, a <c>&gt;&gt;COBOL-WORDS</c> directive that respells <c>DIVISION</c> — it does not guess: it CARRIES the
/// candidate (<see cref="CommentEntryCarrier"/>), an inert slot of the logical text, and this stage decides it.
/// <para><b>It runs where the answer is exact: on the text every stage has finished with, after conditional compilation
/// (the omitted branches are gone and the selected one is all that is left), COPY and REPLACE (library text is in place,
/// replacing has been applied) and the <c>&gt;&gt;COBOL-WORDS</c> stage, whose word map it reads the words through
/// (<see cref="DivisionCursor"/>).</b> That is the order GnuCOBOL, the implementation CLAUDE.md rule 1 names where the
/// standard is silent, works in: it preprocesses (COPY, REPLACE, <c>&gt;&gt;IF</c>) and only then scans the identification
/// division's paragraphs, and 2023 has no comment-entry at all. Frontend.Preprocess places it, and
/// <c>CommentEntryResolutionOrderDriftTests</c> keeps it there.</para>
/// <para>A candidate that proves to be PROGRAM TEXT was hidden from exactly the stages that should have read it (its COPY and
/// REPLACE statements, the replacing over its words, its directives), so the resolver does not leave it converted late: it
/// NAMES the EARLIEST such candidate of the text (<see cref="Resolution.ProgramText"/>) and <c>Frontend</c> runs the text
/// stages again with that candidate read as the program text it is (<see cref="CommentEntryPins"/>), to a fixed point. Only
/// the earliest is named because only it was decided on a text no pin still to come can change: the candidates after it
/// were decided with it hidden, and a COPY statement in its paragraph may bring in the IDENTIFICATION DIVISION a later one
/// stands in (kb/Work PB4487). What the resolver returns for a run that is about to be repeated is written the way the
/// conversion would have written it only so that the cursor reads on through the candidates.</para>
/// <para>A text with no carrier passes through untouched, at the cost of one search.</para>
/// </summary>
internal static class CommentEntryResolver
{
    /// <summary>The CARRIED candidate in progress: its slots in the text, and the lines carried.</summary>
    private sealed class Candidate(bool inIdentificationDivision)
    {
        public bool InIdentificationDivision { get; } = inIdentificationDivision;
        public List<int> Slots { get; } = [];
        public List<CommentEntryCarrier.Carried> Lines { get; } = [];
    }

    /// <summary>The decision: the text, and the earliest candidate that proved to be program text.</summary>
    /// <param name="Text">The text with every carried candidate written as the conversion would have written it, had it known
    /// the division.</param>
    /// <param name="ProgramText">The line, in its inclusion, that opens the EARLIEST candidate of the text that is program text;
    /// null when none is — the fixed point <c>Frontend</c> iterates to.</param>
    public readonly record struct Resolution(MappedText Text, CommentEntryPin? ProgramText);

    /// <summary>Decide every carried candidate of <paramref name="text"/> and write it as the conversion would have, had it
    /// known the division: a comment-entry paragraph in an IDENTIFICATION DIVISION (the header, then its entry discarded),
    /// program text elsewhere (converted so that the division is read on, and the earliest named in
    /// <see cref="Resolution.ProgramText"/>). The line count and every other line are untouched.</summary>
    /// <param name="words">The compilation group's <c>&gt;&gt;COBOL-WORDS</c> map (<see cref="CobolWordsMap.Empty"/> when
    /// there is none), through which the division words of the finished text are read.</param>
    /// <param name="gates">The compilation's reference-format diagnostics, which a candidate that is program text reports
    /// through as every ordinary line does.</param>
    public static Resolution Resolve(MappedText text, CobolWordsMap words, ReferenceFormatDiagnostics? gates)
    {
        if (!text.Text.Contains(CommentEntryCarrier.Prefix, StringComparison.Ordinal)) return new Resolution(text, null);
        string[] lines = text.Text.Split('\n');
        var origins = (LineOrigin[])text.Lines.Clone();
        var cursor = new DivisionCursor(startsInIdentificationDivision: true, words);   // a source text begins in an IDENTIFICATION DIVISION
        Candidate? candidate = null;
        CommentEntryPin? programText = null;   // the earliest candidate that is program text

        void Finish()
        {
            if (candidate is null) return;
            string file = origins[candidate.Slots[0]].Origin.File;
            var (resolved, resolvedOrigins) = ReferenceFormatProcessor.ConvertCommentEntryCandidate(
                candidate.Lines, file, candidate.InIdentificationDivision, gates);
            // A converter pins the line that OPENS a paragraph, so only a candidate its header opens can be named.
            if (!candidate.InIdentificationDivision && candidate.Lines[0].IsHeader)
                programText ??= new CommentEntryPin(file, candidate.Lines[0].Inclusion, candidate.Lines[0].Line.Number);
            for (int k = 0; k < candidate.Slots.Count; k++)
            {
                int slot = candidate.Slots[k];
                lines[slot] = k < resolved.Count ? resolved[k] : "";   // a joined continuation line leaves its slot empty
                if (k < resolved.Count) origins[slot] = resolvedOrigins[k];
            }
            // What is program text is text the division is read from; a comment-entry is commentary.
            if (!candidate.InIdentificationDivision)
                foreach (string line in resolved) ReadLine(line);
            candidate = null;
        }

        void ReadLine(string line)
        {
            if (!Inert(line)) cursor.Read(line);
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (CommentEntryCarrier.Is(line))
            {
                var carried = CommentEntryCarrier.Decode(line);
                // A header opens a candidate; so does a line of an entry whose header is gone (no stage drops one today).
                if (carried.IsHeader || candidate is null)
                {
                    Finish();
                    candidate = new Candidate(cursor.InIdentificationDivision is true);
                }
                candidate.Slots.Add(i);
                candidate.Lines.Add(carried);
                continue;
            }
            if (!Inert(line)) Finish();   // program text ends the candidate; a blank or debugging line does not
            ReadLine(line);
        }
        Finish();
        return new Resolution(new MappedText(string.Join('\n', lines), origins),
            programText);
    }

    /// <summary>A line the division is not read from and that does not end a candidate: blank, or a fixed-form debugging
    /// line (source or commentary as the unit's WITH DEBUGGING MODE says, known only after the parse).</summary>
    private static bool Inert(string line)
        => ReferenceFormatProcessor.HoldsNoProgramText(line) || line.StartsWith(ReferenceFormatProcessor.DebugLineCarrier, StringComparison.Ordinal);
}
