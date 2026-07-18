// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <see cref="CobolDynString"/> receiving-store rule for a DYNAMIC LENGTH elementary item (ISO §8.5.1.10.4 /
/// §13.18.19, COBOL-2014; PHASE-12 wave 2): the sender REPLACES the content and the new length is the sending
/// length, TRUNCATED ON THE RIGHT to the LIMIT with NO padding (the minimum length is zero, §13.18.19.4 GR1 — the
/// difference from <see cref="CobolString.Store"/>, which space-pads to a fixed width). A limit below zero means
/// "no explicit LIMIT phrase" — the implementor-defined maximum (§13.18.19.4 GR2), here unbounded.
/// </summary>
public sealed class CobolDynStringTests
{
    /// <summary>A sender shorter than the LIMIT is stored as-is — its length becomes the sending length (no pad).</summary>
    [Theory]
    [InlineData("HI", 20, "HI")]
    [InlineData("HELLO", 20, "HELLO")]
    [InlineData("EXACTLY-TEN", 11, "EXACTLY-TEN")]   // exact fit at the limit
    public void Store_ShorterThanLimit_KeepsSendingLength(string value, int limit, string expected)
        => Assert.Equal(expected, CobolDynString.Store(value, limit));

    /// <summary>A sender longer than the LIMIT is truncated ON THE RIGHT to the limit (§8.5.1.10.4).</summary>
    [Theory]
    [InlineData("ABCDEFGHIJ", 5, "ABCDE")]
    [InlineData("TRUNCATED-BEYOND-LIMIT", 9, "TRUNCATED")]
    public void Store_LongerThanLimit_TruncatesRight(string value, int limit, string expected)
        => Assert.Equal(expected, CobolDynString.Store(value, limit));

    /// <summary>The minimum length is zero: an empty or null sender yields length 0 (§13.18.19.4 GR1).</summary>
    [Theory]
    [InlineData("", 20)]
    [InlineData(null, 20)]
    [InlineData("", 0)]
    public void Store_EmptyOrNull_YieldsLengthZero(string? value, int limit)
        => Assert.Equal("", CobolDynString.Store(value, limit));

    /// <summary>A negative limit means "no explicit LIMIT" — the implementor-defined maximum (unbounded here), so
    /// the sender is stored in full (§13.18.19.4 GR2).</summary>
    [Theory]
    [InlineData("A VERY LONG UNBOUNDED STRING VALUE", -1)]
    [InlineData("SEED", -1)]
    public void Store_NoLimit_StoresInFull(string value, int limit)
        => Assert.Equal(value, CobolDynString.Store(value, limit));
}
