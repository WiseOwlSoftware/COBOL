// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Cst;

/// <summary>
/// A source position (1-based line, 0-based column, character length) derived from an ANTLR parse context — the
/// one neutral location carrier the typed <c>Cst/</c> façade exposes so a consumer never reaches into the raw
/// <c>ctx.Start</c>/<c>ctx.Stop</c> tokens (rearchitecture PHASE 04, Group C; DESIGN-frontend-grammar §D6/M8).
/// </summary>
public readonly record struct SourceSpan(int Line, int Column, int Length)
{
    /// <summary>The span covering an entire parser rule context (start of the first token to end of the last).</summary>
    public static SourceSpan Of(Antlr4.Runtime.ParserRuleContext ctx)
        => new(ctx.Start.Line, ctx.Start.Column,
               (ctx.Stop?.StopIndex ?? ctx.Start.StopIndex) - ctx.Start.StartIndex + 1);
}
