// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ HOW A DIAGNOSTIC ABOUT AN INTRINSIC FUNCTION'S ARGUMENT CITES THE RULE OF THE EDITION IN FORCE (kb/Work PB4950: "a diagnostic
/// cite the rule of the edition the program was compiled under"). The argument rules are written once, as §2023:15.n.3 clauses on
/// the schema rows of <see cref="IntrinsicArgumentRules"/>; a program compiled at <c>--std 85</c> has the functions only through
/// the 1989 amendment's Intrinsic Function module (ANSI X3.23a-1989 Section A), whose paragraph for a function states the
/// function's argument rules under <c>A.2.n.3</c> ("Arguments") and whose paragraph A.2.2 states the argument types. This class is the
/// ONE place the choice is made, so a message asks it instead of spelling its own <c>Year &lt; 2002</c>.
/// <para>A function the 1989 module does not define (every one a later edition introduced) has no 1985 rule to cite, so its message
/// at COBOL-85 keeps the 2023 clause and says so with the edition gate's own phrase, "(targeting COBOL-85)": the construct is not
/// in the edition, and the gate (COBOL1502) already refused the function.</para>
/// </summary>
internal static class IntrinsicCitations
{
    /// <summary>The clause that states <paramref name="function"/>'s argument rule, as the edition in force numbers it:
    /// <paramref name="clause2023"/> (<c>§2023:15.84.3 AR1</c>, or several joined in prose) at COBOL-2023, the same rule of COBOL-2002
    /// and COBOL-2014 through <see cref="IntrinsicCitationConcordance"/>, and at COBOL-85 the "Arguments" subclause of the
    /// function's own paragraph in the 1989 module (<c>§1989:A.2.40.3</c>).</summary>
    public static string Clause(EditionInfo edition, string function, string clause2023)
    {
        if (edition.Year >= 2002) return Translate(edition, clause2023);
        if (IntrinsicCatalog.Paragraph1989(function) is { } paragraph) return $"§1989:A.2.{paragraph}.3";
        int introduced = IntrinsicCatalog.TryGet(function, out var sig) ? sig.IntroducedIn : 2002;
        return $"{clause2023}; FUNCTION {function} begins in COBOL-{introduced} (targeting COBOL-{edition.Year})";
    }

    /// <summary>⛔ A §2023:15 CITATION A MESSAGE SPELLS, AS THE EDITION IN FORCE PRINTS IT (kb/Work PB6118, PB4950): the citation
    /// <paramref name="cite2023"/> (one, <c>§2023:15.19.3 AR3</c>; the legacy spellings <c>r3</c> and <c>rule 3</c> are read as the
    /// rule) as the standard of the program's edition numbers it, its standard named (<see cref="EditionInfo.Designated"/>).
    /// COBOL-2023 prints it unchanged; COBOL-2002 and COBOL-2014 the rule of the same function in their own text
    /// (<see cref="IntrinsicCitationConcordance"/>, found by wording), or the subclause that holds it where the wording differs;
    /// COBOL-85 the function's paragraph of the 1989 module. A function or clause the edition lacks prints the 2023 rule and says which
    /// edition begins it (the construct gate's own phrase, <c>(targeting COBOL-2002)</c>), never a rule of an edition without it.</summary>
    public static string Rule(EditionInfo edition, string cite2023) => EditionInfo.Designated(BareRule(edition, cite2023));

    /// <summary><see cref="Rule"/> where the message names no standard beside the citation (one written bare, <c>(§2023:15.37.2)</c>, stays bare).</summary>
    public static string BareRule(EditionInfo edition, string cite2023) =>
        Cite2023.Match(cite2023) is { Success: true } m
            ? ForEdition(edition, m)
            : throw new ArgumentException($"'{cite2023}' is no §2023:15 citation", nameof(cite2023));

    /// <summary>Every §2023:15 citation of <paramref name="text"/> as the edition in force prints it.</summary>
    private static string Translate(EditionInfo edition, string text) => Cite2023.Replace(text, m => ForEdition(edition, m));

    /// <summary>One matched citation as <paramref name="edition"/> prints it: its own edition's rule from the concordance (COBOL-2002,
    /// COBOL-2014), the 2023 text, or at COBOL-85 the function's paragraph of the 1989 module; where the function or clause is
    /// absent from the edition, <see cref="Absent"/>.</summary>
    private static string ForEdition(EditionInfo edition, Match m)
    {
        var row = RowOf(m);
        return edition.Cite(
            row.In1985 ?? (row.Function is { } function && IntrinsicCatalog.Paragraph1989(function) is { } paragraph
                ? $"§1989:A.2.{paragraph}.3"
                : Absent(edition, m.Value, row)),
            m.Value,
            row.In2002 ?? Absent(edition, m.Value, row),
            row.In2014 ?? Absent(edition, m.Value, row));
    }

    /// <summary>The row of a matched citation, or a loud failure naming the generator that writes the table: an unresolvable
    /// citation never prints a guess.</summary>
    private static IntrinsicCitationConcordance.Row RowOf(Match m) =>
        IntrinsicCitationConcordance.IndexOf(CanonicalKey(m)) is >= 0 and var at
            ? IntrinsicCitationConcordance.Rows[at]
            : throw new InvalidOperationException(
                $"IntrinsicCitationConcordance has no row for '{m.Value}': run scripts/spec/gen_intrinsic_citation_concordance.py");

