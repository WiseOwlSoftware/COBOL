// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Binding;

/// <summary>The Selection-SUBJECT operand kinds — Table 15's COLUMNS, in the printed left-to-right order.</summary>
public enum EvaluateSubjectOperand
{
    Identifier = 0,
    Literal = 1,
    ArithmeticExpression = 2,
    BooleanExpression = 3,
    Condition = 4,
    TrueOrFalse = 5,
}

/// <summary>The Selection-OBJECT operand kinds — Table 15's ROWS, in the printed top-to-bottom order.</summary>
public enum EvaluateObjectOperand
{
    Identifier = 0,
    Literal = 1,
    ArithmeticExpression = 2,
    BooleanExpression = 3,
    RangeExpression = 4,
    Condition = 5,
    PartialExpression = 6,
    TrueOrFalse = 7,
    Any = 8,
}

/// <summary>
/// ⛔ ISO §14.9.13.3 SR10 + <b>Table 15, "Combination of operands in the EVALUATE statement"</b> — which pairings
/// of a selection subject with a selection object are permissible. "The letter 'Y' indicates a permissible
/// combination. A space indicates an invalid combination."
///
/// <para><b>THIS IS A TABLE BECAUSE THE STANDARD PRINTS A TABLE</b> (fix-queue PB47, and CLAUDE.md rule 5: prefer
/// the shape that makes the NEXT case automatic). A chain of <c>if</c>s over 54 cells is a hand-maintained list
/// where a structure belongs, and it cannot be checked against the source it came from. This one can:
/// <c>EvaluateOperandCombinationsDriftTests</c> re-parses Table 15 out of <c>specs/ISO_COBOL.md</c> and asserts
/// this matrix equals it cell for cell, so a transcription fix upstream fails the battery here rather than
/// silently diverging.</para>
///
/// <para>⚠ <b>What this table does NOT decide is which KIND an operand is.</b> That classification is
/// <c>EvaluateBinder.ClassifyPair</c>'s, and it is a JOINT question, not two independent ones: §14.9.13.3 SR6
/// changes a boolean operand's kind "for a particular WHEN phrase" according to the OTHER side of the pair. The
/// classifier still declines rather than guess where it cannot name a shape — a null on either side means no
/// diagnostic — but that is now the Partial-expression row alone, which no operand can reach while the partial
/// forms do not parse. It used to include the two BOOLEAN rows as well, and declining them was not free: the
/// crash SR6 b) was supposed to prevent shipped instead (kb/Work PB400).</para>
/// </summary>
public static class EvaluateOperandCombinations
{
    // Rows = objects (9), columns = subjects (6), in the printed order. Transcribed from Table 15 and pinned
    // against the spec text by the drift test — do not edit by hand without re-running it.
    private static readonly bool[,] Permitted = BuildFrom(
    //   Ident  Lit    Arith  Bool   Cond   T/F
        "Y      Y      Y      Y      .      .",      // [NOT] identifier
        "Y      .      Y      Y      .      .",      // [NOT] literal
        "Y      Y      Y      .      .      .",      // [NOT] arithmetic-expression
        "Y      Y      .      Y      .      .",      // [NOT] boolean-expression
        "Y      Y      Y      .      .      .",      // [NOT] range-expression
        ".      .      .      .      Y      Y",      // Condition
        "Y      Y      Y      Y      .      .",      // Partial-expression
        ".      .      .      .      Y      Y",      // TRUE or FALSE
        "Y      Y      Y      Y      Y      Y");     // ANY

    /// <summary>True when Table 15 marks this subject/object pairing permissible ('Y'); false for a blank cell,
    /// which §14.9.13.3 SR10 makes a SYNTAX RULE violation — a compile-time diagnostic, never a run-time fault.
    /// </summary>
    public static bool IsPermitted(EvaluateSubjectOperand subject, EvaluateObjectOperand obj) =>
        Permitted[(int)obj, (int)subject];

    /// <summary>Table 15's ROW named as its COLUMN — the same operand kind on the other axis — or null for the
    /// three rows the table prints with no column of their own (range-expression, partial-expression, ANY are
    /// selection-OBJECT forms; §14.9.13.2's general format admits no such selection subject).
    /// <para>⛔ THIS EXISTS SO THE BINDER CAN CLASSIFY A SELECTION SUBJECT AND A SELECTION OBJECT WITH ONE
    /// FUNCTION (kb/Work PB400). Two independently written classifiers is the two-arm-dispatch shape, and it
    /// produced exactly that defect: the object classifier recognised a switch-status condition-name and the
    /// subject classifier did not, so <c>EVALUATE W-ON WHEN TRUE</c> was refused while the level-88 spelling in
    /// the identical position compiled. <c>EvaluateOperandAxisDriftTests</c> pins the mapping against the
    /// printed labels.</para></summary>
    public static EvaluateSubjectOperand? AsSubjectOperand(EvaluateObjectOperand o) => o switch
    {
        EvaluateObjectOperand.Identifier => EvaluateSubjectOperand.Identifier,
        EvaluateObjectOperand.Literal => EvaluateSubjectOperand.Literal,
        EvaluateObjectOperand.ArithmeticExpression => EvaluateSubjectOperand.ArithmeticExpression,
        EvaluateObjectOperand.BooleanExpression => EvaluateSubjectOperand.BooleanExpression,
        EvaluateObjectOperand.Condition => EvaluateSubjectOperand.Condition,
        EvaluateObjectOperand.TrueOrFalse => EvaluateSubjectOperand.TrueOrFalse,
        _ => null,   // RangeExpression / PartialExpression / Any — object-only rows
    };

    /// <summary>The printed row/column labels, so a diagnostic can name the pairing in the standard's own words
    /// rather than in this compiler's enum spelling.</summary>
    public static string Label(EvaluateSubjectOperand s) => s switch
    {
        EvaluateSubjectOperand.Identifier => "an identifier",
        EvaluateSubjectOperand.Literal => "a literal",
        EvaluateSubjectOperand.ArithmeticExpression => "an arithmetic expression",
        EvaluateSubjectOperand.BooleanExpression => "a boolean expression",
        EvaluateSubjectOperand.Condition => "a condition",
        _ => "TRUE or FALSE",
    };

    /// <inheritdoc cref="Label(EvaluateSubjectOperand)"/>
    public static string Label(EvaluateObjectOperand o) => o switch
    {
        EvaluateObjectOperand.Identifier => "an identifier",
        EvaluateObjectOperand.Literal => "a literal",
        EvaluateObjectOperand.ArithmeticExpression => "an arithmetic expression",
        EvaluateObjectOperand.BooleanExpression => "a boolean expression",
        EvaluateObjectOperand.RangeExpression => "a range expression",
        EvaluateObjectOperand.Condition => "a condition",
        EvaluateObjectOperand.PartialExpression => "a partial expression",
        EvaluateObjectOperand.TrueOrFalse => "TRUE or FALSE",
        _ => "ANY",
    };

    private static bool[,] BuildFrom(params string[] rows)
    {
        var t = new bool[rows.Length, 6];
        for (int r = 0; r < rows.Length; r++)
        {
            var cells = rows[r].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int c = 0; c < 6; c++) t[r, c] = cells[c] == "Y";
        }
        return t;
    }
}
