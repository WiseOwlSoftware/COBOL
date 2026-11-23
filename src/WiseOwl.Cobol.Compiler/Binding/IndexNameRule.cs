// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE RULE THAT AN INDEX-NAME IS NOT AN IDENTIFIER, IN THE EDITION IN FORCE (kb/Work PB4950): every COBOL1637 message
/// (an index-name in an identifier or arithmetic position) cites <see cref="NotAnIdentifier"/> and <see cref="Contexts"/>
/// through <see cref="EditionInfo.Cite"/>, so a program compiled at <c>--std 85</c> reads the 1985 rule and one compiled
/// later reads §2023:8.4.3.1.2 and §2023:13.18.38.3 SR7. The ONE place the wordings are written.
/// <para>The 1985 rule is §1985:VI.5.8.3 SR13: the index-name "not being data, cannot be associated with any data
/// hierarchy"; the 1985 standard names no closed list of contexts, so the contexts arm cites the same rule.</para>
/// </summary>
internal static class IndexNameRule
{
    /// <summary>Why an index-name is no identifier (without the "ISO " lead-in).</summary>
    public static string NotAnIdentifier(this EditionInfo edition) =>
        edition.Cite("§1985:VI.5.8.3 SR13", "§2023:8.4.3.1.2");

    /// <summary>The rule that closes the contexts an index-name may appear in (without the "ISO " lead-in).</summary>
    public static string Contexts(this EditionInfo edition) =>
        edition.Cite("§1985:VI.5.8.3 SR13", "§2023:13.18.38.3 SR7");

    /// <summary>The rule that makes a reference-modification leftmost-position and length arithmetic expressions (without the
    /// "ISO " lead-in): §1985:IV.4.3.8.4.3 SR2 at 85, §2023:8.4.3.3.3 SR4 later (kb/Work PB6088). It is not the OCCURS rule
    /// of <see cref="NotAnIdentifier"/>, which says nothing of reference modification.</summary>
    public static string ReferenceModificationBounds(this EditionInfo edition) =>
        edition.Cite("§1985:IV.4.3.8.4.3 SR2", "§2023:8.4.3.3.3 SR4");

    /// <summary>The clause of the COBOL1637 reference-modification message that says why the bound is outside the positions
    /// an index-name may take (kb/Work PB6088). From COBOL-2002 on the standard closes a list (<see cref="Contexts"/>: five
    /// contexts); the 1985 text gives none, so at 85 the clause says only what its formats show, and claims no count.</summary>
    public static string ReferenceModificationContext(this EditionInfo edition) =>
        edition.Cite(
            "the formats of the 1985 standard show an index-name only as a subscript, in PERFORM/SEARCH VARYING, in SET "
                + "and in a relation condition, and a reference-modification leftmost-position/length is none of these",
            $"a reference-modification leftmost-position/length is not one of {edition.Contexts()}'s five contexts (a "
                + "subscript, PERFORM/SEARCH VARYING, SET, a relation condition)");

    /// <summary>Why an index-name is no operand of an arithmetic expression (without the "ISO " lead-in).</summary>
    public static string NotArithmetic(this EditionInfo edition) =>
        edition.Cite("§1985:VI.5.8.3 SR13", "§2023:8.8.1.1 names no index-names; §2023:13.18.38.3 SR7");
}
