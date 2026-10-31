// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>⛔ THE ONE READER OF A <c>procedureName</c> PARSE NODE'S WORDS (kb/Work PB4238). The grammar's
/// <c>procedureName</c> is <c>word ((OF | IN) word)?</c> and is shared by the two header slots
/// (<c>paragraphName</c>, <c>sectionName</c>) and every reference (PERFORM, GO TO, ALTER, RESUME AT, the SORT / MERGE
/// procedures). A consumer that asked the node for <c>GetText()</c> read the children GLUED —
/// <c>P1 OF S1</c> as <c>P1OFS1</c> — so a qualified header declared a procedure no source element wrote and a
/// <c>PERFORM P1OFS1</c> ran it. The head word and the qualifier are read here, from the children, and nowhere
/// else; <see cref="ProcedureTableBuilder.HeaderName"/> refuses a qualified header (ISO §14.2.1: the formats write
/// <c>section-name-1 SECTION.</c> and <c>paragraph-name-1.</c>, §14.4.1: only a procedure-name REFERENCE
/// "consists of a paragraph-name (that may be qualified)").</summary>
internal static class ProcedureNameShape
{
    /// <summary>The procedure-name's own word — the paragraph-name or section-name, never the qualifier.</summary>
    public static string Head(Core.ProcedureNameContext pn) => pn.GetChild(0).GetText();

    /// <summary>Whether the node is written <c>word (OF | IN) word</c>.</summary>
    public static bool IsQualified(Core.ProcedureNameContext pn) => pn.ChildCount >= 3;

    /// <summary>The <c>IN / OF section-name</c> qualifier's word, or null for an unqualified name.</summary>
    public static string? Qualifier(Core.ProcedureNameContext pn) => IsQualified(pn) ? pn.GetChild(2).GetText() : null;

    /// <summary>The qualifier as written, <c> OF S1</c> (leading space), or "" — for a message quoting the name.</summary>
    public static string QualifierAsWritten(Core.ProcedureNameContext pn) =>
        IsQualified(pn) ? " " + pn.GetChild(1).GetText() + " " + pn.GetChild(2).GetText() : "";
}
