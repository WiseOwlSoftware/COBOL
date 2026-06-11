// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// The Report Writer half of the statement binder (ISO/IEC 1989:2023 §14.9.21 INITIATE / §14.9.16 GENERATE /
/// §14.9.46 TERMINATE; COBOLNET_REPORT_WRITER_DESIGN §5): the three verb binders over the bound
/// <see cref="ReportModel"/>s, the LINE-COUNTER / PAGE-COUNTER reference interception (§8.4.3.15 — the
/// registers are RWCS state, never storage; the <c>BoundLinageCounterRef</c> precedent), and the receiving-side
/// guard that keeps a counter from being SILENTLY dropped by the <c>.OfType&lt;Place&gt;()</c> receiver
/// resolution (the loud-failure doctrine, §1.4).
/// </summary>
public sealed partial class StatementBinder
{
    /// <summary><c>INITIATE report-name…</c> (ISO §14.9.21): each name shall be an RD entry (SR1); a multi-name
    /// statement unrolls in written order (GR5).</summary>
    private BoundStatement RwBindInitiate(Core.InitiateStatementContext ctx)
    {
        var reports = new List<ReportModel>();
        foreach (var rn in ctx.reportName())
        {
            if (RwFindReport(rn.GetText()) is not { } r)
                return new BoundUnsupported($"INITIATE '{rn.GetText()}' — not a report description entry (ISO §14.9.21 SR1)");
            reports.Add(r);
        }
        return new BoundInitiate(reports);
    }

    /// <summary><c>GENERATE {data-name | report-name}</c> (ISO §14.9.16): a detail report group (SR1 — detail
    /// reporting) or a report-name whose RD has a CONTROL clause (SR2 — summary reporting, GR2).</summary>
    private BoundStatement RwBindGenerate(Core.GenerateStatementContext ctx)
    {
        string name = ctx.reportName().GetText();
        if (RwFindReport(name) is { } summary)
        {
            if (summary.Controls.Count == 0)
                data.Edition.Error("COBOLNET0899", $"GENERATE {name}: the report-name form requires a CONTROL "
                    + "clause in the report description entry (ISO §14.9.16.3 SR2)");
            return new BoundGenerate(summary, null);   // summary reporting (GR2)
        }
        foreach (var r in data.Reports)
            if (r.Groups.FirstOrDefault(g =>
                    name.Equals(g.Name, StringComparison.OrdinalIgnoreCase)) is { } group)
            {
                if (group.Kind != ReportGroupKindModel.Detail)
                    data.Edition.Error("COBOLNET0899", $"GENERATE {name}: the named report group is not a "
                        + "DETAIL group (ISO §14.9.16.3 SR1)");
                return new BoundGenerate(r, group);
            }
        return new BoundUnsupported($"GENERATE '{name}' names neither a detail report group nor a report (ISO §14.9.16.3 SR1/SR2)");
    }

    /// <summary><c>TERMINATE report-name…</c> (ISO §14.9.46 SR1/GR4).</summary>
    private BoundStatement RwBindTerminate(Core.TerminateStatementContext ctx)
    {
        var reports = new List<ReportModel>();
        foreach (var rn in ctx.reportName())
        {
            if (RwFindReport(rn.GetText()) is not { } r)
                return new BoundUnsupported($"TERMINATE '{rn.GetText()}' — not a report description entry (ISO §14.9.46 SR1)");
            reports.Add(r);
        }
        return new BoundTerminate(reports);
    }

    private ReportModel? RwFindReport(string name) =>
        data.Reports.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Intercept a LINE-COUNTER / PAGE-COUNTER data reference (ISO §8.4.3.15) ahead of normal name
    /// resolution (the LINAGE-COUNTER idiom in <c>FieldOperand</c>/<c>RefExpr</c>). Returns null when the
    /// reference is NOT a counter; a <see cref="BoundReportCounterRef"/> when it resolves (the OF/IN
    /// <c>cobolWord</c> is the report-name qualifier, SR2/§8.4.2.2 — unqualified resolves only against a sole
    /// report); a <see cref="BoundExprError"/> (with a bind diagnostic) for a counter that cannot resolve.</summary>
    private BoundExpr? RwCounterExpr(Core.DataReferenceContext dref)
    {
        bool isPage = dref.PAGE_COUNTER() is not null;
        if (!isPage && dref.LINE_COUNTER() is null) return null;
        string reg = isPage ? "PAGE-COUNTER" : "LINE-COUNTER";
        if (dref.cobolWord() is { } q)   // qualified: COUNTER OF/IN report-name
        {
            if (RwFindReport(q.GetText()) is { } named) return new BoundReportCounterRef(named, isPage);
            data.Edition.Error("COBOLNET0899", $"{reg} OF '{q.GetText()}': the qualifier shall name a report "
                + "description entry (ISO §8.4.3.15 SR2 / §8.4.2.2)");
            return new BoundExprError($"{reg} reference '{dref.GetText()}'");
        }
        if (data.Reports.Count == 1) return new BoundReportCounterRef(data.Reports[0], isPage);
        data.Edition.Error("COBOLNET0899", data.Reports.Count == 0
            ? $"{reg} referenced, but the program has no report description entry (ISO §8.4.3.15.1 — the "
              + "counters are generated per report)"
            : $"unqualified {reg} with more than one report: qualify by report-name (ISO §8.4.3.15 SR2 / §8.4.2.2)");
        return new BoundExprError($"{reg} reference");
    }

    /// <summary>Resolve a RECEIVING data reference to its <see cref="Place"/> — the ONE receiving-side
    /// chokepoint (MOVE targets, arithmetic resultants, SET receivers). A report counter here is rejected at
    /// bind time: LINE-COUNTER shall not be a receiving operand (ISO §8.4.3.15 SR3 — illegal); PAGE-COUNTER as a
    /// receiver is legal but not yet implemented (staged loud). Without this guard the
    /// <c>.OfType&lt;Place&gt;()</c> receiver pipelines would DROP the counter silently — a silent-miscompile
    /// hazard (§1.4).</summary>
    private Place? ResolveReceiving(Core.DataReferenceContext dref)
    {
        if (dref.LINE_COUNTER() is not null)
        {
            data.Edition.Error("COBOLNET0899",
                "LINE-COUNTER shall not be referenced as a receiving operand (ISO §8.4.3.15.3 SR3)");
            return null;
        }
        if (dref.PAGE_COUNTER() is not null)
        {
            data.Edition.Error("COBOLNET0899", "PAGE-COUNTER as a receiving operand (ISO §8.4.3.15 — legal; the "
                + "program assigns page numbers) is not yet implemented");
            return null;
        }
        return refs.Resolve(dref);
    }
}
