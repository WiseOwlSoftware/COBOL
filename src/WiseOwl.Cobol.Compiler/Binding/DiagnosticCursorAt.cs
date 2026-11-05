// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Binding;

/// <summary>The parse-context forms of the diagnostic-cursor positioning (kb/Work PB82) — the walkers write
/// <c>using var _ = edition.At(ctx);</c> and every diagnostic reported inside carries that construct's source
/// position. The layer-neutral primitive is <see cref="DiagnosticCursorExtensions.At(IDiagnosticSink, int, int)"/>;
/// these only read the ANTLR token.</summary>
internal static class DiagnosticCursorAt
{
    /// <summary>Position at <paramref name="ctx"/>'s first token (null ⇒ no change of position, an empty scope).</summary>
    public static DiagnosticCursorScope At(this IDiagnosticSink sink, ParserRuleContext? ctx) => sink.At(ctx?.Start);

    /// <summary>Position at <paramref name="token"/> (null ⇒ no change of position, an empty scope).</summary>
    public static DiagnosticCursorScope At(this IDiagnosticSink sink, IToken? token) =>
        sink.At(ExpansionToken.CursorOf(token));   // the one token-to-cursor rule, the expansion's region included (kb/Work PB4469, PB4649)

    /// <summary>Position at a bound data item's declaring entry (<see cref="Model.DataItem.DeclaredAt"/>) — the
    /// post-build model passes' form.</summary>
    public static DiagnosticCursorScope At(this IDiagnosticSink sink, Model.DataItem item) => sink.At(item.DeclaredAt);
}
