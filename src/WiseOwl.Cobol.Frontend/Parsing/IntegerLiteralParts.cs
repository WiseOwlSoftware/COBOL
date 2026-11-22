// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>⛔ THE READER of an <c>integer-n</c> literal's parts (kb/Work PB6021). The <c>integerLiteral</c> rule parses a sign
/// written as the literal's leftmost character only so that the one integer-n screen (<c>Validation/IntegerOperandPass</c>)
/// refuses it by its rule, §2023:5.5 SR1 / §1985:III.2 p142. An extension, not a partial of the generated context, so the
/// context stays the generated type the architecture census records.</summary>
public static class IntegerLiteralParts
{
    /// <summary>THE VALUE TEXT of an <c>integer-n</c>: its digits, without the sign. Every reader of the operand's VALUE reads it
    /// here, so a refused <c>-3</c> is bound as 3 and draws no second diagnostic from a range rule downstream. <c>GetText()</c> of
    /// the node is never a value (<c>SignedIntegerSlotDriftTests.IntegerLiteral_IsReadByItsDigitsOnly</c>), and a message quotes
    /// the operand as written through <c>WrittenSource.Of</c>.</summary>
    public static string Digits(this CobolParserCore.IntegerLiteralContext literal) => literal.INTEGERLIT().GetText();

    /// <summary>The sign written as the literal's leftmost character, or null for the unsigned literal every
    /// <c>integer-n</c> shall be.</summary>
    public static Antlr4.Runtime.Tree.ITerminalNode? Sign(this CobolParserCore.IntegerLiteralContext literal) =>
        literal.PLUS() ?? literal.MINUS();
}
