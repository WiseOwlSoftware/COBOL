// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>⛔ THE COBOL-85 DESIGNATIONS OF THE SYNTAX RULES A DATA-NAME CLAUSE OPERAND BREAKS WHERE THE 2023 TEXT HAS ANOTHER RULE
/// (kb/Work PB5627), each one a <see cref="Rule1985"/> beside the 2023 citation of the arm that asks it, so a refusal at
/// <c>--std 85</c> names the rule that edition prints. <c>RecordClauseRuleDriftTests.Every1985Designation_QuotesItsOwnNumberedRuleOfTheClauseItCites</c>
/// re-derives each from <c>specs/ANSI_COBOL_1985.md</c>. A 2023 rule with no 1985 counterpart (a dynamic-length item, a BASED
/// clause, a LOCAL-STORAGE section) is unreachable at COBOL-85 and has no designation here.</summary>
internal static class ClauseOperandRules1985
{
    /// <summary>§1985:IV.4.3.8.2.3 SR4 — a table element is named subscripted, and the file and report clauses that take a data-name
    /// (RECORD KEY, ALTERNATE RECORD KEY, RELATIVE KEY, FILE STATUS, LINAGE) are not among the contexts that write none. At
    /// COBOL-85 that is why an operand subject to an OCCURS clause is refused; §2023:12.4.5.12.3 SR1, §2023:12.4.5.6.3 SR1,
    /// §2023:12.4.5.13.3 SR1, §2023:12.4.5.8.3 SR1 and §2023:13.18.34.3 SR1 say it for each clause.</summary>
    internal static readonly Rule1985 TableElementSubscripted = new("IV.4.3.8.2.3", 4, "Each table element reference must be subscripted");

    /// <summary>§1985:VII.2.5.3 SR2 — the FILE STATUS item (§2023:12.4.5.8.3 SR2).</summary>
    internal static readonly Rule1985 FileStatusItem = new("VII.2.5.3", 2,
        "must be defined in the Data Division as a two-character data item of the category alphanumeric and must not be defined in the File Section, Report Section, or the Communication Section");

    /// <summary>§1985:VIII.2.4.3 SR2 — the RELATIVE KEY item's category and picture (§2023:12.4.5.13.3 SR2).</summary>
    internal static readonly Rule1985 RelativeKeyUnsignedInteger = new("VIII.2.4.3", 2, "must reference an unsigned integer data item");

    /// <summary>§1985:VIII.2.4.3 SR3 — the RELATIVE KEY item is not in a record of its file (§2023:12.4.5.13.3 SR3).</summary>
    internal static readonly Rule1985 RelativeKeyNotInRecord = new("VIII.2.4.3", 3,
        "must not be defined in a record description entry associated with that file-name");
}
