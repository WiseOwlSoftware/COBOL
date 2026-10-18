// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using System.Runtime.CompilerServices;   // RuntimeHelpers.GetUninitializedObject — a context type's rule index
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;     // DiagnosticCatalog — the band's three codes

using Core = CobolNet.Frontend.Generated.CobolParserCore;

namespace CobolNet.Frontend.Cst;

/// <summary>
/// One CLOSED general format that ends in the <c>unrecognizedClause</c> error production — everything the
/// diagnostic needs to NAME the format the offending word violates.
/// </summary>
/// <param name="Clause">The general format's own subclause, as §-numbered in ISO/IEC 1989:2023.</param>
/// <param name="FormatLabel">"Format 1 " where the subclause prints several formats and only one carries the
/// clause list; the empty string where it prints one. Written WITH its trailing space so the message reads
/// "§13.16.2 Format 1 general format" or "§13.4.5.2 general format" with no second template.</param>
/// <param name="Subject">The construct as the standard names it, for "… is not a clause of the {Subject}".</param>
/// <param name="Noun">"clause" or "paragraph" — what the format's list is a list OF. The §12.3.2 and §11.2.1
/// formats list PARAGRAPHS, not clauses, and a diagnostic that called an unrecognized paragraph header a
/// "clause" would send the reader to the wrong subclause.</param>
/// <param name="Diagnostic">The catalogue descriptor the refusal reports. §13.16.2 keeps COBOLNET1941, allocated when
/// it was closed alone (kb/Work PB487); the clause-list formats share COBOLNET1970 and the paragraph-list formats
/// COBOLNET1971. Carried on the row, so neither refusal site re-derives the code from the noun.</param>
public sealed record ClosedFormat(string Clause, string FormatLabel, string Subject, string Noun, DiagnosticDescriptor Diagnostic)
{
    /// <summary>⛔ THE ONE WORDING OF THE REFUSAL, for both of its sites (kb/Work PB2533): <c>ClosedFormatPass</c>, which
    /// refuses a word run the error production RECOGNIZED, and <c>CobolErrorStrategy</c>, which refuses a RESERVED word
    /// written at the same position — a word the production, which begins at an IDENTIFIER, could not take.
    /// <paramref name="owner"/> is the entry's own name (<see cref="ClosedFormats.OwnerName"/>) or null.</summary>
    public string Refusal(string word, string? owner)
        => $"'{word}' is not a {Noun} of the {Subject}{(owner is { Length: > 0 } n ? $" for '{n}'" : "")} — the ISO "
           + $"§{Clause} {FormatLabel}general format lists the {Noun}s that may be specified, and it is a closed list";
}

