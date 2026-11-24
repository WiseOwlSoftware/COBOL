// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>How a fixed-form debugging line (indicator <c>D</c>) travels from the §2023:6.5 logical conversion to the
/// lexer: the carrier, then the line's program text. COPY REPLACING and REPLACE match over that text as if the
/// <c>D</c> were absent (the COPY rule of COBOL-85; <c>TextWordScanner</c> skips only this carrier), and the
/// lexer reads the carrier as the hidden <c>DEBUG_LINE</c> marker (<c>CobolLexer.g4</c>, whose spelling <see cref="Text"/>
/// must equal: every <c>DebuggingLineRewriterTests</c> case finds its line by that token) and the line's text as ordinary tokens, which
/// <c>DebuggingLineRewriter</c> then keeps (the unit's SOURCE-COMPUTER says WITH DEBUGGING MODE) or hides as a
/// comment (kb/Work PB1705). The carrier holds U+FDD0, a Unicode NONCHARACTER —
/// reserved for process-internal use and never in interchanged text — so no comment a programmer writes can be
/// mistaken for a debugging line (kb/Work PB1350: the printable <c>*&gt; DEBUG:</c> carrier made a genuine
/// <c>*&gt; DEBUG: …</c> comment take part in matching).
/// <para>The writer (the reference-format stage), the text-manipulation stage and the parser's rewriter all read it,
/// so it lives in the front end's common layer, below each of them (DESIGN-architecture-review §8.1; kb/Work PB2409).</para>
/// </summary>
public static class DebugLineCarrier
{
    /// <summary>The carrier's spelling: the floating comment indicator, U+FDD0, then <c>DEBUG </c>.</summary>
    public static readonly string Text = CobolSourceText.CommentIndicator + (char)0xFDD0 + "DEBUG ";

    /// <summary>Whether <paramref name="text"/> holds the carrier at position <paramref name="at"/>, the one test every
    /// reader of a carried line applies.</summary>
    public static bool StartsAt(string text, int at) => string.CompareOrdinal(text, at, Text, 0, Text.Length) == 0;
}
