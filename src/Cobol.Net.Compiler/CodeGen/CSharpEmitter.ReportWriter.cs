// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The Report Writer half of the Roslyn backend (ISO/IEC 1989:2023 §13.14–§13.18 / §14.9.16/.21/.46;
/// COBOLNET_REPORT_WRITER_DESIGN §4): per report an engine instance field (<c>__RPT_n</c>, a
/// <c>CobolReport</c>), constructed in <c>__Activate</c> alongside the file registration; per report LINE one
/// generated COMPOSE method invoked by the engine at presentation time. Every printable item renders through the
/// orchestrator's ONE MOVE conversion (<see cref="CSharpEmitter.ConvertSource"/>) — which IS §13.18.53.4 GR1's
/// "SOURCE specifies the sending operand of an implicit MOVE statement to the printable item" (PIC-governed
/// alignment/editing; the legacy's byte-copy content bugs cannot recur by construction). No byte plans, no
/// registration kinds — the typed-native singular pattern.
/// </summary>
public sealed partial class CSharpEmitter
{
    /// <summary>Emit the per-report class members: the engine field, the NumProfile statics of the numeric
    /// printable items (synthetic items live outside the storage forest, so <c>FieldEmitter.EmitProfiles</c>
    /// never sees them), and the per-line compose methods.</summary>
    private void RwEmitReportMembers(CodeWriter w)
    {
        var reports = _ctx.Data.Reports;
        if (reports.Count == 0) return;
        w.Line();
        foreach (var r in reports)
            w.Line($"private CobolReport __RPT_{r.CsIndex} = null!;   // RD {r.Name} (ISO §13.14) — constructed in __Activate");
        foreach (var r in reports)
            foreach (var (group, gi) in r.Groups.Select((g, i) => (g, i)))
                foreach (var (line, li) in group.Lines.Select((l, i) => (l, i)))
                {
                    // Profiles first (declaration order is irrelevant for statics, but keep them adjacent).
                    foreach (var f in line.Fields)
                        if (f.PrintItem.Pic is { Category: PicCategory.Numeric, IsFloat: false } pic)
                            w.Line($"private static readonly NumProfile {f.PrintItem.ProfileName} = {pic.ProfileInitializer};");
                    RwEmitCompose(r, group, gi, line, li, w);
                }
    }

    /// <summary>Emit one report line's compose method: a space-filled buffer of the report's line width, each
    /// printable item placed at its COLUMN (§13.18.14) with the value the §13.18.53.4 GR1/GR3 implicit MOVE
    /// produces — evaluated when the ENGINE invokes the method, i.e. at presentation time, after LINE-COUNTER
    /// was set to this line's number (§13.18.35.4 GR6).</summary>
    private void RwEmitCompose(ReportModel r, ReportGroupModel group, int gi, ReportLineModel line, int li, CodeWriter w)
    {
        using (w.Block($"private string __RPT_C_{r.CsIndex}_{gi}_{li}()   // {r.Name} {group.Kind} line {li + 1}"))
        {
            w.Line($"var __ln = CobolReport.NewLine({r.LineWidth});");
            foreach (var f in line.Fields)
                w.Line($"CobolReport.Place(__ln, {f.Column}, {RwFieldImage(r, f)});");
            w.Line("return new string(__ln);");
        }
    }

    /// <summary>The C# expression of one printable item's image — the result of the implicit MOVE of its source
    /// into the printable item (ISO §13.18.53.4 GR1; a VALUE item per §13.18.63), through the orchestrator's ONE
    /// MOVE conversion path. The synthetic print item is StoreAsImage for numerics, so <c>ConvertSource</c>
    /// yields the printable CHARACTER image for every category (FormatDisplay / CobolEdit.Format /
    /// CobolString.Store).</summary>
    private string RwFieldImage(ReportModel r, ReportFieldModel f)
    {
        BoundOperand source;
        switch (f.Source)
        {
            case FieldValueSource v:
                source = RwValueOperand(v.Raw);
                break;
            case FieldCounterSource c:
                // SOURCE LINE-COUNTER / PAGE-COUNTER (§8.4.3.15 SR1) — composed at presentation time, AFTER the
                // §13.18.35.4 GR6 counter update, so a PH line's LINE-COUNTER prints the PH's own line number.
                source = new BoundComputedOperand(new BoundReportCounterRef(r, c.IsPage));
                break;
            case FieldSumSource s:
                // The SUM counter is the printable entry's source item (§13.18.54.4 GR4).
                var sum = r.Sums.First(x => x.Id.Equals(s.CounterId, StringComparison.OrdinalIgnoreCase));
                source = new BoundComputedOperand(new BoundReportSumRef(r, sum.Id, sum.Scale));
                break;
            case FieldDataSource d when d.Item is { } item && _refs.ResolveItem(item) is { } place:
                source = new BoundFieldOperand(place);
                break;
            default:
                return LoudValue("string",
                    $"report {r.Name}: SOURCE operand not resolvable to storage (ISO §13.18.53.3 SR4)");
        }
        return ConvertSource(source, f.PrintItem);
    }

