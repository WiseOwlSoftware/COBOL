// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>The ONE literal codec (rearchitecture PHASE 05, Step 1): ISO/IEC 1989:2023 §8.3.3.1 — the
/// quotation-mark and apostrophe delimiter forms are EQUAL-STANDING, and a doubled OPENING delimiter is one
/// embedded delimiter. Locks the apostrophe-delimited handling the retired hard-coded <c>'"'</c>-only guards
/// silently miscompiled.</summary>
public sealed class CobolLiteralTests
{
    [Theory]
    // Both delimiters decode; a doubled opening delimiter collapses to one embedded delimiter.
    [InlineData("\"AB\"", "AB")]
    [InlineData("'AB'", "AB")]
    [InlineData("'AB''C'", "AB'C")]        // apostrophe form with an embedded apostrophe
    [InlineData("\"AB\"\"C\"", "AB\"C")]  // quote form with an embedded quote
    [InlineData("N'XY'", "XY")]            // national, apostrophe form (prefix stripped)
    [InlineData("B'01'", "01")]            // boolean, apostrophe form
    [InlineData("N\"XY\"", "XY")]         // national, quote form
    [InlineData("42", "42")]               // not a string literal → returned unchanged
    public void Decode_BothDelimiters(string raw, string expected) => Assert.Equal(expected, CobolLiteral.Decode(raw));

    [Theory]
    [InlineData("'x'", true)]
    [InlineData("\"x\"", true)]
    [InlineData("N'x'", true)]
    [InlineData("B\"0\"", true)]
    [InlineData("x", false)]               // bare word
    [InlineData("42", false)]              // numeric
    [InlineData("'x", false)]              // unterminated
    [InlineData("\"x'", false)]           // mismatched delimiters
    public void IsStringLiteral_BothDelimiters(string raw, bool expected) =>
        Assert.Equal(expected, CobolLiteral.IsStringLiteral(raw));

    [Theory]
    [InlineData("ALL 'x'", "x")]
    [InlineData("ALL \"x\"", "x")]
    [InlineData("ALL'ab'", "ab")]          // tolerant of the missing space
    [InlineData("ALL ZEROS", null)]        // a figurative word, not a quoted literal
    [InlineData("'x'", null)]              // not the ALL form
    public void AllLiteralText_BothDelimiters(string raw, string? expected) =>
        Assert.Equal(expected, CobolLiteral.AllLiteralText(raw));
}
