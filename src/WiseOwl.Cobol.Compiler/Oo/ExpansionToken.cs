// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Compiler.Oo;

/// <summary>
/// ⛔ A TOKEN OF AN EXPANSION'S TEXT, WHICH NAMES THE EXPANSION IT BELONGS TO (ISO §12.3.8.4 GR5; kb/Work PB4469). An expansion is the
/// skeleton's tokens with each formal replaced by its actual and the header renamed (<c>OoExpansion.SubstitutedTokens</c>); every token
/// that text holds is one of these, copied from the skeleton's, so the parse nodes built over it start at one and the diagnostic cursor
/// (<c>DiagnosticCursorAt.At</c>) reads the <see cref="Region"/> off the node it is positioned at. That is the one place a diagnostic
/// learns it is about an expansion: no binder pass passes the expansion along, and a pass that replays a captured cursor keeps it.
/// </summary>
internal sealed class ExpansionToken : CommonToken
{
    /// <summary>Copy <paramref name="source"/> as a token of the expansion <paramref name="region"/> stands for.</summary>
    public ExpansionToken(IToken source, DiagnosticRegion region) : base(source) => Region = region;

    /// <summary>The expansion this token is part of.</summary>
    public DiagnosticRegion Region { get; }
}