    /// <summary>A VALUE clause operand (raw text) as a bound operand: a quoted literal, a figurative word
    /// (ZERO/SPACE/QUOTE/HIGH-VALUE/LOW-VALUE — ISO §8.3.3.6), or a numeric literal.</summary>
    private static BoundOperand RwValueOperand(string raw)
    {
        if (CobolLiteral.IsStringLiteral(raw))   // both ISO §8.3.1.2 delimiters (apostrophe VALUE was silently miscompiled)
            return new BoundStringLiteral(CobolLiteral.Decode(raw));
        if (AllLiteralText(raw) is { } all) return new BoundAllLiteral(all);   // ALL "literal" (§8.3.3.6.4 F6)
        return FigurativeConstants.KindOf(raw) is { } k   // the ONE word-recognition table (P7 Step 4)
            ? new BoundFigurative(k)
            : new BoundNumericLiteral(raw);
    }

    /// <summary>Emit the per-instance report-engine construction (called inside <c>__Activate</c>'s
    /// once-per-instance block, right after <c>EmitFileRegistration</c> — hazard: the report FD must be
    /// registered BEFORE the engine's first write, COBOLNET_REPORT_WRITER_DESIGN §4): the engine with its
    /// §13.18.39.4 geometry, each group with its compose table, the CONTROL get/set delegates (§13.18.16), the
    /// SUM counters (§13.18.54), and the USE BEFORE REPORTING hooks (§14.9.49 Format 2 GR8).</summary>
    private void RwEmitReportConstruction(BoundProgram bound, CodeWriter w)
    {
        var reports = _ctx.Data.Reports;
        if (reports.Count == 0) return;
        foreach (var r in reports)
        {
            if (r.File is null) continue;   // diagnosed at bind (§13.18.46) — compile already failed
            w.Line($"__RPT_{r.CsIndex} = new CobolReport({CsLiteral(r.Name)}, {FileKeyExpr(r.File)}, "
                + $"{r.LineWidth}, {(r.Paged ? "true" : "false")}, {r.PageLimit}, {r.Heading}, {r.FirstDetail}, "
                + $"{r.LastControlHeading}, {r.LastDetail}, {r.Footing});");
            foreach (var (group, gi) in r.Groups.Select((g, i) => (g, i)))
            {
                string lines = group.Lines.Count == 0
                    ? "System.Array.Empty<ReportGroupLine>()"
                    : "new[] { " + string.Join(", ", group.Lines.Select((l, li) =>
                        $"new ReportGroupLine(ReportLineKind.{l.Kind}, {l.Value}, __RPT_C_{r.CsIndex}_{gi}_{li})")) + " }";
                w.Line($"var __rg{r.CsIndex}_{gi} = new ReportGroup(ReportGroupKind.{group.Kind}, "
                    + $"{CsLiteral(group.Name ?? "")}, {group.ControlLevel}, {lines});");
                w.Line($"__RPT_{r.CsIndex}.AddGroup(__rg{r.CsIndex}_{gi});");
                // GROUP INDICATE items (§13.18.29): the engine blanks them on repeated presentations.
                foreach (var ln in group.Lines)
                    foreach (var f in ln.Fields)
                        if (f.GroupIndicate)
                            w.Line($"__rg{r.CsIndex}_{gi}.IndicateFields.Add(({f.Column}, {f.PrintItem.ImageWidth}));");
            }
            // CONTROL hierarchy (§13.18.16), major→minor: get/set image delegates over the typed storage — the
            // CALL boundary's one string-carrier pair (CallStringRead/CallStringWrite), reused verbatim.
            foreach (var ctl in r.Controls)
            {
                if (ctl.IsFinal)
                {
                    w.Line($"__RPT_{r.CsIndex}.AddControl(true, static () => \"\", static __v => {{ }});   // FINAL (§13.18.16.4 GR2 — never breaks)");
                    continue;
                }
                if (ctl.Item is not { } item || _refs.ResolveItem(item) is not { } place
                    || place.Item.Pic is { IsFloat: true } or { Usage: Usage.Index })
                {
                    w.Line(LoudStmt($"report {r.Name}: CONTROL operand '{ctl.Name}' not resolvable to "
                        + "image-carrying storage (ISO §13.18.16.3 SR3)"));
                    continue;
                }
                // The prior-control save/compare/restore key is the item's CHARACTER IMAGE (§13.18.16.4 GR3 —
                // representation-faithful for every category): read via the one string-carrier read; the
                // restore decodes through StoreDisplay for a native numeric leaf (the NumericImagePlace shape),
                // or splices the image for string-carried storage.
                string set = CallPlaceIsString(place)
                    ? CallStringWrite(place, "__v")
                    : place.Write($"CobolNum.StoreDisplay(__v, {place.Item.ProfileName}, {place.Read()})");
                w.Line($"__RPT_{r.CsIndex}.AddControl(false, () => {CallStringRead(place)}, __v => {{ {set} }});");
            }
            // SUM counters (§13.18.54): the addend delegate yields the addends' total at the counter's scale
            // (GR3 — ADD-consistent accumulation; GR9 — multiple addends sum together).
            foreach (var sum in r.Sums)
            {
                var terms = sum.Addends
                    .Select(a => _refs.ResolveItem(a) is { } p
                        ? "(" + NumericRenderer.Align(_num.FieldNum(p), sum.Scale) + ")"
                        : LoudValue("long", $"report {r.Name}: SUM addend not resolvable to storage (ISO §13.18.54.3 SR5)"))
                    .ToList();
                string addend = terms.Count == 0 ? "0L" : string.Join(" + ", terms);
                string upon = sum.UponDetails.Count == 0
                    ? "null"
                    : "new[] { " + string.Join(", ", sum.UponDetails.Select(CsLiteral)) + " }";
                int printedGi = r.Groups.IndexOf(sum.PrintedIn);
                w.Line($"__RPT_{r.CsIndex}.AddSum({CsLiteral(sum.Id)}, () => (long)({addend}), {upon}, "
                    + $"{sum.ResetLevel}, __rg{r.CsIndex}_{printedGi});");
            }
        }
        // USE BEFORE REPORTING hooks (ISO §14.9.49 Format 2 GR8): the engine invokes the declarative's bounded
        // dispatch just before the named group is produced. __RunUse exists whenever declaratives do.
        var decls = bound.Declaratives ?? [];
        for (int i = 0; i < decls.Count; i++)
            if (decls[i].ReportGroup is { } hooked)
                foreach (var r in reports)
                {
                    int gi = r.Groups.IndexOf(hooked);
                    if (gi >= 0)
                        w.Line($"__rg{r.CsIndex}_{gi}.BeforeReporting = () => __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc});");
                }
    }

