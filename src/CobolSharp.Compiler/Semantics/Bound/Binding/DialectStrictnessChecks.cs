// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Centralized checks for non-standard CCVS dialect leniencies (see docs/dialect-strictness.md).
/// The grammar parses a permissive superset; these checks flag the lenient forms under the
/// named-strict dialect modes. Each leniency is accepted in <see cref="DialectMode.Default"/>
/// (permissive) and diagnosed — error, or warning when <see cref="CompilationOptions.WarnNonStandard"/>
/// — under <see cref="DialectMode.StrictCobol85"/> and later.
///
/// This is the single home for the strictness axis: every leniency added to the grammar must be
/// routed through a check here from the moment it is introduced, never left as an unconditional
/// grammar relaxation (which would silently leak into strict-conformance mode).
/// </summary>
internal static class DialectStrictnessChecks
{
    private static SourceLocation MakeLocation(ParserRuleContext ctx) =>
        new("<source>", 0, ctx.Start.Line, ctx.Start.Column);

    private static TextSpan MakeSpan(ParserRuleContext ctx) =>
        new(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex);

    /// <summary>
    /// Leniency L1 — the INVALID KEY / NOT INVALID KEY phrase with the required <c>KEY</c> keyword
    /// omitted (e.g. <c>REWRITE rec INVALID GO TO …</c>). <c>KEY</c> is unbracketed in the ISO
    /// statement formats so it is required; <c>INVALID</c> is a reserved word so dropping <c>KEY</c>
    /// is unambiguous but non-conformant. Detects a missing keyword by comparing the count of direct
    /// <c>INVALID</c> tokens (1 or 2 — the INVALID and the NOT INVALID branch) against <c>KEY</c>
    /// tokens in the same phrase.
    /// </summary>
    internal static void CheckInvalidKeyNoiseWord(BindingContext ctx, ParserRuleContext? phrase)
    {
        if (phrase == null) return;

        int invalidCount = phrase.GetTokens(CobolParserCore.INVALID).Length;
        int keyCount = phrase.GetTokens(CobolParserCore.KEY).Length;
        if (keyCount >= invalidCount) return; // every INVALID had its KEY — conformant

        if (ctx.Options.Dialect >= DialectMode.StrictCobol85)
            ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3611,
                MakeLocation(phrase), MakeSpan(phrase), ctx.Options.DialectName);
        else if (ctx.Options.WarnNonStandard)
            ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3612,
                MakeLocation(phrase), MakeSpan(phrase));
    }

    /// <summary>
    /// Leniency L5 — the SORT/MERGE collating phrase with the required <c>COLLATING</c> keyword omitted
    /// (e.g. <c>MERGE … SEQUENCE alphabet-name</c>). <c>COLLATING</c> is unbracketed in the ISO SORT/MERGE
    /// formats so it is required; <c>SEQUENCE</c> is a reserved word so dropping <c>COLLATING</c> is
    /// unambiguous but non-conformant.
    /// </summary>
    internal static void CheckCollatingNoiseWord(BindingContext ctx, CobolParserCore.SortCollatingPhraseContext? phrase)
    {
        if (phrase == null || phrase.COLLATING() != null) return; // absent phrase or conformant — nothing to flag

        if (ctx.Options.Dialect >= DialectMode.StrictCobol85)
            ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3617,
                MakeLocation(phrase), MakeSpan(phrase), ctx.Options.DialectName);
        else if (ctx.Options.WarnNonStandard)
            ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3618,
                MakeLocation(phrase), MakeSpan(phrase));
    }
}
