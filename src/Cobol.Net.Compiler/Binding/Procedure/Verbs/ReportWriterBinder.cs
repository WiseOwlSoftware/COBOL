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
                        foreach (var v in f.Varyings)
                        {
                            if (v.FromCtx is { } fc) v.From = host.Expr.BindExpr(fc);
                            if (v.ByCtx is { } bc) v.By = host.Expr.BindExpr(bc);
                        }
                    }
                }
            foreach (var s in r.Sums)
                foreach (var c in s.PresentWhenCtxs) s.PresentWhen.Add(Bind(c));
        }
    }
    /// <summary><c>INITIATE report-name…</c> (ISO §14.9.21): each name shall be an RD entry (SR1); a multi-name
    /// statement unrolls in written order (GR5).</summary>
    public BoundStatement BindInitiate(Core.InitiateStatementContext stmt)
    {
        var reports = new List<ReportModel>();
        foreach (var rn in stmt.reportName())
        {
            if (RwFindReport(rn.GetText()) is not { } r)
                return new BoundUnsupported($"INITIATE '{rn.GetText()}' — not a report description entry (ISO §14.9.21 SR1)");
            reports.Add(r);
        }
        return new BoundInitiate(reports);
    }

    /// <summary><c>GENERATE {data-name | report-name}</c> (ISO §14.9.16): a detail report group (SR1 — detail
    /// reporting) or a report-name whose RD has a CONTROL clause (SR2 — summary reporting, GR2).</summary>
    public BoundStatement BindGenerate(Core.GenerateStatementContext stmt)
    {
        string name = stmt.reportName().GetText();
        if (RwFindReport(name) is { } summary)
        {
            if (summary.Controls.Count == 0)
                ctx.Edition.Error(DiagnosticCatalog.ReportGenerateNeedsControl, $"GENERATE {name}: the report-name form requires a CONTROL "
                    + "clause in the report description entry (ISO §14.9.16.3 SR2)");
            return new BoundGenerate(summary, null);   // summary reporting (GR2)
        }
        foreach (var r in ctx.Data.Reports)
            if (r.Groups.FirstOrDefault(g =>
                    name.Equals(g.Name, StringComparison.OrdinalIgnoreCase)) is { } group)
            {
                if (group.Kind != ReportGroupKindModel.Detail)
                    ctx.Edition.Error(DiagnosticCatalog.ReportGenerateNotDetail, $"GENERATE {name}: the named report group is not a "
                        + "DETAIL group (ISO §14.9.16.3 SR1)");
                return new BoundGenerate(r, group);
            }
        return new BoundUnsupported($"GENERATE '{name}' names neither a detail report group nor a report (ISO §14.9.16.3 SR1/SR2)");
    }

    /// <summary><c>TERMINATE report-name…</c> (ISO §14.9.46 SR1/GR4).</summary>
    public BoundStatement BindTerminate(Core.TerminateStatementContext stmt)
    {
        var reports = new List<ReportModel>();
        foreach (var rn in stmt.reportName())
        {
            if (RwFindReport(rn.GetText()) is not { } r)
                return new BoundUnsupported($"TERMINATE '{rn.GetText()}' — not a report description entry (ISO §14.9.46 SR1)");
            reports.Add(r);
        }
        return new BoundTerminate(reports);
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
        var decl = ctx.Table.Declaratives.FirstOrDefault(d =>
            d.ReportGroup is not null && ctx.BindCursor >= d.StartPc && ctx.BindCursor <= d.EndPc);
        if (decl?.ReportGroup is not { } group)
        {
            ctx.Edition.Error(DiagnosticCatalog.ReportSuppressContext,
                "SUPPRESS PRINTING may appear only in a USE BEFORE REPORTING procedure (ISO §14.9.45.3 SR1)");
            return new BoundUnsupported("SUPPRESS outside a USE BEFORE REPORTING procedure (ISO §14.9.45.3 SR1)");
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
