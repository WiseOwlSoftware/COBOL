// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Cst;

using Core = CobolParserCore;

/// <summary>The small stringly-typed helpers the binder repeats over the highest-churn leaf rules
/// (<c>cobolWord</c>, <c>integerLiteral</c>) — the named replacements for the raw <c>GetText()</c> /
/// <c>int.Parse(GetText())</c> idioms (rearchitecture PHASE 04, Group C).</summary>
public static class CstExtensions
{
    /// <summary>The user-defined-word text of a <c>cobolWord</c> (a data-name, qualifier, index-name, …).</summary>
    public static string Name(this Core.CobolWordContext ctx) => ctx.GetText();

    /// <summary>The value of an <c>integerLiteral</c> as an <see cref="int"/> (throws on a non-integer — the caller
    /// guarantees the grammar shape).</summary>
    public static int AsInt(this Core.IntegerLiteralContext ctx) => int.Parse(ctx.GetText());

    /// <summary>Try-parse an <c>integerLiteral</c> (null-tolerant) to an <see cref="int"/>.</summary>
    public static bool TryAsInt(this Core.IntegerLiteralContext? ctx, out int value) =>
        int.TryParse(ctx?.GetText(), out value);
}
