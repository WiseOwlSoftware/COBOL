// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>⛔ ONE NUMBERED SYNTAX RULE OF THE COBOL-85 TEXT, and THE ONE PLACE a clause-rule citation chooses between the
/// 1985 designation and the 2023 one (kb/Work PB5361). A syntax rule of a file or record clause is usually the same obligation in
/// every edition, but it is NUMBERED and WORDED by each edition's own text (§1985:VII.3.7.3 SR1 is §2023:13.18.34.3 SR2), and a
/// refusal that names the 2023 rule at <c>--std 85</c> sends the reader to a rule the program was not written to. A table row
/// or clause check that cites a rule carries its <see cref="Rule1985"/> beside the 2023 citation and asks
/// <see cref="CitationAt"/>, so the next row's 1985 designation is a field to fill, not a branch to write; the drift tests
/// re-derive <see cref="Clause"/>, <see cref="Ordinal"/> and <see cref="RuleText"/> from <c>specs/ANSI_COBOL_1985.md</c>.</summary>
/// <param name="Clause">The 1985 clause, chapter then number, as <c>scripts/spec/cite.py --check</c> takes it: <c>VII.3.8.3</c>.</param>
/// <param name="Ordinal">The printed ordinal of the rule within that subclause.</param>
/// <param name="RuleText">A verbatim span of the printed rule, long enough to identify it, and absent from the 2023 text.</param>
internal readonly record struct Rule1985(string Clause, int Ordinal, string RuleText)
{
    /// <summary>The citation as a diagnostic prints it, in the form of the 1985 rows of <see cref="FileControlKeyRules"/>.</summary>
    public string Citation => $"ANSI §1985:{Clause} SR{Ordinal}";

    /// <summary>The citation at <paramref name="edition"/>: this rule's below COBOL-2002, and <paramref name="citation2023"/>
    /// (the same obligation as the 2023 text prints it, which 2002 and 2014 share) from COBOL-2002 on.</summary>
    public string CitationAt(EditionContext edition, string citation2023) =>
        edition.DialectLevel < 2002 ? Citation : citation2023;
}
