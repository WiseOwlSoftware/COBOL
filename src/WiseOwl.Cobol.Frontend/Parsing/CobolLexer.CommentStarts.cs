// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Generated;

/// <summary>The lexer's record of where each inline comment begins (DESIGN-listing.md section 7.2; kb/Work PB5949). The lexer
/// SKIPS a comment (COMMENT_START, PIC_COMMENT), so no token says where one stands; the source listing colours a comment from this
/// record, which the lexer's own rule writes, never from a pattern over the text. A partial of the generated lexer, beside its
/// rules, because generated code carries no nullable context.</summary>
public partial class CobolLexer
{
    /// <summary>Where each inline comment the lexer met begins: its 1-based resultant line and 0-based column. Null, the default,
    /// records nothing, so a compilation without a listing does no work for it (DESIGN-listing.md section 9).</summary>
    internal List<(int Line, int Column)>? CommentStarts { get; set; }

    /// <summary>The action of the comment rules: record the comment's start when a record is kept.</summary>
    private void OnCommentStart() => CommentStarts?.Add((TokenStartLine, TokenStartColumn));
}
