// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;

namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>
/// ⛔ WHICH TOKENS OF AN EXPANSION'S TEXT THE CLIENT WROTE (ISO §12.3.8.4 GR5; kb/Work PB4721): the one question every screen of a text
/// asks before it reports a word or a literal, so one fault of the client's is one report, at the place the client wrote it, and never
/// again at the EXPANDS entry. A token of a text the group parsed carries the answer (<see cref="ExpansionToken.ClientWritten"/>); the
/// text of an expansion compiled in an assembly of its own is lexed again from a file, whose tokens carry none, so that compilation
/// is handed the texts of the client's tokens instead (<see cref="OfTexts"/>).
/// </summary>
public sealed class ClientWrittenTokens
{
    private readonly IReadOnlySet<string>? _texts;

    private ClientWrittenTokens(IReadOnlySet<string>? texts) => _texts = texts;

    /// <summary>The answer of every compilation but an expansion's own: only a token that says so (<see cref="ExpansionToken.ClientWritten"/>) is
    /// the client's.</summary>
    public static ClientWrittenTokens FlaggedOnly { get; } = new(null);

    /// <summary>The answer of the compilation of an expansion's own assembly: a token that says so, or one spelled as a token the client
    /// wrote (<paramref name="texts"/>, as the lexer reads them, compared exactly).</summary>
    public static ClientWrittenTokens OfTexts(IReadOnlySet<string> texts) => texts.Count == 0 ? FlaggedOnly : new(texts);

    /// <summary>Whether the client wrote <paramref name="token"/>.</summary>
    public bool Contains(IToken token) =>
        token is ExpansionToken { ClientWritten: true } || (_texts is not null && token.Text is { } text && _texts.Contains(text));
}
