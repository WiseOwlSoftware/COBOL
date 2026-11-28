// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ HOW A DIAGNOSTIC ABOUT AN INTRINSIC FUNCTION'S ARGUMENT CITES THE RULE OF THE EDITION IN FORCE (kb/Work PB4950: "a diagnostic
/// cite the rule of the edition the program was compiled under"; PB5922 for 2002 and 2014). The argument rules are written once, as
/// §2023:15.n.3 clauses on the schema rows of <see cref="IntrinsicArgumentRules"/>; a program compiled at <c>--std 85</c> has the functions
/// only through the 1989 amendment's Intrinsic Function module (ANSI X3.23a-1989 Section A), whose paragraph for a function states the
/// function's argument rules under <c>A.2.n.3</c> ("Arguments") and whose paragraph A.2.2 states the argument types; a program compiled at
/// <c>--std 2002</c> or <c>--std 2014</c> has them under the function's own <c>15.n.2</c> clause of that edition. This class is the
/// ONE place the choice is made, so a message asks it instead of spelling its own edition comparison.
/// <para>A function the edition does not define (every one a later edition introduced) has no rule to cite there, so its message
/// keeps the 2023 clause and says so with the edition gate's own phrase, "(targeting COBOL-85)": the construct is not
/// in the edition, and the gate (COBOL1502) already refused the function.</para>
/// </summary>
internal static class IntrinsicCitations
{
    /// <summary>The clause that states <paramref name="function"/>'s argument rule, as the edition in force numbers it: the 2023 clause
    /// <paramref name="clause2023"/> (<c>§2023:15.84.3 AR1</c>) at COBOL-2023, the same rule under the function's own clause of ISO/IEC 1989:2002
    /// or 1989:2014 at those editions (<c>§2002:15.68.2 AR1</c>; <see cref="Edition2002"/> and <see cref="Edition2014"/>), and at COBOL-85
    /// the "Arguments" subclause of the function's own paragraph in the 1989 module (<c>§1989:A.2.40.3</c>).</summary>
    public static string Clause(EditionInfo edition, string function, string clause2023) =>
        edition.Cite(Clause1985(edition, function, clause2023), clause2023,
            OfEdition(edition, 2002, function, clause2023), OfEdition(edition, 2014, function, clause2023));

    /// <summary>The clause of an argument rule the 1989 module lacks (kb/Work PB5922; a zero-length-literal rule, NUMVAL-C's currency-string content
    /// rule): <see cref="Clause"/> at COBOL-2002 and later, and at COBOL-85, which has no such rule to cite, the 2023 clause unchanged.</summary>
    public static string ClauseSince2002(EditionInfo edition, string function, string clause2023) =>
        edition.Cite(clause2023, clause2023, OfEdition(edition, 2002, function, clause2023), OfEdition(edition, 2014, function, clause2023));

    private static string Clause1985(EditionInfo edition, string function, string clause2023) =>
        IntrinsicCatalog.Paragraph1989(function) is { } paragraph ? $"§1989:A.2.{paragraph}.3" : NotInEdition(edition, function, clause2023);

    /// <summary>The clause of a function the edition lacks: the 2023 clause with the edition gate's own phrase.</summary>
    private static string NotInEdition(EditionInfo edition, string function, string clause2023)
    {
        int introduced = IntrinsicCatalog.TryGet(function, out var sig) ? sig.IntroducedIn : 2002;
        return $"{clause2023}; FUNCTION {function} begins in COBOL-{introduced} (targeting COBOL-{edition.Year})";
    }