/// <summary>
/// ⛔ THE ONE TABLE OF CLOSED GENERAL FORMATS whose residue is refused by name (kb/Work PB829), keyed by the
/// parse-tree context of the alternative list that ends in <c>unrecognizedClause</c>.
///
/// <para>WHY A TABLE AND NOT EIGHT DIAGNOSTIC SITES. The catch-all this closes (<c>genericClause :
/// IDENTIFIER (IDENTIFIER|literal)*</c>) was ONE grammar rule wired into SIX sites spanning EIGHT closed general
/// formats, and closing it one site at a time is how it survived: kb/Work PB487 closed §13.16.2 and its sibling
/// sweep, reading the grammar by rule NAME, counted four of the remaining five and missed the I-O-CONTROL
/// paragraph's INLINE alternative entirely. One production, one pass and one table make the NEXT closed format
/// automatic — add <c>| unrecognizedClause</c> to its alternative list and <c>ClosedFormatDriftTests</c> fails
/// until a row lands here, rather than the format parsing into silence.</para>
///
/// <para>⚠ EVERY ROW'S CLAUSE LIST WAS RENDERED FROM THE PRINTED PAGE before its site was closed
/// (<c>scripts/render-spec-page.py</c>; the OCR'd diagrams are systematically lossy toward falsely-restrictive
/// syntax, and closing a list against a lossy diagram REJECTS LEGAL SOURCE — strictly worse than the silence it
/// replaces). The rendered list is recorded in a comment above each site's alternative list in the .g4 files,
/// with the PDF page and printed folio.</para>
/// </summary>
public static class ClosedFormats
{
    /// <summary>Every alternative list that ends in <c>unrecognizedClause</c>, by the context type ANTLR
    /// generates for it. A <see cref="FrozenDictionary{TKey,TValue}"/> rather than a type-pattern <c>switch</c>
    /// because it must be ENUMERABLE — <c>ClosedFormatDriftTests</c> reflects over the generated parser for every
    /// context type carrying an <c>unrecognizedClause()</c> accessor and compares the two sets in BOTH
    /// directions, so neither a new closed format nor a stale row can hide.</summary>
    public static readonly FrozenDictionary<Type, ClosedFormat> ByContext =
        new Dictionary<Type, ClosedFormat>
        {
            // §13.16.2 Format 1 — the data description entry. Closed first, alone, by kb/Work PB487; PDF p393 /
            // folio 363. Keeps COBOLNET1941 and its message verbatim: the code was allocated for this format and
            // is pinned by conformance:cases/13.16-Data-description/pb487-unrecognized-data-clause.
            [typeof(Core.DataDescriptionClauseContext)] =
                new("13.16.2", "Format 1 ", "data description entry", "clause", DiagnosticCatalog.DataClauseUnrecognized),

            // §13.4.5.2 — the file description entry (FD). PDF p372-373 / folios 342-343.
            [typeof(Core.FileDescriptionClauseContext)] =
                new("13.4.5.2", "", "file description entry", "clause", DiagnosticCatalog.ClosedFormatUnrecognizedClause),

            // §13.4.6.2 — the sort-merge file description entry (SD). PDF p376 / folio 346. The SAME grammar
            // rule used to serve both this format and the FD's, which is why one catch-all left TWO formats open.
            [typeof(Core.SortMergeDescriptionClauseContext)] =
                new("13.4.6.2", "", "sort-merge file description entry", "clause", DiagnosticCatalog.ClosedFormatUnrecognizedClause),

            // §12.4.5.1 — the file control entry (SELECT). PDF p342-344 / folios 312-314.
            [typeof(Core.FileControlClausesContext)] =
                new("12.4.5.1", "", "file control entry", "clause", DiagnosticCatalog.ClosedFormatUnrecognizedClause),

            // §12.4.6.2 — the I-O-CONTROL paragraph. PDF p363 / folio 333. ⚠ The alternative kb/Work PB487's
            // sweep missed: it is INLINE, not a named `xxxClause : genericClause` wrapper.
            [typeof(Core.IoControlClauseContext)] =
                new("12.4.6.2", "", "I-O-CONTROL paragraph", "clause", DiagnosticCatalog.ClosedFormatUnrecognizedClause),

            // §12.3.7.2 — the SPECIAL-NAMES paragraph. PDF p320 / folio 290.
            [typeof(Core.SpecialNameEntryContext)] =
                new("12.3.7.2", "", "SPECIAL-NAMES paragraph", "clause", DiagnosticCatalog.ClosedFormatUnrecognizedClause),

            // §12.3.5.2 — the SOURCE-COMPUTER paragraph. PDF p314 / folio 284: `SOURCE-COMPUTER. [computer-name-1] .`
            // and nothing else (the X3.23-1985 WITH DEBUGGING MODE clause is modelled so its deleted-2002 gate can
            // name it). Closed by kb/Work PB830 — the residue was a `~DOT` token sink, not `genericClause`.
            [typeof(Core.SourceComputerParagraphContext)] =
                new("12.3.5.2", "", "SOURCE-COMPUTER paragraph", "clause", DiagnosticCatalog.ClosedFormatUnrecognizedClause),

            // §12.3.6.2 — the OBJECT-COMPUTER paragraph. PDF p315 / folio 285: the CHARACTER CLASSIFICATION and
            // PROGRAM COLLATING SEQUENCE clauses (plus the modelled '85 MEMORY SIZE and SEGMENT-LIMIT, deleted
            // 2002). Closed by kb/Work PB830, the SOURCE-COMPUTER row's twin.
            [typeof(Core.ObjectComputerClauseContext)] =
                new("12.3.6.2", "", "OBJECT-COMPUTER paragraph", "clause", DiagnosticCatalog.ClosedFormatUnrecognizedClause),

            // §12.3.2 — the configuration section. PDF p313 / folio 283. A PARAGRAPH list, not a clause list:
            // four bracketed paragraph names and nothing else.
            [typeof(Core.ConfigurationParagraphContext)] =
                new("12.3.2", "", "CONFIGURATION SECTION", "paragraph", DiagnosticCatalog.ClosedFormatUnrecognizedParagraph),

            // §11.2.1 — the identification division structure. PDF p293 / folio 263. Also a PARAGRAPH list.
            [typeof(Core.IdentificationParagraphContext)] =
                new("11.2.1", "", "IDENTIFICATION DIVISION", "paragraph", DiagnosticCatalog.ClosedFormatUnrecognizedParagraph),
        }.ToFrozenDictionary();

