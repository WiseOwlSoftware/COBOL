// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⛔ THE ONE READER OF A QUOTED LITERAL (kb/Work PB2725): the class, format and value of the parser's <c>quotedLiteral</c>
/// node — an alphanumeric literal in either format (§2023:8.3.3.2.1, "Alphanumeric literals are of the class and category
/// alphanumeric"), a boolean literal (§2023:8.3.3.4.1) or a national literal (§2023:8.3.3.5.1). The class and format come
/// from <see cref="CobolLiteral.FormOfToken"/>, the value from <see cref="CobolLiteral.Decode"/>; no reader asks the token
/// kind. Five binders and the compile-time evaluator each mapped STRINGLIT / HEXLIT / NATLIT / BOOLLIT to a class and a
/// decoder by hand, and disagreed on the decoder, before this.
/// </summary>
/// <param name="Class">The literal's class, by its written prefix.</param>
/// <param name="Hexadecimal">True for a hexadecimal format: <c>X"…"</c>, <c>NX"…"</c> or <c>BX"…"</c>.</param>
/// <param name="Written">The literal as written, prefix and delimiters included.</param>
/// <param name="Value">The decoded characters (a boolean literal's are its <c>0</c>/<c>1</c> characters).</param>
public readonly record struct QuotedLiteral(LiteralClass Class, bool Hexadecimal, string Written, string Value)
{
    /// <summary>The literal <paramref name="literal"/> denotes.</summary>
    public static QuotedLiteral Of(CobolParserCore.QuotedLiteralContext literal)
    {
        string written = literal.GetText();
        var (cls, hexadecimal) = CobolLiteral.FormOfToken(written);
        return new(cls, hexadecimal, written, CobolLiteral.Decode(written));
    }

    /// <summary>The quoted literal <paramref name="literal"/> is, or null when it is another alternative (a figurative
    /// constant, a concatenation expression, NULL) or absent.</summary>
    public static QuotedLiteral? In(CobolParserCore.NonNumericLiteralContext? literal) =>
        literal?.quotedLiteral() is { } q ? Of(q) : null;
}
