// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// Is a token the program's COBOL text? The one answer every reader of the whole token stream asks before it judges a token
/// by a rule of the COBOL text (the separator, literal and PICTURE screens; the word-character screen reads only the
/// default channel, which holds neither channel below): NOT a token on a channel a
/// rewriter moved text to because it is not the program's COBOL —
/// <list type="bullet">
/// <item><c>ABSENT_DEBUG_LINE</c>: a fixed-form debugging line whose source unit has no WITH DEBUGGING MODE, a comment
/// (<see cref="DebuggingLineRewriter"/>, kb/Work PB1705);</item>
/// <item><c>EMBEDDED_BLOCK_TEXT</c>: the text of an embedded precompiler block, another language
/// (<see cref="EmbeddedBlockReading"/>, kb/Work PB4341). Its period in <c>T.A</c> is no COBOL period, and its comma in
/// <c>(A,B)</c> no COBOL comma.</item>
/// </list>
/// </summary>
public static class ProgramText
{
    /// <summary>Is <paramref name="token"/> on a channel of the program's COBOL text?</summary>
    public static bool Holds(IToken token)
        => token.Channel is not (CobolLexer.ABSENT_DEBUG_LINE or CobolLexer.EMBEDDED_BLOCK_TEXT);
}