    // ── Verb emission (ISO §14.9.21 / §14.9.16 / §14.9.46) ───────────────────────────────────────────────────

    /// <summary>INITIATE: one engine call per report, in written order (§14.9.21.4 GR5).</summary>
    private void RwEmitInitiate(BoundInitiate s)
    {
        foreach (var r in s.Reports)
            _ctx.Writer.Line($"__RPT_{r.CsIndex}.Initiate();");
    }

    /// <summary>GENERATE: detail reporting names the detail group; summary reporting (the report-name form,
    /// §14.9.16.4 GR2) passes null.</summary>
    private void RwEmitGenerate(BoundGenerate s)
    {
        if (s.Detail is { } det && det.Name is null)
        {
            // A GENERATE-able detail always has a data-name (§13.16.3 SR7) — unreachable unless the binder let
            // an unnamed group through; loud, never a silent wrong-group generate (§1.4).
            _ctx.Writer.Line(LoudStmt($"GENERATE of an unnamed detail group of report {s.Report.Name}"));
            return;
        }
        _ctx.Writer.Line($"__RPT_{s.Report.CsIndex}.Generate({(s.Detail is { } d ? CsLiteral(d.Name!) : "null")});");
    }

    /// <summary>TERMINATE: one engine call per report, in written order (§14.9.46.4 GR4).</summary>
    private void RwEmitTerminate(BoundTerminate s)
    {
        foreach (var r in s.Reports)
            _ctx.Writer.Line($"__RPT_{r.CsIndex}.Terminate();");
    }
}
