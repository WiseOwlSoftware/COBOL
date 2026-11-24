// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding.Procedure;

/// <summary>
/// ⛔ THE CALL STATEMENT'S SYNTAX RULES AS EACH EDITION NUMBERS THEM (kb/Work PB6149, PB4950: "a diagnostic cite the rule of the
/// edition the program was compiled under"). A CALL message names its rule by its COBOL-2023 number, the number the code's comments
/// use, and asks <see cref="Rule"/> for the citation the edition in force prints:
/// <list type="bullet">
/// <item>COBOL-2023 and COBOL-2014 number the rules alike: §2023:14.9.4.3 SR<i>n</i> is §2014:14.9.4.2 SR<i>n</i> for every
/// <i>n</i> (each re-read with <c>cite.py --check</c> on its 2023 words, 2026-11-24).</item>
/// <item>COBOL-2002 (§2002:14.8.4.2) has no SR12 (the variable-length group: 2002 has no DYNAMIC LENGTH item) and no SR14 (a
/// restricted program-pointer), states the ANY LENGTH rules of SR11 and SR18 as its one SR10, and numbers the rest after them:
/// <see cref="Rule2002"/>.</item>
/// </list>
/// At COBOL-85 a rule the 1985 CALL has is the caller's own 1985 arm (<c>StorageSectionRule.CallUsing</c>, <c>ProgramName</c>); the
/// rest are rules of a CALL format COBOL-85 lacks, whose 2002 number is cited there.
/// </summary>
internal static class CallSyntaxRules
{
    /// <summary>The 2002 number of each 2023 syntax rule, by 2023 number (index 0 unused); 0 where 2002 has no such rule.</summary>
    private static readonly int[] Rule2002 =
        [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 10, 0, 12, 0, 13, 14, 15, 10, 16, 17, 18, 19, 20, 21, 22];

    /// <summary>The CALL syntax rule <paramref name="rule2023"/> (its COBOL-2023 number) as the edition in force numbers it, designated
    /// as a message prints it (<c>ISO §2002:14.8.4.2 SR19</c>).</summary>
    public static string Rule(EditionInfo edition, int rule2023) => EditionInfo.Designated(Clause(edition, rule2023));

    /// <summary>The CALL syntax rule <paramref name="rule2023"/> as the edition in force numbers it, undesignated
    /// (<c>§2002:14.8.4.2 SR19</c>). A rule 2002 lacks is cited by its 2014 number at 2002 and 85: such a rule's construct does not
    /// exist there, so its message cannot fire.</summary>
    public static string Clause(EditionInfo edition, int rule2023)
    {
        if (rule2023 < 1 || rule2023 >= Rule2002.Length)
            throw new ArgumentOutOfRangeException(nameof(rule2023), rule2023, "§2023:14.9.4.3 has syntax rules 1 to 25");
        string y2014 = $"§2014:14.9.4.2 SR{rule2023}";
        string y2002 = Rule2002[rule2023] is > 0 and int n ? $"§2002:14.8.4.2 SR{n}" : y2014;
        return edition.Cite(y2002, $"§2023:14.9.4.3 SR{rule2023}", y2002, y2014);
    }

    /// <summary>The conformance clause for parameters (§2023:14.8.2 "Parameters"; §2014:14.8.1; §2002:14.7.1) or, with
    /// <paramref name="returning"/>, for returning items (§2023:14.8.3; §2014:14.8.2; §2002:14.7.2) that an activation imports, as
    /// the edition in force numbers it, undesignated. COBOL-85 has neither clause; its 2002 number stands there.</summary>
    public static string Conformance(EditionInfo edition, bool returning = false) => returning
        ? edition.Cite("§2002:14.7.2", "§2023:14.8.3", "§2002:14.7.2", "§2014:14.8.2")
        : edition.Cite("§2002:14.7.1", "§2023:14.8.2", "§2002:14.7.1", "§2014:14.8.1");
}
