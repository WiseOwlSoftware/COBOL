// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A SUBSCRIPTED data-name-n OPERAND IS REFUSED BY ITS OWN CLAUSE'S RULE, IN THE EDITION IN FORCE (kb/Work PB2041). The one
/// data-name-n screen (<c>DataBinder.ScreenDataNameShape</c>, COBOL2024) used to close every clause operand's refusal with one
/// fixed sentence, "the operand shall not be subject to any OCCURS clauses", which is false for an OCCURS KEY data-name-2 (it
/// lies under the table's OCCURS by definition) and is not the rule OCCURS DEPENDING, CAPACITY or RENAMES state; and at
/// COBOL-85 it quoted the Identifier definition Amendment 1 replaced. Now each caller passes its clause's rule: at 85 the
/// data-name definition, §1985:III.2 p69 ("represents a word which must not be reference-modified, subscripted, or qualified
/// unless specifically permitted by the rules of the format"), with the clause's own 1985 sentence where it has one; at 2023
/// the clause's own syntax rule. Expected texts are the rules', each checked with <c>cite.py --check</c>.
/// </summary>
public sealed class ClauseOperandSubscriptRuleTests
{
    private const string Fixed = "the operand shall not be subject to any OCCURS clauses";
    private const string DataName85 = "\"represents a word which must not be reference-modified, subscripted, or qualified unless "
        + "specifically permitted by the rules of the format\" (§1985:III.2 p69)";

    private static string Program(string fileControl, string fileSection, string ws) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB2041.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "pb2041.dat"{fileControl}.
        DATA DIVISION.
        FILE SECTION.
        FD F{fileSection}.
        01 FR PIC X(10).
        WORKING-STORAGE SECTION.
        01 N PIC 9 VALUE 1.
        01 T.
            05 TS PIC XX OCCURS 2.
            05 TN PIC 9 OCCURS 2.
        {ws}
        PROCEDURE DIVISION.
        MAIN-P.
            STOP RUN.
        """;

    [Theory]
    // OCCURS KEY data-name-2: 1985 has a sentence of its own; 2023 SR2. The key lies under the OCCURS, so the fixed sentence was false.
    [InlineData(85, "", "", "01 TK.\n    05 E OCCURS 3 ASCENDING KEY K(1) INDEXED BY I.\n        10 K PIC X.",
        "OCCURS KEY IS 'K(1)'", "data-name-2 \"must be specified without the subscripting normally required\" (§1985:VI.5.8.3 SR4)")]
    [InlineData(2023, "", "", "01 TK.\n    05 E OCCURS 3 ASCENDING KEY K(1) INDEXED BY I.\n        10 K PIC X.",
        "OCCURS KEY IS 'K(1)'", "\"Data-name-1 and data-name-2 shall not be subscripted\" (ISO §2023:13.18.38.3 SR2)")]
    // OCCURS DEPENDING ON data-name-1: no 1985 sentence of its own (§1985:VI.5.8.3 SR2 says only "may be qualified").
    [InlineData(85, "", "", "01 TD.\n    05 E PIC X OCCURS 1 TO 3 DEPENDING ON TN(1).", "OCCURS DEPENDING ON 'TN(1)'", DataName85)]
    [InlineData(2023, "", "", "01 TD.\n    05 E PIC X OCCURS 1 TO 3 DEPENDING ON TN(1).",
        "OCCURS DEPENDING ON 'TN(1)'", "\"Data-name-1 and data-name-2 shall not be subscripted\" (ISO §2023:13.18.38.3 SR2)")]
    // RENAMES data-name-2.
    [InlineData(85, "", "", "66 X1 RENAMES TS(1).", "RENAMES 'TS(1)'", DataName85)]
    [InlineData(2023, "", "", "66 X1 RENAMES TS(1).", "RENAMES 'TS(1)'",
        "\"Data-name-2 and data-name-3 shall not be subscripted\" (ISO §2023:13.18.45.3 SR7)")]
    // The file clauses: a FILE STATUS item and a LINAGE item shall not be under an OCCURS at all (2023), so the sentence is theirs.
    [InlineData(85, "\n    FILE STATUS IS TS(1)", "", "", "FILE STATUS 'TS(1)'", DataName85)]
    [InlineData(2023, "\n    FILE STATUS IS TS(1)", "", "", "FILE STATUS 'TS(1)'",
        "\"Data-name-1 shall not be subject to any OCCURS clauses\" (ISO §2023:12.4.5.8.3 SR1)")]
    [InlineData(2023, "", "\n    LINAGE IS TN(1) LINES", "", "LINAGE clause operand 'TN(1)'",
        "shall not be subject to any OCCURS clauses\" (ISO §2023:13.18.34.3 SR1)")]
    public void SubscriptedOperand_IsRefusedByItsClausesOwnRule(int std, string fc, string fd, string ws, string face, string rule)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(fc, fd, ws), std);
        Assert.False(ok, $"[{face} @ {std}] must be REJECTED");
        string? refusal = errors.FirstOrDefault(e => e.Contains("COBOL2024", StringComparison.Ordinal) && e.Contains(face, StringComparison.Ordinal));
        Assert.True(refusal is not null, $"[{face} @ {std}] no COBOL2024 naming the operand: {string.Join(" | ", errors)}");
        Assert.Contains("is written with a subscript", refusal);
        Assert.Contains(rule, refusal);
        // The old closing sentence stands only where it is the clause's own words, and never the pre-Amendment Identifier quote.
        if (!rule.Contains("subject to any OCCURS clauses", StringComparison.Ordinal)) Assert.DoesNotContain(Fixed, refusal);
        Assert.DoesNotContain("syntactically correct combination of a data-name", refusal);
        if (std == 85)
        {
            Assert.Contains(DataName85, refusal);
            Assert.DoesNotContain("ISO §2023", refusal);
        }
    }
}
