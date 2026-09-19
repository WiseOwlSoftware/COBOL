// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The Report Writer half of the statement binder (ISO/IEC 1989:2023 §14.9.21 INITIATE / §14.9.16 GENERATE /
/// §14.9.46 TERMINATE; COBOLNET_REPORT_WRITER_DESIGN §5): the three verb binders over the bound
/// <see cref="ReportModel"/>s, the LINE-COUNTER / PAGE-COUNTER reference interception (§8.4.3.15 — the
/// registers are RWCS state, never storage; the <c>BoundLinageCounterRef</c> precedent), and the report-section
/// PRESENT WHEN / VARYING expression binding (§13.18.41/§13.18.64 — parse contexts captured at data bind,
/// bound HERE through the host's ONE condition/expression binders). P7 Step 10f collaborator; the host is
/// consumed only by <see cref="BindReportGroupClauses"/> (the standard (ctx, host) collaborator shape). The
/// receiving-side counter guard (<c>ResolveReceiving</c>) did NOT ride along — it is the shared receiving
/// spine (5 host pipelines consume it) and hoisted to the core, final home <c>ExpressionBinder</c> at 10q.
/// </summary>
internal sealed class ReportWriterBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind every report-section PRESENT WHEN condition (ISO §13.18.41 Format 1) and VARYING FROM/BY
    /// expression (§13.18.64) captured on this unit's report models — once per unit bind, through the ONE
    /// <c>ConditionBinder</c> / expression binder. Each DISTINCT condition context binds exactly once (an
    /// entry's condition appears in every subordinate chain — the memo keeps diagnostics single-shot).</summary>
    public void BindReportGroupClauses()
    {
        var memo = new Dictionary<Core.ConditionContext, BoundCondition>(ReferenceEqualityComparer.Instance);
        BoundCondition Bind(Core.ConditionContext c)
        {
            if (!memo.TryGetValue(c, out var b)) memo[c] = b = host.Cond.BindCondition(c);
            return b;
        }
        foreach (var r in ctx.Data.Reports)
        {
            foreach (var g in r.Groups)
                foreach (var ln in g.Lines)
                {
                    foreach (var c in ln.PresentWhenCtxs) ln.PresentWhen.Add(Bind(c));
                    foreach (var f in ln.Fields)
                    {
                        foreach (var c in f.PresentWhenCtxs) f.PresentWhen.Add(Bind(c));
                        // A SOURCE operand written as arithmetic-expression-1, or as identifier-1 under the
                        // clause's ROUNDED phrase (§13.18.53.3 SR5) — §13.18.53.4 GR2's implicit COMPUTE. The
                        // expression binds HERE through the same BindExpr a procedure-division reference takes
                        // (the kb/Work PB482 argument: a subscript may be an index-name or an expression and has
                        // no value at data bind), and the ROUNDED phrase resolves through the ONE §14.7.4
                        // rounding-mode reader (kb/Work PB852).
                        foreach (var cs in f.Sources.OfType<FieldComputeSource>())
                        {
                            if (cs.Rejected) continue;
                            cs.Value = host.Expr.BindExpr(cs.Ctx);
                            cs.Rounding = host.Expr.RoundingOf(cs.Rounded);
                        }
                        foreach (var v in f.Varyings)
                        {
                            if (v.FromCtx is { } fc) v.From = host.Expr.BindIndexWindowExpr(fc);   // RW VARYING (kb/Work R29 — lenient window)
                            if (v.ByCtx is { } bc) v.By = host.Expr.BindIndexWindowExpr(bc);
                        }
                    }
                }
            foreach (var s in r.Sums)
            {
                foreach (var c in s.PresentWhenCtxs) s.PresentWhen.Add(Bind(c));
                // SUM addends (§13.18.54.3 SR5 — kb/Work PB482). An addend written as identifier-1 is an
                // ORDINARY IDENTIFIER (§8.4.3.1.2 Format 2, qualified-data-name-with-subscripts), so its VALUE
                // is bound HERE, through the same `BindExpr` a procedure-division reference takes — which is
                // what makes `SUM WS-CELL(2)`, `SUM WS-CELL(IX)` and `SUM WS-CELL(IX + 1)` resolve at all: a
                // subscript may be an arithmetic expression or an index-name, and neither has a value at data
                // bind. A REJECTED addend is skipped: its rule has already been named, and re-binding it would
                // report the same words twice under a second clause.
                foreach (var t in s.Terms)
                    foreach (var a in t.Addends)
                        if (!a.Rejected) a.Value = host.Expr.BindExpr(a.Ctx);
                // The SUM clause's own ROUNDED phrase (§13.18.54.2's trailing rounded-phrase) — §13.18.54.4 GR4
                // computes the counter's delivery to the printable item "according to the general rules for the
                // COMPUTE statement with the ROUNDED phrase". Same §14.7.4 reader as SOURCE's (kb/Work PB852).
                s.Rounding = host.Expr.RoundingOf(s.Rounded);
            }
        }
    }
    /// <summary><c>INITIATE report-name…</c> (ISO §14.9.21): each name shall be an RD entry (SR1); a multi-name
    /// statement IS a separate INITIATE statement per report-name in written order (§14.9.21.4 GR5) — one
    /// <see cref="BoundInitiate"/> per name inside a <see cref="BoundImplicitSeries"/>, so GR5's second sentence
    /// ("processing resumes at the next implicit INITIATE statement, if any") has a boundary to land on.</summary>
    public BoundStatement BindInitiate(Core.InitiateStatementContext stmt)
    {
        var members = new List<BoundStatement>();
        foreach (var rn in stmt.reportName())
        {
            if (RwFindReport(rn.GetText()) is not { } r)
                return new BoundUnsupported($"INITIATE '{rn.GetText()}' — not a report description entry (ISO §14.9.21 SR1)");
            members.Add(new BoundInitiate([r]));
        }
        return BoundImplicitSeries.Of(members);
    }

    /// <summary><c>GENERATE {data-name | report-name}</c> (ISO §14.9.16): a detail report group (SR1 — detail
    /// reporting) or a report-name whose RD has a CONTROL clause (SR2 — summary reporting, GR2).</summary>
    public BoundStatement BindGenerate(Core.GenerateStatementContext stmt)
    {
        var (name, qualifier) = ReportGroupResolution.Parts(stmt.reportGroupReference());
        // The report-name form (SR2, summary reporting) has NO qualifier — §8.4.2.2 gives a report-name no
        // qualifier at all — so a qualified operand can only be the data-name form and skips this arm.
        if (qualifier is null && RwFindReport(name) is { } summary)
        {
            if (summary.Controls.Count == 0)
                ctx.Edition.Error(DiagnosticCatalog.ReportGenerateNeedsControl, $"GENERATE {name}: the report-name form requires a CONTROL "
                    + "clause in the report description entry (ISO §14.9.16.3 SR2)");
            return new BoundGenerate(summary, null);   // summary reporting (GR2)
        }
        // SR1's data-name form, through the ONE funnel: it owns the qualified spelling ("It may be qualified by
        // a report-name") AND the §8.4.2.2.3 SR1 ambiguity this loop used to resolve by writing order (PB365).
        string where = $"GENERATE {name}{(qualifier is null ? "" : $" OF {qualifier}")}";
        if (ReportGroupResolution.Resolve(ctx.Edition, ctx.Data.Reports, name, qualifier, where,
                out var report, out var group) != ReportGroupResolution.Match.None)
        {
            if (group!.Kind != ReportGroupKindModel.Detail)
                ctx.Edition.Error(DiagnosticCatalog.ReportGenerateNotDetail, $"{where}: the named report group is not a "
                    + "DETAIL group (ISO §14.9.16.3 SR1)");
            return new BoundGenerate(report!, group);
        }
        return new BoundUnsupported($"GENERATE '{name}' names neither a detail report group nor a report (ISO §14.9.16.3 SR1/SR2)");
    }

    /// <summary><c>TERMINATE report-name…</c> (ISO §14.9.46 SR1; §14.9.46.4 GR4) — one <see cref="BoundTerminate"/> per
    /// report-name inside a <see cref="BoundImplicitSeries"/>, GR4's "as though a separate TERMINATE statement had
    /// been executed for each report-name-1" and its per-implicit-statement resumption point.</summary>
    public BoundStatement BindTerminate(Core.TerminateStatementContext stmt)
    {
        var members = new List<BoundStatement>();
        foreach (var rn in stmt.reportName())
        {
            if (RwFindReport(rn.GetText()) is not { } r)
                return new BoundUnsupported($"TERMINATE '{rn.GetText()}' — not a report description entry (ISO §14.9.46 SR1)");
            members.Add(new BoundTerminate([r]));
        }
        return BoundImplicitSeries.Of(members);
    }

    /// <summary><c>SUPPRESS PRINTING</c> (ISO §14.9.45): inhibit the current instance's printing of the report
    /// group named by the USE BEFORE REPORTING procedure in which this SUPPRESS lexically appears. §14.9.45.4 GR1
    /// makes that group a STATIC property of the enclosing declarative, so it is resolved HERE at bind time: the
    /// declarative whose pc range covers the current bind cursor (a Format-2 <c>ReportGroup</c> scope) fixes the
    /// group, and its owning report drives the emitted engine call. §14.9.45.3 SR1 — a SUPPRESS outside any USE
    /// BEFORE REPORTING procedure has no group to inhibit and is rejected (the per-instance suppression itself is
    /// the runtime half, GR2 — the report engine's one-shot flag).</summary>
    public BoundStatement BindSuppress(Core.SuppressStatementContext stmt)
    {
        // The containing declarative comes from the ONE bind-position probe (kb/Work PB403) — SUPPRESS's
        // §14.9.45.3 SR1 is a placement rule like §14.9.14.3 SR2 and §14.9.33.3 SR1/SR2, and each of them used to
        // walk ctx.Table.Declaratives itself. Ranges are disjoint, so "the declarative containing the cursor,
        // if it is a USE BEFORE REPORTING one" is the same set as the old "first declarative that has a report
        // group AND contains the cursor".
        if (ctx.Enclosing.Declarative?.ReportGroup is not { } group)
        {
            ctx.Edition.Error(DiagnosticCatalog.ReportSuppressContext,
                "SUPPRESS PRINTING may appear only in a USE BEFORE REPORTING procedure (ISO §14.9.45.3 SR1)");
            return new BoundNop();   // reported above — not a deferral (kb/Work PB236)
        }
        var report = ctx.Data.Reports.First(r => r.Groups.Contains(group));
        return new BoundSuppress(report);
    }

    private ReportModel? RwFindReport(string name) =>
        ctx.Data.Reports.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Intercept a LINE-COUNTER / PAGE-COUNTER data reference (ISO §8.4.3.15) ahead of normal name
    /// resolution (the LINAGE-COUNTER idiom in <c>FieldOperand</c>/<c>RefExpr</c>). Returns null when the
    /// reference is NOT a counter; a <see cref="BoundReportCounterRef"/> when it resolves (the OF/IN
    /// <c>cobolWord</c> is the report-name qualifier, SR2/§8.4.2.2 — unqualified resolves only against a sole
    /// report); a <see cref="BoundExprError"/> (with a bind diagnostic) for a counter that cannot resolve.</summary>
    public BoundExpr? CounterExpr(Core.DataReferenceContext dref)
    {
        bool isPage = dref.PAGE_COUNTER() is not null;
        if (!isPage && dref.LINE_COUNTER() is null) return null;
        string reg = isPage ? "PAGE-COUNTER" : "LINE-COUNTER";
        if (dref.cobolWord() is { } q)   // qualified: COUNTER OF/IN report-name
        {
            if (RwFindReport(q.GetText()) is { } named) return new BoundReportCounterRef(named, isPage);
            ctx.Edition.Error(DiagnosticCatalog.ReportCounterQualifierNotReport, $"{reg} OF '{q.GetText()}': the qualifier shall name a report "
                + "description entry (ISO §8.4.3.15 SR2 / §8.4.2.2)");
            return new BoundExprError($"{reg} reference '{dref.GetText()}'");
        }
        if (ctx.Data.Reports.Count == 1) return new BoundReportCounterRef(ctx.Data.Reports[0], isPage);
        ctx.Edition.Error(DiagnosticCatalog.ReportCounterNoReport, ctx.Data.Reports.Count == 0
            ? $"{reg} referenced, but the program has no report description entry (ISO §8.4.3.15.1 — the "
              + "counters are generated per report)"
            : $"unqualified {reg} with more than one report: qualify by report-name (ISO §8.4.3.15 SR2 / §8.4.2.2)");
        return new BoundExprError($"{reg} reference");
    }
}
