// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>⛔ THE ONE "is this a comment-entry paragraph" question (kb/Work PB6009): the six identification paragraphs whose
/// body is a comment-entry (<c>AUTHOR</c>, <c>INSTALLATION</c>, <c>DATE-WRITTEN</c>, <c>DATE-COMPILED</c>, <c>SECURITY</c>,
/// and the removed <c>REMARKS</c>), each the grammar shape <c>KEYWORD DOT commentEntry? DOT</c> (§1985:VI.3.2.1.2 SR1:
/// "The comment-entry may be any combination of the characters from the computer's character set"). The reference-format
/// conversion discards the entry and writes the paragraph's terminating period itself (<c>AUTHOR. .</c>), so a reader of the
/// paragraph AS WRITTEN (<see cref="WrittenSource"/>) and a reader of where the entries stand (<c>SourceUnitFacts</c>) both ask
/// here, and a seventh such paragraph is one arm.</summary>
public static class CommentEntryParagraph
{
    /// <summary>Whether <paramref name="ctx"/> is a comment-entry paragraph.</summary>
    public static bool Is(ParserRuleContext ctx) => ctx is CobolParserCore.AuthorParagraphContext
        or CobolParserCore.InstallationParagraphContext or CobolParserCore.DateWrittenParagraphContext
        or CobolParserCore.DateCompiledParagraphContext or CobolParserCore.SecurityParagraphContext
        or CobolParserCore.RemarksParagraphContext;

    /// <summary>The separator period that closes the header of the comment-entry paragraph <paramref name="ctx"/> (the
    /// <c>.</c> of <c>AUTHOR.</c>), or null when <paramref name="ctx"/> is not one or the parse recovered without it.</summary>
    public static IToken? HeaderPeriod(ParserRuleContext ctx) =>
        Is(ctx) && ctx.ChildCount > 1 && ctx.GetChild(1) is ITerminalNode { Symbol: { Type: CobolParserCore.DOT } period }
            ? period : null;
}
