// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1626 — <see cref="DefineParameterValue"/>, the ONE rule for what an operating-environment value is as the
/// value of a <c>&gt;&gt;DEFINE … PARAMETER</c> compilation variable (DOC-A.1-49), and <see cref="NumericLiteral.IsFixedPointLiteralForm"/>,
/// the ONE spelling test of a fixed-point numeric literal (ISO §8.3.3.3.2) it asks. The expected forms are GnuCOBOL 3.2's
/// (<c>cobc/ppparse.y</c>), where §7.3.11.4 GR4 leaves the form to the implementor, narrowed by §8.3.3.3.2 where that clause is
/// the standard's own (a decimal point is never the rightmost character).
/// </summary>
public sealed class DefineParameterValueTests
{
    [Theory] // §8.3.3.3.2 1)-3): at least one digit; one sign, leftmost; one decimal point, never rightmost
    [InlineData("7", true)]
    [InlineData("0", true)]
    [InlineData("-2.5", true)]
    [InlineData("+7", true)]
    [InlineData("+.5", true)]
    [InlineData("-.5", true)]
    [InlineData("1234567890123456789012345678901", true)]
    [InlineData("12345678901234567890123456789012345678901234567890", true)]   // the digit COUNT is the evaluator's, not the spelling's
    [InlineData("", false)]
    [InlineData("+", false)]
    [InlineData("-", false)]
    [InlineData(".", false)]
    [InlineData("-.", false)]
    [InlineData("5.", false)]            // 3): the decimal point may appear "except as the rightmost character"
    [InlineData("1.2.3", false)]         // 3): not more than one decimal point
    [InlineData("--5", false)]           // 2): not more than one sign
    [InlineData("5-", false)]            // 2): the sign is the leftmost character
    [InlineData("1,000", false)]
    [InlineData("1E5", false)]
    [InlineData(" 7", false)]
    [InlineData("7 ", false)]
    [InlineData("٣", false)]        // an Arabic-Indic digit is no basic digit
    public void FixedPointLiteralForm_IsThe8_3_3_3_2_Spelling(string text, bool expected)
        => Assert.Equal(expected, NumericLiteral.IsFixedPointLiteralForm(text));

    [Theory] // DOC-A.1-49 (kb/Work PB1626)
    [InlineData("", nameof(ParameterValueForm.NoValue))]
    [InlineData(" ", nameof(ParameterValueForm.NoValue))]
    [InlineData(" 7", nameof(ParameterValueForm.NoValue))]
    [InlineData("7", nameof(ParameterValueForm.NumericLiteral))]
    [InlineData("-2.5", nameof(ParameterValueForm.NumericLiteral))]
    [InlineData("1234567890123456789012345678901", nameof(ParameterValueForm.NumericLiteral))]
    [InlineData("12345678901234567890123456789012", nameof(ParameterValueForm.NumericLiteral))]   // any length; the literal's own limit applies at evaluation
    [InlineData("5.", nameof(ParameterValueForm.Alphanumeric))]
    [InlineData("1,000", nameof(ParameterValueForm.Alphanumeric))]
    [InlineData("1E5", nameof(ParameterValueForm.Alphanumeric))]
    [InlineData("7 ", nameof(ParameterValueForm.Alphanumeric))]
    [InlineData("abc", nameof(ParameterValueForm.Alphanumeric))]
    [InlineData("X\"41\"", nameof(ParameterValueForm.Alphanumeric))]   // a prefix is not a quotation symbol: the text of five characters
    [InlineData("\"abc\"", nameof(ParameterValueForm.QuotedLiteral))]
    [InlineData("'abc'", nameof(ParameterValueForm.QuotedLiteral))]
    [InlineData("\"\"", nameof(ParameterValueForm.QuotedLiteral))]
    [InlineData("\"a\"\"b\"", nameof(ParameterValueForm.QuotedLiteral))]   // §8.3.3.2.3 3): a doubled quotation symbol is content
    [InlineData("\"it's\"", nameof(ParameterValueForm.QuotedLiteral))]     // the other quotation symbol is content too
    [InlineData("\"", nameof(ParameterValueForm.NoValue))]
    [InlineData("\"abc", nameof(ParameterValueForm.NoValue))]
    [InlineData("abc\"", nameof(ParameterValueForm.Alphanumeric))]
    [InlineData("\"abc'", nameof(ParameterValueForm.NoValue))]
    [InlineData("\"\"\"", nameof(ParameterValueForm.NoValue))]             // an opened literal whose last quotation symbol is content
    [InlineData("\"a\"b\"", nameof(ParameterValueForm.NoValue))]
    [InlineData("\"a\" & \"b\"", nameof(ParameterValueForm.NoValue))]
    public void Classify_IsSyntactic(string environmentText, string expected)
        => Assert.Equal(expected, DefineParameterValue.Classify(environmentText).ToString());
}
