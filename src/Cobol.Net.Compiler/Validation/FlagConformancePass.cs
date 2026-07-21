// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;              // FlagState / PictureAnalyzer / EditionContext
using CobolNet.Binding.Model;        // Usage / PicCategory / PicInfo
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
    // Resolved-model lookups keyed by SOURCE NAME (the flag pass runs before the file-connector renaming, so the
    // model still carries source names — see BinderDriver): the WRITE targets (record- and file-names) whose file
    // has a LINAGE clause (FLAG-14 m), and the report-names whose description carries a VARYING clause (FLAG-02 f).
    private readonly IReadOnlySet<string> _linageWriteTargets;
    private readonly IReadOnlySet<string> _varyingReports;
    // A discard EditionContext so the reused PictureAnalyzer (the ONE picture-category mechanism) can classify a
    // parse-tree PICTURE string WITHOUT re-emitting its bind-time diagnostics to the real sink. At 2023 (the
    // superset) so no legal symbol is spuriously rejected; the picture was already validated during binding.
    private readonly EditionContext _discard = new(2023);

    private FlagConformancePass(FlagState flag, IDiagnosticSink sink,
        IReadOnlySet<string> linageWriteTargets, IReadOnlySet<string> varyingReports)
    {
        _flag = flag;
        _sink = sink;
        _linageWriteTargets = linageWriteTargets;
        _varyingReports = varyingReports;
    }

    /// <summary>Flag every construct an active FLAG option covers. A no-op (no walk) when no FLAG directive is
    /// present — the zero-overhead invariant: a source with no <c>&gt;&gt;FLAG</c> line is byte-identical to a
    /// build without this pass.</summary>
    public static void Run(GroupBindContext group, FlagState flag, IDiagnosticSink sink)
    {
        if (!flag.Any) return;

        // Build the two source-name lookups the statement detectors need (m WRITE-END-OF-PAGE / f
        // TERMINATE-WITH-VARYING). Files/reports live in program units (not OO class data), so units suffice.
        var linage = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var varying = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var unit in group.Units)
        {
            foreach (var file in unit.Data.Files)
                if (file.Linage is not null)
                {
                    linage.Add(file.CobolName);
                    foreach (var rec in file.Records)
                        if (rec.CobolName is { } rn) linage.Add(rn);
                }
            foreach (var report in unit.Data.Reports)
                if (report.Groups.Any(g => g.Lines.Any(l => l.Fields.Any(f => f.Varyings.Count > 0))))
                    varying.Add(report.Name);
        }

        new FlagConformancePass(flag, sink, linage, varying).Visit(group.Tree);
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

    // ── FLAG-14 g NUM-ED-ZERO-FIGCONST (§7.3.15.4 GR4 g) + l VALUE-ZERO (GR4 l) — the SAME predicate stated from
    //    two sides (the use of figurative ZERO in the VALUE clause of a numeric-edited item), so one detector
    //    serves both flags; each fires only if its own option is ON. Anchored at the VALUE clause's source line
    //    (the flaggable syntax). The numeric-edited category is a PICTURE-string property (§13.18.40), decided by
    //    the ONE PictureAnalyzer on the parse-tree picture — FILLER-safe (no name lookup). ──
    public override object? VisitDataDescriptionEntry(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        if (NumericEditedValue(ctx) is { } value && ValueIsFigurativeZero(value))
        {
            int line = value.Start.Line;
            Flag(FlagOption.Flag14NumEdZeroFigconst, line, "the figurative constant ZERO in the VALUE clause of a numeric-edited item");
            Flag(FlagOption.Flag14ValueZero, line, "the figurative constant ZERO in the VALUE clause of a numeric-edited item");
        }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-14 m WRITE-END-OF-PAGE (§7.3.15.4 GR4 m) — a WRITE that ALLOWS an END-OF-PAGE phrase (its file has a
    //    LINAGE clause, §14.9.51) but does not specify it. The "allows EOP" fact is the file's LINAGE, resolved by
    //    name from the model; anchored at the WRITE. ──
    public override object? VisitWriteStatement(CobolParserCore.WriteStatementContext ctx)
    {
        if (ctx.writeAtEndOfPage() is null)
        {
            // recordName (a dataReference — unqualified for a WRITE record in practice; a qualified record name is a
            // rare false-negative for this advisory flag) or the FILE fileName form.
            string? target = ctx.recordName()?.GetText() ?? ctx.fileName()?.GetText();
            if (target is not null && _linageWriteTargets.Contains(target))
                Flag(FlagOption.Flag14WriteEndOfPage, ctx.Start.Line,
                    "the WRITE without an END-OF-PAGE phrase (the file has a LINAGE clause)");
        }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-02 f TERMINATE-WITH-VARYING (§7.3.14.4 GR4 f) — a TERMINATE of a report whose description contains a
    //    VARYING clause (§13.18.64). Flagged once per TERMINATE when any named report carries a VARYING. ──
    public override object? VisitTerminateStatement(CobolParserCore.TerminateStatementContext ctx)
    {
        foreach (var report in ctx.reportName())
            if (_varyingReports.Contains(report.GetText()))
            {
                Flag(FlagOption.Flag02TerminateWithVarying, ctx.Start.Line,
                    "the TERMINATE of a report whose description contains a VARYING clause");
                break;
            }
        return base.VisitChildren(ctx);
    }

    /// <summary>The entry's VALUE clause when the item is numeric-edited (its PICTURE classifies as
    /// <see cref="PicCategory.NumericEdited"/> via the ONE <see cref="PictureAnalyzer"/>), else null. A custom
    /// CURRENCY SIGN symbol is not threaded here — a numeric-edited picture using a non-default currency symbol is
    /// a rare false-negative for this advisory flag, never a false-positive.</summary>
    private CobolParserCore.ValueClauseContext? NumericEditedValue(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        var clauses = ctx.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause();
        if (clauses is null) return null;
        string? picture = null;
        CobolParserCore.ValueClauseContext? value = null;
        foreach (var c in clauses)
        {
            if (c.pictureClause()?.PIC_STRING() is { } ps) picture = ps.GetText();
            if (c.valueClause() is { } vc) value = vc;
        }
        if (picture is null || value is null) return null;
        return PictureAnalyzer.Analyze(picture, Usage.Display, _discard, "a flagged VALUE clause").Category
            == PicCategory.NumericEdited ? value : null;
    }

    /// <summary>Whether a VALUE clause specifies the figurative constant ZERO / ZEROS / ZEROES (with or without
    /// ALL) — the <c>ZERO</c> lexer token covers all three spellings.</summary>
    private static bool ValueIsFigurativeZero(CobolParserCore.ValueClauseContext value)
        => FirstDescendant<CobolParserCore.FigurativeConstantContext>(value)?.ZERO() is not null;

    /// <summary>The first descendant of type <typeparamref name="T"/> in <paramref name="node"/>'s subtree (pre-order),
    /// or null. A small generic walk — the flag detectors reach into a construct's operand subtree without threading
    /// the exact (edition-varying) grammar path.</summary>
    private static T? FirstDescendant<T>(Antlr4.Runtime.Tree.IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T hit) return hit;
            if (FirstDescendant<T>(child) is { } deeper) return deeper;
        }
        return null;
    }
}
