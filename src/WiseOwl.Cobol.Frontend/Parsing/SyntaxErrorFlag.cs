// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>Error-presence flag for BOTH recognizers — the parser's token errors and the lexer's character errors — of a
/// reading that only asks whether its text read cleanly (<see cref="FragmentParse"/>, <c>CobolKeywordTokens.LexProbe</c>).
/// The one copy: the fragment parsers each had their own, and the keyword probe a counting twin.</summary>
internal sealed class SyntaxErrorFlag : BaseErrorListener, IAntlrErrorListener<int>
{
    public bool HasError { get; private set; }

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
}
