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
/// COBOLNET_REPORT_WRITER_DESIGN §4; P7 Step 9f — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): per report an engine instance field (<c>__RPT_n</c>, a <c>CobolReport</c>),
/// constructed in <c>__Activate</c> alongside the file registration; per report LINE one generated COMPOSE
/// method invoked by the engine at presentation time. Every printable item renders through the orchestrator's
/// ONE MOVE conversion (<c>ConvertSource</c>) — which IS §13.18.53.4 GR1's "SOURCE specifies the sending operand
/// of an implicit MOVE statement to the printable item" (PIC-governed alignment/editing; the legacy's byte-copy
/// content bugs cannot recur by construction). No byte plans, no registration kinds — the typed-native singular
/// pattern.
/// </summary>
internal sealed class ReportWriterEmitter(
    EmitContext ctx, NumericRenderer num, ReferenceResolver refs, MoveEmitter move, ConditionRenderer cond)
{
    /// <summary>Emit the per-report class members: the engine field, the NumProfile statics of the numeric
    /// printable items (synthetic items live outside the storage forest, so <c>FieldEmitter.EmitProfiles</c>
    /// never sees them), and the per-line compose methods.</summary>
    public void EmitReportMembers(CodeWriter w)
    {
        var reports = ctx.Data.Reports;
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
                    EmitCompose(r, group, gi, line, li, w);
                }
    }

    /// <summary>Emit one report line's compose method: a space-filled buffer of the report's line width, each
    /// printable item placed at its COLUMN (§13.18.14) with the value the §13.18.53.4 GR1/GR3 implicit MOVE
    /// produces — evaluated when the ENGINE invokes the method, i.e. at presentation time, after LINE-COUNTER
    /// was set to this line's number (§13.18.35.4 GR6). A field-local PRESENT WHEN chain guards its placements
    /// (§13.18.41.4 GR2b — an absent item places nothing and never advances the horizontal counter, GR3f);
    /// a multiple COLUMN entry places once per operand (§13.18.14.4 GR12) with its VARYING counters stepped
    /// per repetition (§13.18.64.4 GR3); a relative (PLUS) operand places against the line's horizontal
    /// counter (§13.18.14.4 GR7/GR8/GR9).</summary>
    private void EmitCompose(ReportModel r, ReportGroupModel group, int gi, ReportLineModel line, int li, CodeWriter w)
    {
        using (w.Block($"private string __RPT_C_{r.CsIndex}_{gi}_{li}()   // {r.Name} {group.Kind} line {li + 1}"))
        {
            w.Line($"var __ln = {RuntimeApi.ReportNewLine(r.LineWidth)};");
            // The horizontal counter (§13.18.14.4 GR7 — the rightmost occupied column, 0 at line start) exists
            // only when some operand is relative; every placed item then updates it (GR9).
            bool needsHc = line.Fields.Any(f => f.Columns.Any(c => c.Relative));
            if (needsHc) w.Line("int __hc = 0;   // the §13.18.14.4 GR7 horizontal counter");
            foreach (var f in line.Fields)
                EmitFieldPlacements(r, f, needsHc, w);
            w.Line("return new string(__ln);");
        }
    }

    /// <summary>Emit one printable entry's placements into the compose body (see <see cref="EmitCompose"/>).
    /// The COBOL-85 shape — one absolute operand, unconditional, no VARYING, in an all-absolute line — keeps
    /// its exact single-statement emission (the characterization-pinned text).</summary>
    private void EmitFieldPlacements(ReportModel r, ReportFieldModel f, bool needsHc, CodeWriter w)
    {
        if (!needsHc && f.Columns.Count == 1 && f.PresentWhen.Count == 0 && f.Varyings.Count == 0)
        {
            w.Line($"{RuntimeApi.ReportPlace("__ln", f.Column, FieldImage(r, f))};");
            return;
        }
        using IDisposable? guard = f.PresentWhen.Count > 0
            ? w.Block($"if ({PresentExpr(f.PresentWhen)})   // PRESENT WHEN (§13.18.41.4 GR2b)")
            : null;
        // VARYING counters (§13.18.64.4 GR3a): the first occurrence takes FROM (default 1). A noninteger
        // FROM/BY truncates through Rescale — the EC-REPORT-VARYING seam (GR5; checking default-off, SSOT §18.16).
        for (int k = 0; k < f.Varyings.Count; k++)
            w.Line($"long {VaryName(f, k)} = {VaryValue(f.Varyings[k].From)};   // VARYING {f.Varyings[k].Name} (§13.18.64.4 GR3a)");
        for (int rep = 0; rep < f.Columns.Count; rep++)
        {
            if (rep > 0)
                for (int k = 0; k < f.Varyings.Count; k++)
                    w.Line($"{VaryName(f, k)} += {VaryValue(f.Varyings[k].By)};   // §13.18.64.4 GR3b");
            var spec = f.Columns[rep];
            string image = FieldImage(r, f);
            if (!spec.Relative)
            {
                w.Line($"{RuntimeApi.ReportPlace("__ln", spec.Value, image)};");
                if (needsHc) w.Line($"__hc = {spec.Value + f.PrintItem.ImageWidth - 1};   // §13.18.14.4 GR9");
            }
            else
            {
                w.Line($"__hc += {spec.Value};   // §13.18.14.4 GR8 — leftmost = horizontal counter + integer-2");
                w.Line($"{RuntimeApi.ReportPlace("__ln", "__hc", image)};");
                w.Line($"__hc += {f.PrintItem.ImageWidth - 1};   // §13.18.14.4 GR9");
            }
        }
    }

    /// <summary>The compose-local name of a VARYING counter (§13.18.64) — keyed by the synthetic print item's
    /// uid + the counter's index within the entry's VARYING clause.</summary>
    private static string VaryName(ReportFieldModel f, int k) => $"__rv{f.PrintItem.Uid}_{k}";

    /// <summary>A VARYING FROM/BY value as a scale-0 C# expression (ISO §13.18.64.4 GR3a/GR3b; absent ⇒ 1).
    /// A noninteger evaluation truncates (GR5's undefined-content case — the EC-REPORT-VARYING seam).</summary>
    private string VaryValue(BoundExpr? e) =>
        e is null ? "1" : NumericRenderer.Align(num.Render(e, ReceiverContext.None), 0);

    /// <summary>A PRESENT WHEN chain as ONE C# boolean expression — the AND of the chain (ISO §13.18.41.4 GR2b:
    /// an absent ancestor absents every subordinate, so presence = every condition true).</summary>
    private string PresentExpr(IReadOnlyList<BoundCondition> conds) =>
        string.Join(" && ", conds.Select(c => $"({cond.Render(c)})"));

    /// <summary>The C# expression of one printable item's image — the result of the implicit MOVE of its source
    /// into the printable item (ISO §13.18.53.4 GR1; a VALUE item per §13.18.63), through the orchestrator's ONE
    /// MOVE conversion path. The synthetic print item is StoreAsImage for numerics, so <c>ConvertSource</c>
    /// yields the printable CHARACTER image for every category (the display format / edit-mask / string-store
    /// renders).</summary>
    private string FieldImage(ReportModel r, ReportFieldModel f)
    {
        BoundOperand source;
        switch (f.Source)
        {
            case FieldValueSource v:
                source = ValueOperand(v.Raw);
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
            case FieldVaryingSource v:
                // The entry's own VARYING counter as the source item (§13.18.64.4 GR4 NOTE) — the compose-local
                // counter, re-read at each repetition's placement.
                source = new BoundComputedOperand(new BoundReportVaryingRef(VaryName(f, v.Index)));
                break;
            case FieldDataSource d when d.Item is { } item && refs.ResolveItem(item) is { } place:
                source = new BoundFieldOperand(place);
                break;
            default:
                return LoudValue("string",
                    $"report {r.Name}: SOURCE operand not resolvable to storage (ISO §13.18.53.3 SR4)");
        }
        return move.ConvertSource(source, f.PrintItem);
    }

    /// <summary>A VALUE clause operand (raw text) as a bound operand: a quoted literal, a figurative word
    /// (ZERO/SPACE/QUOTE/HIGH-VALUE/LOW-VALUE — ISO §8.3.3.6), or a numeric literal.</summary>
    private static BoundOperand ValueOperand(string raw)
    {
        if (CobolLiteral.IsStringLiteral(raw))   // both ISO §8.3.1.2 delimiters (apostrophe VALUE was silently miscompiled)
            return new BoundStringLiteral(CobolLiteral.Decode(raw));
        if (AllLiteralText(raw) is { } all) return new BoundAllLiteral(all);   // ALL "literal" (§8.3.3.6.4 F6)
        return FigurativeConstants.KindOf(raw) is { } k   // the ONE word-recognition table (P7 Step 4)
            ? new BoundFigurative(k)
            : new BoundNumericLiteral(raw);
    }

    /// <summary>Emit the per-instance report-engine construction (called inside <c>__Activate</c>'s
    /// once-per-instance block, right after the file registration — hazard: the report FD must be
    /// registered BEFORE the engine's first write, COBOLNET_REPORT_WRITER_DESIGN §4): the engine with its
    /// §13.18.39.4 geometry, each group with its compose table, the CONTROL get/set delegates (§13.18.16), the
    /// SUM counters (§13.18.54), and the USE BEFORE REPORTING hooks (§14.9.49 Format 2 GR8).</summary>
    public void EmitReportConstruction(BoundProgram bound, CodeWriter w)
    {
        var reports = ctx.Data.Reports;
        if (reports.Count == 0) return;
        foreach (var r in reports)
        {
            if (r.File is null) continue;   // diagnosed at bind (§13.18.46) — compile already failed
            w.Line($"__RPT_{r.CsIndex} = new CobolReport({CsLiteral(r.Name)}, {FileKeyExpr(r.File)}, "
                + $"{r.LineWidth}, {(r.Paged ? "true" : "false")}, {r.PageLimit}, {r.Heading}, {r.FirstDetail}, "
                + $"{r.LastControlHeading}, {r.LastDetail}, {r.Footing});");
            foreach (var (group, gi) in r.Groups.Select((g, i) => (g, i)))
            {
                // A conditioned line carries its PRESENT WHEN chain as a delegate the engine evaluates once per
                // presentation, BEFORE any LINE processing (§13.18.41.4 GR2); unconditional lines keep the
                // three-argument construction (the characterization-pinned text).
                string lines = group.Lines.Count == 0
                    ? "System.Array.Empty<ReportGroupLine>()"
                    : "new[] { " + string.Join(", ", group.Lines.Select((l, li) =>
                        $"new ReportGroupLine(ReportLineKind.{l.Kind}, {l.Value}, __RPT_C_{r.CsIndex}_{gi}_{li}"
                        + (l.PresentWhen.Count > 0 ? $", () => {PresentExpr(l.PresentWhen)}" : "") + ")")) + " }";
                w.Line($"var __rg{r.CsIndex}_{gi} = new ReportGroup(ReportGroupKind.{group.Kind}, "
                    + $"{CsLiteral(group.Name ?? "")}, {group.ControlLevel}, {lines});");
                w.Line($"__RPT_{r.CsIndex}.AddGroup(__rg{r.CsIndex}_{gi});");
                // GROUP INDICATE items (§13.18.29): the engine blanks them on repeated presentations — one span
                // per absolute COLUMN operand (a relative operand with GROUP INDICATE is staged loud at bind).
                foreach (var ln in group.Lines)
                    foreach (var f in ln.Fields)
                        if (f.GroupIndicate)
                            foreach (var spec in f.Columns)
                                if (!spec.Relative)
                                    w.Line($"__rg{r.CsIndex}_{gi}.IndicateFields.Add(({spec.Value}, {f.PrintItem.ImageWidth}));");
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
                if (ctl.Item is not { } item || refs.ResolveItem(item) is not { } place
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
                string set = CallEmitter.CallPlaceIsString(place)
                    ? CallEmitter.CallStringWrite(place, "__v")
                    : PlaceRenderer.Write(place, RuntimeApi.NumStoreDisplay("__v", place.Item.ProfileName, PlaceRenderer.Read(place)));
                w.Line($"__RPT_{r.CsIndex}.AddControl(false, () => {CallEmitter.CallStringRead(place)}, __v => {{ {set} }});");
            }
            // SUM counters (§13.18.54): the addend delegate yields the addends' total at the counter's scale
            // (GR3 — ADD-consistent accumulation; GR9 — multiple addends sum together).
            // ARITHMETIC IS STANDARD / STANDARD-DECIMAL (§8.8.1.5.1 names the SUM clause; P10 Step 12): this
            // native path IS the standard-decimal result, documented rather than routed — each GR3 accumulation
            // is ONE addition of fixed-point values into a fixed-point counter, and an aligned addition of a
            // ≤31-digit counter and a ≤31-digit addend total is ≤32 significant digits, EXACT both in this
            // Int128 accumulation and in a 34-digit SDIDI (§8.8.1.5.2 — an exact ≤34-digit result never
            // rounds), then stored to the counter's own picture identically; the two engines are
            // digit-identical for every reachable SUM shape (report SUM addends are fixed-point by
            // §13.18.54.3, never float).
            foreach (var sum in r.Sums)
            {
                var terms = sum.Addends
                    .Select(a => refs.ResolveItem(a) is { } p
                        ? "(" + NumericRenderer.Align(num.FieldNum(p), sum.Scale) + ")"
                        : LoudValue("long", $"report {r.Name}: SUM addend not resolvable to storage (ISO §13.18.54.3 SR5)"))
                    .ToList();
                string addend = terms.Count == 0 ? "0L" : string.Join(" + ", terms);
                string upon = sum.UponDetails.Count == 0
                    ? "null"
                    : "new[] { " + string.Join(", ", sum.UponDetails.Select(CsLiteral)) + " }";
                int printedGi = r.Groups.IndexOf(sum.PrintedIn);
                // A conditioned SUM entry passes its PRESENT WHEN chain — false at a presentation suppresses
                // the end-of-group reset (§13.18.41.4 GR3g / §13.18.54.4 GR10); the print half rides the
                // printable face's identical chain inside the compose.
                string sumPresent = sum.PresentWhen.Count > 0 ? $", () => {PresentExpr(sum.PresentWhen)}" : "";
                w.Line($"__RPT_{r.CsIndex}.AddSum({CsLiteral(sum.Id)}, () => (long)({addend}), {upon}, "
                    + $"{sum.ResetLevel}, __rg{r.CsIndex}_{printedGi}{sumPresent});");
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
    public void EmitInitiate(BoundInitiate s)
    {
        foreach (var r in s.Reports)
            ctx.Writer.Line($"__RPT_{r.CsIndex}.Initiate();");
    }

    /// <summary>GENERATE: detail reporting names the detail group; summary reporting (the report-name form,
    /// §14.9.16.4 GR2) passes null.</summary>
    public void EmitGenerate(BoundGenerate s)
    {
        if (s.Detail is { } det && det.Name is null)
        {
            // A GENERATE-able detail always has a data-name (§13.16.3 SR7) — unreachable unless the binder let
            // an unnamed group through; loud, never a silent wrong-group generate (§1.4).
            ctx.Writer.Line(LoudStmt($"GENERATE of an unnamed detail group of report {s.Report.Name}"));
            return;
        }
        ctx.Writer.Line($"__RPT_{s.Report.CsIndex}.Generate({(s.Detail is { } d ? CsLiteral(d.Name!) : "null")});");
    }

    /// <summary>TERMINATE: one engine call per report, in written order (§14.9.46.4 GR4).</summary>
    public void EmitTerminate(BoundTerminate s)
    {
        foreach (var r in s.Reports)
            ctx.Writer.Line($"__RPT_{r.CsIndex}.Terminate();");
    }

    /// <summary>SUPPRESS PRINTING (§14.9.45): set the one-shot suppression flag on the engine of the report that
    /// owns the enclosing USE BEFORE REPORTING group (resolved at bind, <see cref="BoundSuppress.Report"/>). The
    /// engine consumes it at the next group presentation (GR2 — current instance only), inhibiting printing,
    /// page advance, NEXT GROUP and LINE-COUNTER changes but NOT the end-of-group sum reset.</summary>
    public void EmitSuppress(BoundSuppress s) =>
        ctx.Writer.Line($"__RPT_{s.Report.CsIndex}.SuppressPrinting();");
}
