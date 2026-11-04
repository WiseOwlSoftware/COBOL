// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>⛔ THE ONE "a parse node AS THE PROGRAMMER WROTE IT" reader, for both assemblies. <c>GetText()</c>
/// concatenates tokens with no separators (<c>LINAGE-COUNTEROFLPF</c>, <c>FUNCTIONSQRT(4)</c>), which is unreadable
/// in a diagnostic about the spelling itself. It lives in the FRONTEND because a frontend rule
/// (<c>ArithmeticFormationRules</c>, run by the compile-time directive evaluator before any compiler pass exists)
/// quotes source too; the compiler's <c>DataBinder.WrittenText</c> — the name its binders call (kb/Work PB399,
/// PB983) — delegates here, so there is one definition.</summary>
public static class WrittenSource
{
    /// <summary>The source extent of <paramref name="ctx"/> as written. A node continued across source lines keeps
    /// ONE space per separator run, and a node with no complete source extent (an error-recovered one) falls back
    /// to its token text rather than throwing inside a diagnostic.</summary>
    public static string Of(Antlr4.Runtime.ParserRuleContext ctx)
    {
        if (ctx.Start is not { StartIndex: >= 0 } start || ctx.Stop is not { } stop || stop.StopIndex < start.StartIndex
            || start.InputStream is null)
            return ctx.GetText();
        string raw = start.InputStream.GetText(new Antlr4.Runtime.Misc.Interval(start.StartIndex, stop.StopIndex));
        return raw.AsSpan().IndexOfAny('\r', '\n') < 0 ? raw
            : System.Text.RegularExpressions.Regex.Replace(raw, @"\s*[\r\n]\s*", " ");
    }

    /// <summary>The source extent of <paramref name="token"/> as written, or its text where it has none (a synthesized
    /// token). <see cref="Antlr4.Runtime.IToken.Text"/> is NOT what the programmer wrote after a <c>&gt;&gt;COBOL-WORDS</c>
    /// retype, which spells the token as its original word (<c>CobolWordsRewriter.Plan.Apply</c>, ISO §7.3.10.4 GR2, GR4):
    /// a diagnostic that names the word asks the source.</summary>
    public static string Of(Antlr4.Runtime.IToken token)
        => token is { StartIndex: >= 0 } && token.StopIndex >= token.StartIndex && token.InputStream is { } input
            ? input.GetText(new Antlr4.Runtime.Misc.Interval(token.StartIndex, token.StopIndex))
            : token.Text;
}