    /// <summary>A 2023 argument-rule citation inside a clause: the function's clause, the subclause (<c>.2</c> general format, <c>.3</c> argument
    /// rules) and the rules written as <c>AR n</c>, <c>rule n</c> or <c>r n</c> (<c>AR1/AR3</c> for several), with an optional sub-item letter.</summary>
    private static readonly Regex Citation2023 = new(
        @"§2023:15\.(?<clause>\d+)\.(?<sub>[23])(?<rules> (?:AR|rule |r)\s*\d+[a-z]*(?:/(?:AR)?\d+[a-z]*)*)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RuleOrdinal = new(@"\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The clause <paramref name="clause2023"/> names, as ISO/IEC 1989:<paramref name="year"/> (2002 or 2014) numbers it when the program
    /// compiles at that edition, else null (⛔ kb/Work PB5922). Every §2023:15.n.2 / §2023:15.n.3 citation in the text is read: n names the function
    /// (<see cref="IntrinsicCatalog.FunctionAt"/>), the edition prints its general format as <c>15.m.1</c> and its arguments as <c>15.m.2</c>
    /// (<see cref="IntrinsicCatalog.Paragraph"/> numbers m), and an argument rule is the same rule under another ordinal where
    /// <see cref="Edition2002"/> or <see cref="Edition2014"/> say so; where the edition has no such rule the citation is the whole arguments
    /// subclause. A sub-item letter is dropped: 2002 prints a rule's sub-items as dashes. A function the edition lacks gives the clause
    /// with the edition gate's phrase (<see cref="NotInEdition"/>). A text with no such citation throws, so a rule written in a form this does
    /// not read fails the drift test at once instead of citing 2023 at 2002.</summary>
    private static string? OfEdition(EditionInfo edition, int year, string function, string clause2023)
    {
        if (edition.Year != year) return null;
        if (!Citation2023.IsMatch(clause2023)) throw new ArgumentException($"'{clause2023}' cites no §2023:15.n.2 or §2023:15.n.3 clause", nameof(clause2023));
        var counterparts = year == 2002 ? Edition2002 : Edition2014;
        bool lacksAFunction = false;
        string clause = Citation2023.Replace(clause2023, m =>
        {
            string? named = IntrinsicCatalog.FunctionAt(2023, int.Parse(m.Groups["clause"].Value, CultureInfo.InvariantCulture));
            if (named is null || IntrinsicCatalog.Paragraph(year, named) is not { } number) { lacksAFunction = true; return m.Value; }
            string subclause = $"§{year}:15.{number}.{(m.Groups["sub"].Value == "2" ? 1 : 2)}";
            if (!m.Groups["rules"].Success) return subclause;
            var rules = RuleOrdinal.Matches(m.Groups["rules"].Value).Select(o => int.Parse(o.Value, CultureInfo.InvariantCulture))
                .Select(o => counterparts.TryGetValue((named, o), out var other) ? other : o)
                .Where(o => o is not null).Select(o => o!.Value).ToArray();
            return rules.Length == 0 ? subclause : $"{subclause} AR{string.Join("/AR", rules)}";
        });
        return lacksAFunction ? NotInEdition(edition, function, clause2023) : clause;
    }

    /// <summary>The argument rules COBOL-2002 numbers differently from COBOL-2023 or lacks (kb/Work PB5922): (function, 2023's rule) to the
    /// ordinal in <c>§2002:15.n.2</c>, null where 2002 has no such rule. A rule absent here is the same rule under the same ordinal. Each pair
    /// was found by matching the 2023 rule's text against the 2002 clause's rules and read; <c>IntrinsicEditionClausesDriftTests</c> holds every
    /// ordinal to the rules the 2002 text prints.</summary>
    internal static readonly IReadOnlyDictionary<(string Function, int Rule), int?> Edition2002 = new Dictionary<(string, int), int?>
    {
        [("EXCEPTION-FILE", 1)] = null, [("EXCEPTION-FILE-N", 1)] = null,   // 2023 added the optional file-connector argument
        [("HIGHEST-ALGEBRAIC", 2)] = null, [("HIGHEST-ALGEBRAIC", 3)] = null, [("LOWEST-ALGEBRAIC", 2)] = null, [("LOWEST-ALGEBRAIC", 3)] = null,
        [("MAX", 3)] = null, [("MIN", 3)] = null, [("NATIONAL-OF", 3)] = null,   // the zero-length literal rule is 2023's
        [("NUMVAL", 2)] = null, [("NUMVAL", 3)] = 2, [("NUMVAL", 4)] = null, [("NUMVAL", 5)] = 3,
        [("NUMVAL-C", 3)] = 4, [("NUMVAL-C", 4)] = 5, [("NUMVAL-C", 5)] = 6, [("NUMVAL-C", 6)] = 7, [("NUMVAL-C", 7)] = null,
        [("NUMVAL-F", 3)] = null, [("NUMVAL-F", 4)] = 3, [("NUMVAL-F", 5)] = null,
        [("ORD-MAX", 2)] = null, [("ORD-MAX", 3)] = 2, [("ORD-MIN", 2)] = null, [("ORD-MIN", 3)] = 2,
        [("STANDARD-COMPARE", 4)] = null, [("STANDARD-COMPARE", 5)] = 4, [("STANDARD-COMPARE", 6)] = 5,
        [("TEST-NUMVAL-C", 1)] = null,   // 2023 refers to NUMVAL-C's rules; 2002 states its own
    };

    /// <summary>The argument rules COBOL-2014 numbers differently from COBOL-2023 or lacks: as <see cref="Edition2002"/>, for <c>§2014:15.n.2</c>.</summary>
    internal static readonly IReadOnlyDictionary<(string Function, int Rule), int?> Edition2014 = new Dictionary<(string, int), int?>
    {
        [("EXCEPTION-FILE", 1)] = null, [("EXCEPTION-FILE-N", 1)] = null,
        [("HIGHEST-ALGEBRAIC", 2)] = 3, [("HIGHEST-ALGEBRAIC", 3)] = 4, [("LOWEST-ALGEBRAIC", 2)] = 3, [("LOWEST-ALGEBRAIC", 3)] = 4,
        [("NUMVAL-C", 3)] = 4, [("NUMVAL-C", 4)] = 5, [("NUMVAL-C", 5)] = 6, [("NUMVAL-C", 6)] = 7, [("NUMVAL-C", 7)] = 8,
        [("TRIM", 2)] = null, [("TRIM", 3)] = null,   // 2014's TRIM has no argument-2
    };

    /// <summary>The clause that states the TYPES of argument a function takes — §2023:15.3 at COBOL-2023, §2014:15.3 and §2002:15.2 at those
    /// editions, paragraph A.2.2 of the 1989 module at COBOL-85.</summary>
    public static string ArgumentTypes(EditionInfo edition) => edition.Cite("§1989:A.2.2", "§2023:15.3", "§2002:15.2", "§2014:15.3");

    /// <summary>The rule that states argument type 6, INTEGER — §2023:15.3 AR6, the fifth type of §2014:15.3 and §2002:15.2 (AR5); at COBOL-85
    /// the fourth type of paragraph A.2.2 ("An arithmetic expression which will always result in an integer value must be specified").</summary>
    public static string IntegerType(EditionInfo edition) => edition.Cite("§1989:A.2.2 AR4", "§2023:15.3 AR6", "§2002:15.2 AR5", "§2014:15.3 AR5");

    /// <summary>What <see cref="IntegerType"/> admits, as a message says it: from COBOL-2002 on "an integer data item or an
    /// always-integral arithmetic expression" (§2023:15.3 AR6, §2014:15.3 AR5 and §2002:15.2 AR5: "An arithmetic expression that will always
    /// result in an integer value or an integer data item shall be specified"); at COBOL-85 the expression alone (§1989:A.2.2 AR4: "An
    /// arithmetic expression which will always result in an integer value must be specified", which names no integer data item).</summary>
    public static string IntegerTypeAdmits(EditionInfo edition) => edition.Cite(
        "an arithmetic expression that always results in an integer value",
        "an integer data item or an always-integral arithmetic expression");

    /// <summary>Where a floating-point numeric data item is defined, as a message names it: §2023:14.6.8.3 at COBOL-2023, and at COBOL-2002 and
    /// COBOL-2014 the USAGE clause rule that defines the FLOAT-SHORT, FLOAT-LONG and FLOAT-EXTENDED usages; at COBOL-85 nowhere, because the
    /// USAGE clause's general format (§1985:VI.5.14.2) has no floating-point usage, so COMP-1 and COMP-2 are this implementation's extension there.</summary>
    public static string FloatingPointItem(EditionInfo edition) => edition.Cite(
        "USAGE COMP-1 or COMP-2, an extension at COBOL-85, whose USAGE clause (§1985:VI.5.14.2) has no floating-point usage",
        EditionInfo.Designated("§2023:14.6.8.3"),
        EditionInfo.Designated("§2002:13.16.58.3 GR12"), EditionInfo.Designated("§2014:13.18.60.3 GR12"));

    /// <summary>The rule that bars a numeric function from a position that needs an integer — §2023:8.4.3.2.3 SR11, §2014:8.4.2.2.2 SR11 and
    /// §2002:8.4.2.2.2 SR9; at COBOL-85 paragraph A.2.3 of the 1989 module ("A numeric function may not be referenced where an integer operand
    /// is required").</summary>
    public static string NumericFunctionAsInteger(EditionInfo edition) =>
        edition.Cite("§1989:A.2.3", "§2023:8.4.3.2.3 SR11", "§2002:8.4.2.2.2 SR9", "§2014:8.4.2.2.2 SR11");
}
