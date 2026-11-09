// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// THE ONE PLACE THE TOKENS OF A COMMENT-ENTRY ARE NAMED (kb/Work PB3845, PB5024). A comment-entry is arbitrary text
/// (<see cref="CobolParserCore.CommentEntryContext"/>; the identification paragraphs AUTHOR, REMARKS and the rest), so the
/// stages that ask a rule of every token of the stream (<see cref="WordCharacterScreen"/>, <see cref="MarginRCutScreen"/>) skip
/// the tokens the parse tree gives to one: its words are not COBOL words, and text cut at margin R in it changes no program.
/// </summary>
internal static class CommentEntryTokens
{
    /// <summary>Adds the stream index of every token <paramref name="entry"/> holds to <paramref name="indexes"/>.</summary>
    public static void AddTo(CobolParserCore.CommentEntryContext entry, HashSet<int> indexes)
    {
        if (entry.Stop is null) return;
        for (int i = entry.Start.TokenIndex; i <= entry.Stop.TokenIndex; i++) indexes.Add(i);
    }

    /// <summary>The stream index of every token a comment-entry of <paramref name="tree"/> holds (the tree may be partial).</summary>
    public static HashSet<int> Of(IParseTree tree)
    {
        var indexes = new HashSet<int>();
        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is CobolParserCore.CommentEntryContext entry) { AddTo(entry, indexes); continue; }
            if (node is ITerminalNode) continue;
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
        return indexes;
    }
}