    /// <summary>The text of a citation of a function or clause the edition does not have: the 2023 rule and the edition that begins it.</summary>
    private static string Absent(EditionInfo edition, string cite2023, IntrinsicCitationConcordance.Row row)
    {
        int begins = row.In2002 is not null ? 2002 : row.In2014 is not null ? 2014 : 2023;
        string what = row.Function is { } function ? $"FUNCTION {function}" : "this clause";
        return $"{cite2023}; {what} begins in COBOL-{begins} (targeting COBOL-{edition.Year})";
    }

    /// <summary>The one reading of a §2023:15 citation: clause <c>15.n</c>, its subclauses, and an optional rule written <c>AR3</c>,
    /// <c>RV2</c>, <c>r1</c>, <c>rule 1</c> or <c>argument rule 1</c> (a trailing sub-item letter is read at its rule), several chained
    /// <c>AR1/AR3</c>. <c>scripts/spec/gen_intrinsic_citation_concordance.py</c> spells the same pattern.</summary>
    internal static readonly Regex Cite2023 = new(
        @"§2023:15\.(\d+)((?:\.\d+)*)(?: (AR|RV|r|rule|argument rule) ?(\d+)(?:[a-z]\d?)?((?:/(?:AR|RV)\d+)*))?(?!\w)(?!\.\d)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>The concordance's key of a matched citation, <c>15.19.3 AR3</c>: the rule's kind is taken from its subclause when
    /// written <c>r</c> or <c>rule</c> (a returned-value rule, <c>.4</c>, is <c>RV</c>).</summary>
    internal static string CanonicalKey(Match m)
    {
        string key = $"15.{m.Groups[1].Value}{m.Groups[2].Value}";
        if (!m.Groups[3].Success) return key;
        string kind = m.Groups[3].Value is "AR" or "RV" ? m.Groups[3].Value : m.Groups[2].Value == ".4" ? "RV" : "AR";
        return $"{key} {kind}{m.Groups[4].Value}{m.Groups[5].Value}";
    }

    /// <summary>The clause that states the TYPES of argument a function takes — §2023:15.3 (§2002:15.2, §2014:15.3), paragraph A.2.2 of the
    /// 1989 module at COBOL-85.</summary>
    public static string ArgumentTypes(EditionInfo edition) => edition.Cite("§1989:A.2.2", "§2023:15.3", "§2002:15.2", "§2014:15.3");

    /// <summary>The rule that states argument type 6, INTEGER — §2023:15.3 AR6; at COBOL-85 the fourth type of paragraph A.2.2
    /// ("An arithmetic expression which will always result in an integer value must be specified").</summary>
    public static string IntegerType(EditionInfo edition) => edition.Cite("§1989:A.2.2 AR4", "§2023:15.3 AR6", "§2002:15.2 AR5", "§2014:15.3 AR5");

    /// <summary>What <see cref="IntegerType"/> admits, as a message says it: from COBOL-2002 on "an integer data item or an
    /// always-integral arithmetic expression" (§2023:15.3 AR6: "An arithmetic expression that will always result in an integer value
    /// or an integer data item shall be specified"); at COBOL-85 the expression alone (§1989:A.2.2 AR4: "An arithmetic expression
    /// which will always result in an integer value must be specified", which names no integer data item).</summary>
    public static string IntegerTypeAdmits(EditionInfo edition) => edition.Cite(
        "an arithmetic expression that always results in an integer value",
        "an integer data item or an always-integral arithmetic expression");

    /// <summary>Where a floating-point numeric data item is defined, as a message names it: §2023:14.6.8.3 from COBOL-2002 on
    /// (the FLOAT-SHORT, FLOAT-LONG and FLOAT-EXTENDED usages); at COBOL-85 nowhere, because the USAGE clause's general format
    /// (§1985:VI.5.14.2) has no floating-point usage, so COMP-1 and COMP-2 are this implementation's extension there.</summary>
    public static string FloatingPointItem(EditionInfo edition) => edition.Cite(
        "USAGE COMP-1 or COMP-2, an extension at COBOL-85, whose USAGE clause (§1985:VI.5.14.2) has no floating-point usage",
        EditionInfo.Designated("§2023:14.6.8.3"), EditionInfo.Designated("§2002:13.16.58.3 GR13"), EditionInfo.Designated("§2014:14.6.8.2"));

    /// <summary>The rule that bars a numeric function from a position that needs an integer — §2023:8.4.3.2.3 SR11; at COBOL-85
    /// paragraph A.2.3 of the 1989 module ("A numeric function may not be referenced where an integer operand is required").</summary>
    public static string NumericFunctionAsInteger(EditionInfo edition) =>
        edition.Cite("§1989:A.2.3", "§2023:8.4.3.2.3 SR11", "§2002:8.4.2.2.2 SR9", "§2014:8.4.2.2.2 SR11");
}
