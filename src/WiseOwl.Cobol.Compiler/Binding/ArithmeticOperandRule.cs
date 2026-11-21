// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ WHAT AN ARITHMETIC EXPRESSION MAY BE, IN THE EDITION IN FORCE (kb/Work PB4950): every COBOL0844 message cites
/// <see cref="ArithmeticCitation"/> and states <see cref="Admits"/> through <see cref="EditionInfo.Cite"/>, so a program compiled at
/// <c>--std 85</c> reads §1985:VI.6.2.1 ("An arithmetic expression can be an identifier of a numeric elementary item, a numeric
/// literal, the figurative constant ZERO (ZEROS, ZEROES), ...") and one compiled later reads §2023:8.8.1.1. The ONE place the two
/// wordings are written: the expression binder, the subscript binder and the intrinsic-argument screen ask it.
/// </summary>
internal static class ArithmeticOperandRule
{
    /// <summary>The rule's citation in the edition form, without the "ISO " lead-in.</summary>
    public static string ArithmeticCitation(this EditionInfo edition) =>
        edition.Cite("§1985:VI.6.2.1", "§2023:8.8.1.1");

    /// <summary>The verb phrase "admits only ...", the rule's content in the edition's own words.</summary>
    public static string Admits(this EditionInfo edition) =>
        edition.Cite(
            "admits only an identifier of a numeric elementary item, a numeric literal, or the figurative constant ZERO (ZEROS, ZEROES)",
            "admits only an identifier referencing a NUMERIC data item, a numeric literal, or the figurative constant ZERO");

    /// <summary>The citation of the rule that gives an item its class: the 1985 concept of classes of data, or 2023's Table 2.</summary>
    public static string ClassRule(this EditionInfo edition) =>
        edition.Cite("§1985:IV.4.3.3", "§2023:8.5.2.1 Table 2");

    /// <summary>The clause naming where an index data item may be referenced, in the edition's own words.</summary>
    public static string IndexDataItemRule(this EditionInfo edition) =>
        edition.Cite(
            "§1985:VI.5.14.3 SR5 admits an index data item only in a SEARCH or SET statement, a relation condition, or the USING phrase of a Procedure Division header or CALL statement",
            "ISO §2023:13.18.60.3 SR10 admits an index data item only in SEARCH/SET, a relation condition, or an intrinsic argument");
}
