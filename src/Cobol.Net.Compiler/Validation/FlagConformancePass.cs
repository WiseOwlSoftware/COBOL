// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;              // FlagState
using CobolNet.Binding.Passes;       // GroupBindContext
using CobolNet.Editions;             // IDiagnosticSink / EditionDiagnostic / EditionSeverity
using CobolNet.Editions.Diagnostics; // DiagnosticCatalog
using CobolNet.Frontend.Generated;   // CobolParserCore / CobolParserCoreBaseVisitor
using CobolNet.Frontend.Preprocessor;// FlagDirective / FlagOption / FlagOptions / FlagDirectiveLine

namespace CobolNet.Validation;

/// <summary>
/// The migration-flagging pass (ISO §7.3.14 FLAG-02 / §7.3.15 FLAG-14) — a sibling to
/// <see cref="VersionConformancePass"/>, run right after it in <c>BinderDriver</c>. It emits a Warning for every
/// construct that an active <c>&gt;&gt;FLAG-02</c>/<c>&gt;&gt;FLAG-14</c> option flags (GR1: the implementor SHALL
/// provide the warning mechanism). It is a SEPARATE pass, not a bolt-on, because flagging is an orthogonal axis to
/// edition gating: it fires on the user's DIRECTIVE STATE (a <c>&gt;&gt;FLAG</c> ON at the construct's line),
/// regardless of <c>--std</c>, and is ALWAYS a Warning (never fails the compile).
///
/// It is a PARSE-TREE visitor (the source-line reason, design D2): the flag fold is line-sensitive (GR2 — a flag
/// applies to the text FOLLOWING its directive), and a bound statement carries no uniform source line, whereas
/// every parse node has <c>ctx.Start.Line</c> — anchored to the same final-text lines as the
/// <see cref="FlagEvent"/>s (<see cref="FlagDirectiveProcessor"/> collects on the FINAL text). It reuses the
/// generated ANTLR base visitor (the same traversal mechanism the <see cref="VersionConformancePass"/> parse arm uses).
/// Syntactic options decide from the parse node directly; options needing a resolved fact look it up by name in the
/// models reachable from <see cref="GroupBindContext"/>. Design SSOT: <c>docs/rearchitecture/DESIGN-flag-directives.md</c>.
/// </summary>
internal sealed class FlagConformancePass : CobolParserCoreBaseVisitor<object?>
{
    private readonly FlagState _flag;
    private readonly IDiagnosticSink _sink;

    private FlagConformancePass(FlagState flag, IDiagnosticSink sink)
    {
        _flag = flag;
        _sink = sink;
    }

    /// <summary>Flag every construct an active FLAG option covers. A no-op (no walk) when no FLAG directive is
    /// present — the zero-overhead invariant: a source with no <c>&gt;&gt;FLAG</c> line is byte-identical to a
    /// build without this pass.</summary>
    public static void Run(GroupBindContext group, FlagState flag, IDiagnosticSink sink)
    {
        if (!flag.Any) return;
        new FlagConformancePass(flag, sink).Visit(group.Tree);
    }

    /// <summary>Emit the option's Warning if it is flagging at <paramref name="line"/>. The Code is per-directive;
    /// the ConstructId (suppress-key), Message, and Citation are per-option (spec-faithful — each names its own GR4
    /// sub-rule + Annex-E item).</summary>
    private void Flag(FlagOption option, int line, string where)
    {
        if (!_flag.IsOnAt(line, option)) return;
        var info = FlagOptions.Info(option);
        bool f02 = info.Directive == FlagDirective.Flag02;
        string code = (f02 ? DiagnosticCatalog.Flag02Warning : DiagnosticCatalog.Flag14Warning).Code;
        string constructId = $"flag-{(f02 ? "02" : "14")}-{info.Word.ToLowerInvariant()}";
        _sink.Report(new EditionDiagnostic(code, EditionSeverity.Warning, constructId,
            $"{info.Change} — flagged by >>{FlagDirectiveLine.DirectiveWord(info.Directive)} {info.Word}",
            where, info.Citation));
    }

    // ── FLAG-14 h READ-PREVIOUS (§7.3.15.4 GR4 h) — a READ … PREVIOUS (sequential or keyed; the parse rule is
    //    common). Purely syntactic. ──
    public override object? VisitReadStatement(CobolParserCore.ReadStatementContext ctx)
    {
        if (ctx.readDirection()?.PREVIOUS() is not null)
            Flag(FlagOption.Flag14ReadPrevious, ctx.Start.Line, "the READ PREVIOUS statement");
        return base.VisitChildren(ctx);
    }

    // ── FLAG-02 c I-O-STATUS-07 (§7.3.14.4 GR4 c) — a CLOSE specifying WITH NO REWIND or the UNIT phrase (NOT
    //    REEL — GR4 c names only UNIT and NO REWIND). Purely syntactic; one flag per CLOSE statement. ──
    public override object? VisitCloseStatement(CobolParserCore.CloseStatementContext ctx)
    {
        foreach (var phrase in ctx.closeFilePhrase())
        {
            var opt = phrase.closeOption();
            if (opt is null) continue;
            if (opt.UNIT() is not null || (opt.NO() is not null && opt.REWIND() is not null))
            {
                Flag(FlagOption.Flag02IoStatus07, ctx.Start.Line, "the CLOSE WITH NO REWIND / UNIT statement");
                break;   // GR4 c flags the CLOSE statement once, however many such phrases it carries
            }
        }
        return base.VisitChildren(ctx);
    }
}