    /// <summary>The closed general format an <c>unrecognizedClause</c> node violates, or null if its parent
    /// carries no row — which <c>ClosedFormatDriftTests</c> makes impossible, so the null arm is a belt-and-braces
    /// guard against a diagnostic-less refusal rather than a reachable state.</summary>
    public static ClosedFormat? Of(Type? parentContextType)
        => parentContextType is not null && ByContext.TryGetValue(parentContextType, out var f) ? f : null;

    /// <summary>The same rows keyed by the generated parser's RULE INDEX of the alternative list — the key the parse
    /// layer holds when it walks the ATN rather than a finished tree (<c>CobolErrorStrategy</c>, kb/Work PB2533). Read
    /// off each context type's own <see cref="RuleContext.RuleIndex"/> override, which returns its rule's constant and
    /// touches no field, so the two keys cannot disagree and no rule name is spelled here.</summary>
    public static readonly FrozenDictionary<int, ClosedFormat> ByRuleIndex =
        ByContext.ToFrozenDictionary(e => ((RuleContext)RuntimeHelpers.GetUninitializedObject(e.Key)).RuleIndex, e => e.Value);

    /// <summary>The closed general format whose alternative list is the parser rule <paramref name="ruleIndex"/>, or null.</summary>
    public static ClosedFormat? OfRule(int ruleIndex) => ByRuleIndex.GetValueOrDefault(ruleIndex);

    /// <summary>The name of the entry <paramref name="ctx"/> sits in, for a message that points at the user's own
    /// declaration rather than at a line number alone. Four of the formats have such a name (the data description
    /// entry's data-name, the FD's and SD's file-name, the file control entry's SELECT file-name; a report group
    /// description entry's data-name as well); the rest — I-O-CONTROL, SPECIAL-NAMES, SOURCE-/OBJECT-COMPUTER, the
    /// configuration section and the identification division — are unnamed paragraphs and correctly yield null,
    /// because no ancestor of theirs matches an arm below. Read from a finished tree (<c>ClosedFormatPass</c>) and
    /// from the parser's half-built context stack alike (<c>CobolErrorStrategy</c>), where the entry's name has
    /// already been matched when a clause position is reached.
    /// <para>⚠ NEAREST ancestor wins, which is the point: a <c>dataDescriptionEntry</c> nested under an
    /// <c>fileDescriptionEntry</c> must report ITS data-name, not the file's.</para></summary>
    public static string? OwnerName(RuleContext? ctx)
    {
        for (RuleContext? p = ctx; p is not null; p = p.Parent)   // the nearest ancestor-or-self: a clause container asks for its own entry
            switch (p)
            {
                case Core.ReportGroupEntryContext r: return r.dataName()?.GetText();
                case Core.DataDescriptionEntryContext d: return d.dataName()?.GetText();
                case Core.FileDescriptionEntryContext f: return f.fileName()?.GetText();
                case Core.SortMergeDescriptionEntryContext s: return s.fileName()?.GetText();
                case Core.FileControlClauseGroupContext g: return g.fileName()?.GetText();
                case Core.EnvironmentDivisionContext: return null;   // bound: no named ancestor above
                case Core.IdentificationDivisionContext: return null;
                case Core.DataDivisionContext: return null;
            }
        return null;
    }
}
