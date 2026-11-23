// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>The floating-point literal's digit limits are the edition's (kb/Work PB5613, PB6039): §2002:8.3.1.2.2.2 rules 2 and 3
/// allow a significand of 1 through 31 digits and an exponent of at most three; §2014:8.3.1.2.2.2 rules 2-3 and §2023:8.3.3.3.3
/// SR2-SR3 allow 36 and four. <see cref="DialectBehavior.FloatingLiteralWideLimits"/> is the edition gate (2014).</summary>
public sealed class FloatingLiteralFormLimitsTests
{
    private static string Significand(int digits) => "1." + new string('2', digits - 1);

    [Theory]
    [InlineData(31, "E+99", false, NumericLiteral.FloatingLiteralIssue.None)]
    [InlineData(32, "E+99", false, NumericLiteral.FloatingLiteralIssue.SignificandDigits)]
    [InlineData(36, "E+99", false, NumericLiteral.FloatingLiteralIssue.SignificandDigits)]
    [InlineData(5, "E+999", false, NumericLiteral.FloatingLiteralIssue.None)]
    [InlineData(5, "E+9999", false, NumericLiteral.FloatingLiteralIssue.ExponentDigits)]
    [InlineData(36, "E+99", true, NumericLiteral.FloatingLiteralIssue.None)]
    [InlineData(37, "E+99", true, NumericLiteral.FloatingLiteralIssue.SignificandDigits)]
    [InlineData(5, "E+9999", true, NumericLiteral.FloatingLiteralIssue.None)]
    [InlineData(5, "E+99999", true, NumericLiteral.FloatingLiteralIssue.ExponentDigits)]
    public void Limits_FollowTheEdition(int significandDigits, string exponent, bool wide, NumericLiteral.FloatingLiteralIssue expected) =>
        Assert.Equal(expected, NumericLiteral.CheckFloatingPointForm(Significand(significandDigits) + exponent, wide));

    [Theory]
    [InlineData(85, false)]
    [InlineData(2002, false)]
    [InlineData(2014, true)]
    [InlineData(2023, true)]
    public void WideLimits_BeginAtCobol2014(int edition, bool wide) =>
        Assert.Equal(wide, DialectBehaviors.IsActive(DialectBehavior.FloatingLiteralWideLimits, edition));

    [Theory]
    [InlineData(85, true)]
    [InlineData(2002, true)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    public void TestNumvalCArgumentRules_AreNumvalCsFrom2014(int edition, bool classOnly) =>
        Assert.Equal(!classOnly, DialectBehaviors.IsActive(DialectBehavior.TestNumvalCImportsNumvalCArgumentRules, edition));
}
