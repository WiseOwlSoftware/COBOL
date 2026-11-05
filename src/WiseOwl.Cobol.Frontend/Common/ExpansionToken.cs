// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>
/// ⛔ A TOKEN OF AN EXPANSION'S TEXT, WHICH NAMES THE EXPANSION IT BELONGS TO (ISO §12.3.8.4 GR5; kb/Work PB4469). An expansion is the
/// skeleton's tokens with each formal replaced by its actual and the header renamed (<c>OoExpansion.SubstitutedTokens</c>); every token
/// that text holds is one of these, copied from the skeleton's, so the parse nodes built over it start at one and the diagnostic cursor
/// (<see cref="CursorOf"/>) reads the <see cref="Region"/> off the token it is positioned at. That is the one place a diagnostic
/// learns it is about an expansion: no pass passes the expansion along, and a pass that replays a captured cursor keeps it.
/// <para>It lives in the frontend, beside the screens that report at a token (<c>LiteralScreen</c>, <c>WordCharacterScreen</c>), so a
/// screen of the source text positions a diagnostic of an expansion's text exactly as the binder's walkers do (kb/Work PB4649): the
/// token-to-cursor rule has ONE definition, <see cref="CursorOf"/>.</para>
/// </summary>
public sealed class ExpansionToken : CommonToken
{
    /// <summary>Copy <paramref name="source"/> as a token of the expansion <paramref name="region"/> stands for.</summary>
    public ExpansionToken(IToken source, DiagnosticRegion region) : base(source) => Region = region;

    /// <summary>The expansion this token is part of.</summary>
    public DiagnosticRegion Region { get; }

    /// <summary>⭐ THE CURSOR <paramref name="token"/> stands at: its line and column, and the <see cref="Region"/> of the expansion it
    /// belongs to when it is a token of one (kb/Work PB4469, PB4649). Unset for null. Every reporter that positions a sink at a token
    /// asks this, so a diagnostic about an expansion's text is attributed the same whichever screen raised it.</summary>
    public static DiagnosticCursor CursorOf(IToken? token) =>
        token is null ? default : new DiagnosticCursor(token.Line, token.Column) { Region = (token as ExpansionToken)?.Region };
}
