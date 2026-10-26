// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⛔ THE ONE READING OF A COMPUTE RIGHT SIDE THAT BOTH GENERAL FORMATS PRINT (kb/Work PB51, PB157, PB3830 review R2-1).
///
/// <para><c>computeStatement</c> parses Format 1's <c>arithmeticExpression</c> first and falls to Format 2's
/// <c>booleanExpression</c> only when that fails (ISO §14.9.8.2). A bare figurative <c>ZERO</c> (<c>ZEROS</c>,
/// <c>ZEROES</c>) is the one right side that fails the arithmetic alternative although §8.8.1.1 makes it an arithmetic
/// expression ("An arithmetic expression may be … the figurative constant ZERO (ZEROS, ZEROES)"):
/// <c>ZeroTokenRewriter</c> leaves a ZERO that no operator or parenthesis touches figurative, and only the boolean
/// operand admits the figurative. So the <c>booleanExpression</c> alternative does NOT by itself select Format 2: a
/// sole figurative ZERO is written in BOTH formats, and the receiver's class decides which one the statement is.</para>
///
/// <para>Two readers ask this, and must agree: the binder's Format 2 → Format 1 re-route
/// (<c>ArithmeticBinder.BindComputeBoolean</c>) and §14.5.3.3's phrase association
/// (<see cref="ConditionalPhraseAssociation"/>'s COMPUTE selector), which lets the SIZE ERROR pair associate with a
/// COMPUTE whose right side may be Format 1. When they disagreed, the parse refused <c>COMPUTE X = ZERO ON SIZE ERROR
/// …</c> (COBOL2269) and moved a nested COMPUTE's NOT ON SIZE ERROR to the enclosing statement.</para>
/// </summary>
public static class ComputeFormats
{
    /// <summary>The operand a parsed boolean right side consists of when it is a sole figurative ZERO (<c>ZERO</c> /
    /// <c>ZEROS</c> / <c>ZEROES</c>, and <c>ALL ZERO</c>, whose ALL is the figurative constant's optional word,
    /// §8.3.3.6.2, and which the arithmetic operand's edition gate then reads, kb/Work PB157, PB1415), or null for
    /// anything else, including <c>ALL B"…"</c> (the ALL literal, §8.3.3.6.3) and any boolean operator.</summary>
    public static CobolParserCore.NonNumericLiteralContext? SoleFigurativeZero(CobolParserCore.BooleanExpressionContext? b)
    {
        if (b?.booleanXorTerm() is not [{ } xor]) return null;
        if (xor.booleanAndTerm() is not [{ } and]) return null;
        if (and.booleanShiftTerm() is not [{ } shift]) return null;
        if (shift.booleanShiftSuffix().Length != 0) return null;
        if (shift.booleanFactor() is not { } factor) return null;
        var literal = factor.valueOperand()?.nonNumericLiteral();
        return literal?.figurativeConstant()?.zeroWord() is not null ? literal : null;
    }
}
