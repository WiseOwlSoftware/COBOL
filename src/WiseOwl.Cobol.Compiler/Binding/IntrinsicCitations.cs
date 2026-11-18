// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
    /// <paramref name="clause2023"/> (<c>§2023:15.84.3 r1</c>) from COBOL-2002 on, and at COBOL-85 the "Arguments" subclause of the
    /// function's own paragraph in the 1989 module (<c>§1989:A.2.40.3</c>).</summary>
    public static string Clause(EditionInfo edition, string function, string clause2023)
    {
        if (edition.Year >= 2002) return clause2023;
        if (IntrinsicCatalog.Paragraph1989(function) is { } paragraph) return $"§1989:A.2.{paragraph}.3";
        int introduced = IntrinsicCatalog.TryGet(function, out var sig) ? sig.IntroducedIn : 2002;
        return $"{clause2023}; FUNCTION {function} begins in COBOL-{introduced} (targeting COBOL-{edition.Year})";
    }

    /// <summary>The clause that states the TYPES of argument a function takes — §2023:15.3 from COBOL-2002 on, paragraph A.2.2 of the
    /// 1989 module at COBOL-85.</summary>
    public static string ArgumentTypes(EditionInfo edition) => edition.Cite("§1989:A.2.2", "§2023:15.3");

    /// <summary>The rule that states argument type 6, INTEGER — §2023:15.3 type 6; at COBOL-85 the fourth type of paragraph A.2.2
    /// ("An arithmetic expression which will always result in an integer value must be specified").</summary>
    public static string IntegerType(EditionInfo edition) => edition.Cite("§1989:A.2.2 AR4", "§2023:15.3 type 6");

    /// <summary>The rule that bars a numeric function from a position that needs an integer — §2023:8.4.3.2.3 SR11; at COBOL-85
    /// paragraph A.2.3 of the 1989 module ("A numeric function may not be referenced where an integer operand is required").</summary>
    public static string NumericFunctionAsInteger(EditionInfo edition) =>
        edition.Cite("§1989:A.2.3", "§2023:8.4.3.2.3 SR11");
}
