// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The ONE reader for a <c>signedIntegerLiteral</c> parse node (kb/Work PB553).
///
/// <para>The grammar rule is <c>(PLUS | MINUS)? INTEGERLIT</c> — the sign is a SEPARATE token, because a
/// DEFAULT-mode <c>(</c> after FROM/TO opens no signed-literal lexer region and the sign-adjacent
/// <c>SIGNED_INTEGERLIT</c> token therefore never reaches these slots. That is superset-parse: it also admits
/// <c>( + 1 )</c>, which the standard does not. ISO §8.3.3.3.2 2) is the rule — <i>"A literal shall not contain
/// more than one sign character. If a sign is used, it shall appear as the leftmost character of the
/// literal."</i> — and a literal is ONE character-string, so a space between the sign and its digits makes the
/// two things two things. Narrowing happens HERE, by name, instead of being left to the ANTLR error
/// reporter.</para>
///
/// <para>⛔ THE VALUE IS NOT WHAT <c>GetText()</c> RETURNS, and the difference is not cosmetic: ANTLR's
/// <c>GetText()</c> concatenates the node's tokens with the whitespace stripped, so <c>+ 1</c> and <c>+1</c>
/// both read back as <c>"+1"</c> and the violation is invisible to any caller that goes straight to the text.
/// Every consumer of the rule comes through this method so that cannot happen once.</para>
/// </summary>
internal static class SignedIntegerLiteral
{
    /// <summary>The literal's text with its sign (e.g. <c>"+1"</c>, <c>"-3"</c>, <c>"12"</c>), after screening
    /// §8.3.3.3.2 2)'s adjacency. On violation the diagnostic is reported and the UNSIGNED digits are returned,
    /// so the caller's own range screens still run on a recovered value rather than cascading.</summary>
    /// <param name="ctx">The <c>signedIntegerLiteral</c> node.</param>
    /// <param name="edition">The diagnostic sink; the caller has already positioned it.</param>
    /// <param name="where">The source-shaped prefix of the message, e.g. <c>data item 'T', Format 2 VALUE</c>.</param>
    public static string Screen(Core.SignedIntegerLiteralContext ctx, EditionContext edition, string where)
    {
        var digits = ctx.INTEGERLIT();
        var sign = (Antlr4.Runtime.Tree.ITerminalNode?)ctx.PLUS() ?? ctx.MINUS();
        if (sign is null) return digits.GetText();
        // Adjacency, measured on the INPUT STREAM and not on GetText(): the sign's last character index and
        // the digits' first must be consecutive. Same-line is implied — a newline is a character too.
        if (sign.Symbol.StopIndex + 1 != digits.Symbol.StartIndex)
        {
            edition.Error(DiagnosticCatalog.SignedLiteralSignNotAdjacent,
                $"{where}: `{sign.GetText()} {digits.GetText()}` — a sign is part of the numeric literal and "
                + "shall be its leftmost character, with no space between the sign and its digits: \"A literal "
                + "shall not contain more than one sign character. If a sign is used, it shall appear as the "
                + $"leftmost character of the literal\" (ISO §8.3.3.3.2 2)). Write `{sign.GetText()}{digits.GetText()}`.");
            return digits.GetText();
        }
        return sign.GetText() + digits.GetText();
    }
}
