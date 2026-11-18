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
        edition.Cite("§1985:VI.5.8.3 SR13", "§2023:8.4.3.1.2", "§2023:8.4.3.1.2");

    /// <summary>The rule that closes the contexts an index-name may appear in (without the "ISO " lead-in).</summary>
    public static string Contexts(this EditionInfo edition) =>
        edition.Cite("§1985:VI.5.8.3 SR13", "§2023:13.18.38.3 SR7", "§2023:13.18.38.3 SR7");

    /// <summary>Why an index-name is no operand of an arithmetic expression (without the "ISO " lead-in).</summary>
    public static string NotArithmetic(this EditionInfo edition) =>
        edition.Cite("§1985:VI.5.8.3 SR13", "§2023:8.8.1.1 names no index-names; §2023:13.18.38.3 SR7", "§2023:8.8.1.1 names no index-names; §2023:13.18.38.3 SR7");
}
