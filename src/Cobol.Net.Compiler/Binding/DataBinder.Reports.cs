// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  REPORT SECTION binding (ISO/IEC 1989:2023 §13.6 report section / §13.14 report description / §13.15 report
//  group description; COBOLNET_REPORT_WRITER_DESIGN §3). Each RD becomes a ReportModel — page geometry with the
//  §13.18.39.4 GR3 defaults applied, the CONTROL hierarchy, and the report groups as LINE-clause-built line
//  lists of printable fields. A printable item is carried as a SYNTHETIC DataItem (PicInfo + flags, never added
//  to the storage forest — report groups are not data storage): the emitter then renders each SOURCE/VALUE
//  through the ONE MOVE conversion path (CSharpEmitter.ConvertSource), which IS §13.18.53.4 GR1's implicit MOVE.
//  Legal-but-unimplemented clauses stage LOUD here (Edition.Error, COBOLNET0899) — never silently dropped (§1.4).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>One report description entry (ISO §13.14): identity, owning file (resolved post-build from the FD's
/// REPORT clause, §13.18.46), the §13.18.39.4 page regions (GR3 defaults applied), the §13.18.16 control
/// hierarchy (major→minor; FINAL first when present), the report groups, and the SUM counters.</summary>
public sealed class ReportModel
{
    public required string Name { get; init; }

    /// <summary>The report file whose FD names this report in a REPORT(S) clause (ISO §13.18.46), resolved
    /// post-build; a report named by NO file description entry is a bind error.</summary>
    public FileModel? File { get; set; }

    /// <summary>True when the RD has a PAGE clause (§13.18.39.4 GR2a — absent ⇒ one page of indefinite length;
    /// the page-fit/advance machinery is then inert).</summary>
    public bool Paged { get; set; }

    // The §13.18.39.4 GR2 page regions (GR3 defaults applied by the binder; meaningful only when Paged).
    public int PageLimit { get; set; }
    public int Heading { get; set; }
    public int FirstDetail { get; set; }
    public int LastControlHeading { get; set; }
    public int LastDetail { get; set; }
    public int Footing { get; set; }

    /// <summary>The report line width: the FD's fixed RECORD CONTAINS when present, else the widest field extent
    /// (column + image width − 1) over the report (the §13.18.39.4 GR5 page-width default 999 is a MAXIMUM, not
    /// a record length). Computed post-build.</summary>
    public int LineWidth { get; set; } = 1;

    /// <summary>The CONTROL hierarchy in major→minor order (ISO §13.18.16.4 GR1; FINAL, if present, first — GR2).</summary>
    public List<ReportControlModel> Controls { get; } = [];

    /// <summary>The report groups in declaration order.</summary>
    public List<ReportGroupModel> Groups { get; } = [];

    /// <summary>The SUM counters of this report (ISO §13.18.54), keyed by counter id.</summary>
    public List<ReportSumModel> Sums { get; } = [];

    /// <summary>This report's index within its program unit — backs the emitted engine field name
    /// (<c>__RPT_{CsIndex}</c>).</summary>
    public int CsIndex { get; set; }
}

/// <summary>One CONTROL clause operand (ISO §13.18.16): FINAL or a (possibly qualified) data-name resolved
/// post-build to its item.</summary>
public sealed class ReportControlModel
{
    public bool IsFinal { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<string> Qualifiers { get; init; } = [];
    public DataItem? Item { get; set; }
}

/// <summary>A report group's TYPE (ISO §13.18.57 Format 2).</summary>
public enum ReportGroupKindModel { ReportHeading, PageHeading, ControlHeading, Detail, ControlFooting, PageFooting, ReportFooting }

/// <summary>One report group description (ISO §13.15): its 01-level name (a detail's GENERATE handle,
/// §14.9.16 SR1), TYPE, CH/CF control association, and the LINE-built report lines.</summary>
public sealed class ReportGroupModel
{
    public string? Name { get; set; }
    public ReportGroupKindModel Kind { get; set; } = ReportGroupKindModel.Detail;

    /// <summary>The CH/CF control operand as written (data-name or FINAL); null when omitted (legal only with a
    /// one-operand CONTROL clause, §13.18.57.3 SR11) or for non-control groups.</summary>
    public string? ControlName { get; set; }
    public bool ControlFinal { get; set; }

    /// <summary>The resolved control LEVEL (the index into <see cref="ReportModel.Controls"/>); −1 until
    /// resolved / for non-control groups.</summary>
    public int ControlLevel { get; set; } = -1;

    public List<ReportLineModel> Lines { get; } = [];
}

/// <summary>The LINE clause form of one report line (ISO §13.18.35; the NEXT PAGE phrases are staged loud).</summary>
public enum ReportLineKindModel { Absolute, Relative }

/// <summary>One report line: its LINE clause, its printable fields in declaration order, and its effective
/// PRESENT WHEN chain (ISO §13.18.41 Format 1) — every condition on the entry that opened the line AND on its
/// ancestors up to the 01 (GR2b: an absent ancestor makes every subordinate absent, so the line is present iff
/// ALL chain conditions are true). Conditions are captured as parse contexts at data bind and bound through the
/// ONE <c>ConditionBinder</c> when the procedure phase runs (<c>ReportWriterBinder.BindReportGroupClauses</c>).</summary>
public sealed class ReportLineModel(ReportLineKindModel kind, int value)
{
    public ReportLineKindModel Kind { get; } = kind;
    public int Value { get; } = value;
    public List<ReportFieldModel> Fields { get; } = [];

    /// <summary>The PRESENT WHEN condition chain (01 → line entry) as captured parse contexts (§13.18.41).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    /// <summary>The bound chain (AND-composed by the emitter); parallel to <see cref="PresentWhenCtxs"/>.</summary>
    public List<BoundCondition> PresentWhen { get; } = [];
}

/// <summary>One COLUMN clause operand (ISO §13.18.14 Format 1): absolute (<c>integer-1</c>) or relative
/// (<c>PLUS integer-2</c> — positioned against the line's horizontal counter, GR7/GR8).</summary>
public readonly record struct ReportColumnSpec(bool Relative, int Value);

/// <summary>One report VARYING counter (ISO §13.18.64): the counter name, the FROM/BY expressions as captured
/// parse contexts (bound to <see cref="From"/>/<see cref="By"/> in the procedure phase; null = the GR3 default 1),
/// stepping once per repetition of the entry's multiple COLUMN clause (GR3a/GR3b).</summary>
public sealed class ReportVaryingModel
{
    public required string Name { get; init; }
    public CobolParserCore.ArithmeticExpressionContext? FromCtx { get; init; }
    public CobolParserCore.ArithmeticExpressionContext? ByCtx { get; init; }
    public BoundExpr? From { get; set; }
    public BoundExpr? By { get; set; }
}

/// <summary>One PRINTABLE item (an entry with a COLUMN clause, ISO §13.18.14): its column operands (one per
/// repetition — a multiple COLUMN clause is a repeating entry, §13.15.4 GR3), the synthetic
/// <see cref="DataItem"/> carrying its PICTURE/JUSTIFIED/BLANK WHEN ZERO (so the emitter reuses the ONE MOVE
/// conversion — §13.18.53.4 GR1's implicit MOVE), its value source, the GROUP INDICATE flag (§13.18.29), its
/// field-local PRESENT WHEN chain (the conditions BELOW the line entry — the line's own chain already gates the
/// whole line), and the entry's VARYING counters (§13.18.64).</summary>
public sealed class ReportFieldModel
{
    public required IReadOnlyList<ReportColumnSpec> Columns { get; init; }
    public required DataItem PrintItem { get; init; }
    public required ReportFieldSource Source { get; init; }
    public bool GroupIndicate { get; init; }

    /// <summary>The first column operand's value — the single-absolute fast path and diagnostics anchor.</summary>
    public int Column => Columns[0].Value;

    /// <summary>PRESENT WHEN conditions strictly below the line entry, down to this entry (§13.18.41 GR2b).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    public List<BoundCondition> PresentWhen { get; } = [];

    /// <summary>The entry's VARYING counters (§13.18.64) — empty for a non-VARYING entry.</summary>
    public List<ReportVaryingModel> Varyings { get; } = [];
}

/// <summary>A printable item's value source, by clause kind.</summary>
public abstract record ReportFieldSource;

/// <summary>A VALUE clause literal (raw operand text — figurative word or literal; ISO §13.18.63).</summary>
public sealed record FieldValueSource(string Raw) : ReportFieldSource;

/// <summary>A SOURCE clause data reference (ISO §13.18.53), captured as base word + IN/OF qualifiers (the FILE
/// STATUS capture pattern) and resolved post-build to <see cref="Item"/>.</summary>
public sealed record FieldDataSource(string Name, IReadOnlyList<string> Qualifiers) : ReportFieldSource
{
    public DataItem? Item { get; set; }
}

/// <summary>A <c>SOURCE IS LINE-COUNTER / PAGE-COUNTER</c> reference (ISO §8.4.3.15 SR1 — referable in the
/// report section only in SOURCE). The counter is the OWN report's (a report-name qualifier naming another
/// report is staged loud — no corpus surface).</summary>
public sealed record FieldCounterSource(bool IsPage) : ReportFieldSource;

/// <summary>The printable face of a SUM entry (ISO §13.18.54.4 GR4 — the sum counter acts as the source item).</summary>
public sealed record FieldSumSource(string CounterId) : ReportFieldSource;

/// <summary>A SOURCE naming the entry's own VARYING counter (ISO §13.18.64.4 GR4 NOTE — the counter is usable as
/// a source data item); <paramref name="Index"/> indexes <see cref="ReportFieldModel.Varyings"/>. Renders as the
/// compose-local counter, re-read per repetition.</summary>
public sealed record FieldVaryingSource(int Index) : ReportFieldSource;

/// <summary>One SUM counter (ISO §13.18.54): its id (the entry's data-name per GR5, else synthesized), the
/// counter scale (GR1 — derived from the entry's PICTURE), the addend data-names (SR5 — items OUTSIDE the
/// report section; report-section addends/rolled totals are staged loud), the UPON detail names (GR7c2), and
/// the RESET operand (GR2).</summary>
public sealed class ReportSumModel
{
    public required string Id { get; init; }
    public int Scale { get; init; }
    public List<(string Name, IReadOnlyList<string> Qualifiers)> AddendNames { get; } = [];
    public List<DataItem> Addends { get; } = [];
    public List<string> UponDetails { get; } = [];
    public string? ResetName { get; set; }
    public bool ResetFinal { get; set; }
    /// <summary>The resolved RESET control level; −1 = no RESET phrase (reset where printed, GR2).</summary>
    public int ResetLevel { get; set; } = -1;
    /// <summary>The group whose processing end resets the counter when no RESET phrase is given (GR2).</summary>
    public required ReportGroupModel PrintedIn { get; init; }
    /// <summary>The COBOL-2002 PICTURE-skeleton introduction gate (a <c>Constructs.*</c> id) this counter's PICTURE
    /// carries — an external-float / national-edited picture, from <see cref="PicInfo.SkeletonGate"/>. The SUM-counter
    /// scale-derivation <c>Analyze</c> (GR1) is a DISTINCT call off <c>ConformanceForest</c>, so this preserves its
    /// gate for the post-bind <c>VersionConformancePass</c> GateData report-Sums walk (DEVLOG 740; else the 0900 below
    /// 2002 is dropped on this error path). Null when the picture is version-invariant (the normal numeric case).</summary>
    public string? SkeletonGate { get; init; }
    /// <summary>The exact where-string the SUM-counter <c>Analyze</c> used (<c>RD '…' SUM counter '…'</c>) — replayed
    /// verbatim by GateData when <see cref="SkeletonGate"/> fires, so the 0900 is byte-identical to the former site.</summary>
    public string SkeletonWhere { get; init; } = "";

    /// <summary>The SUM entry's FULL PRESENT WHEN chain (01 → entry). When any condition is false at a group
    /// presentation the counter is neither printed nor reset for that instance (ISO §13.18.41.4 GR3g /
    /// §13.18.54.4 GR10) — the engine consults the AND of these per presentation.</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    public List<BoundCondition> PresentWhen { get; } = [];
}

public sealed partial class DataBinder
{
    /// <summary>The program unit's report description entries, in source order (ISO §13.6 REPORT SECTION).
    /// (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<ReportModel> Reports => _reports;
    private readonly List<ReportModel> _reports = [];

    private int _sumCounterId;

    /// <summary>Bind the REPORT SECTION's RD entries into <see cref="Reports"/> (ISO §13.14/§13.15). Runs after
    /// <c>BindFileControl</c>/<c>BindFileSection</c> (the FD REPORT clauses are captured there); SOURCE/CONTROL
    /// data references resolve post-build in <see cref="ResolveReports"/> (the FILE STATUS pattern — one
    /// canonical resolution point).</summary>
    private void BindReportSection(Core.ProgramUnitContext program)
    {
        var rs = program.dataDivision()?.reportSection();
        if (rs is null) return;
        foreach (var rd in rs.reportDescriptionEntry())
        {
            using var _ = Edition.At(rd);
            if (rd.reportName()?.GetText() is not { } name) continue;
            var model = new ReportModel { Name = name, CsIndex = Reports.Count };
            BindReportDescriptionClauses(rd, model);
            BindReportGroups(rd, model);
            _reports.Add(model);
        }
    }

    /// <summary>Bind one RD entry's description clauses: PAGE geometry (§13.18.39) with the GR3 defaults,
    /// CONTROL (§13.18.16); GLOBAL (§13.18.27 on an RD) and CODE (§13.18.12) stage loud.</summary>
    private void BindReportDescriptionClauses(Core.ReportDescriptionEntryContext rd, ReportModel model)
    {
        bool heading = false, firstDetail = false, lastDetail = false, footing = false;
        foreach (var clause in rd.reportDescriptionClause())
        {
            if (clause.reportGlobalClause() is not null)
                Edition.Error(DiagnosticCatalog.ReportGlobalClause, $"RD '{model.Name}': the GLOBAL clause on a report description "
                    + "(ISO §13.18.27) is not yet implemented — cross-program report visibility is staged");
            else if (clause.reportCodeClause() is not null)
                Edition.Error(DiagnosticCatalog.ReportCodeClause, $"RD '{model.Name}': the CODE clause (ISO §13.18.12) is not yet "
                    + "implemented");
            else if (clause.reportControlClause() is { } ctl)
            {
                // Operand order IS the hierarchy, major→minor (§13.18.16.4 GR1); FINAL is the highest level (GR2).
                for (int i = 0; i < ctl.ChildCount; i++)
                    switch (ctl.GetChild(i))
                    {
                        case Antlr4.Runtime.Tree.ITerminalNode t when t.Symbol.Type == CobolLexer.FINAL:
                            model.Controls.Add(new ReportControlModel { IsFinal = true });
                            break;
                        case Core.DataReferenceContext dref:
                            var (b, q) = KeyReference(dref);
                            model.Controls.Add(new ReportControlModel { Name = b, Qualifiers = q });
                            break;
                    }
            }
            else if (clause.reportPageClause() is { } page)
            {
                model.Paged = true;
                model.PageLimit = int.Parse(page.integerLiteral().GetText());
                foreach (var sub in page.reportPageSubclause())
                {
                    int v = int.Parse(sub.integerLiteral().GetText());
                    if (sub.HEADING() is not null) { model.Heading = v; heading = true; }
                    else if (sub.FIRST() is not null) { model.FirstDetail = v; firstDetail = true; }
                    else if (sub.LAST() is not null) { model.LastDetail = v; lastDetail = true; }
                    else if (sub.FOOTING() is not null) { model.Footing = v; footing = true; }
                }
            }
        }
        if (!model.Paged) return;
        // The §13.18.39.4 GR3 defaults (the grammar has no LAST CONTROL HEADING phrase, so GR3c always defaults):
        if (!heading) model.Heading = 1;                                              // GR3a
        if (!firstDetail) model.FirstDetail = model.Heading;                          // GR3b
        if (!lastDetail) model.LastDetail = footing ? model.Footing : model.PageLimit;   // GR3d
        model.LastControlHeading = lastDetail ? model.LastDetail
            : footing ? model.Footing : model.PageLimit;                              // GR3c
        if (!footing) model.Footing = lastDetail ? model.LastDetail : model.PageLimit;   // GR3e
    }

    /// <summary>Build one RD's report groups from its (flat, level-numbered) group entries. The line-building
    /// rule (ISO §13.15 / COBOLNET_REPORT_WRITER_DESIGN §3.3): walking the entries in declaration order, a
    /// 01-level entry opens a new GROUP; an entry whose clauses include a LINE clause OPENS a new report line
    /// (LINE is legal at ANY level — RW101A puts <c>LINE PLUS 1</c> on an 03); an entry with a COLUMN clause
    /// appends a printable field to the CURRENT line. PRESENT WHEN conditions (§13.18.41 Format 1) accumulate
    /// down a level-number stack — a line carries the chain 01→line-entry, a field the chain strictly below the
    /// line entry, a SUM entry the full chain (GR2b: an absent ancestor absents every subordinate,
    /// "irrespective of any PRESENT WHEN clauses they may also contain" — the AND of independent conditions).
    /// Legal-but-unimplemented clauses stage loud (§1.4).</summary>
    private void BindReportGroups(Core.ReportDescriptionEntryContext rd, ReportModel model)
    {
        ReportGroupModel? group = null;
        ReportLineModel? line = null;
        // The PRESENT WHEN scope stack: one frame per entry on the current level path (§13.18.41 GR2b).
        var chain = new List<(int Level, Core.ConditionContext? Cond)>();
        int lineChainDepth = 0;   // stack frames whose conditions the CURRENT line already carries
        foreach (var ge in rd.reportGroupEntry())
        {
            using var _ = Edition.At(ge);
            int.TryParse(ge.levelNumber().GetText(), out int level);
            string? entryName = ge.reportGroupName()?.GetText();
            if (level == 1)
            {
                group = new ReportGroupModel { Name = entryName };
                model.Groups.Add(group);
                line = null;
            }
            if (group is null)
            {
                Edition.Error(DiagnosticCatalog.ReportGroupBefore01, $"RD '{model.Name}': report group entry before any 01-level entry");
                continue;
            }
            while (chain.Count > 0 && chain[^1].Level >= level) chain.RemoveAt(chain.Count - 1);

            // Clause capture for THIS entry (clauses may appear in any order within the entry — RW104A).
            string? picText = null, usageText = null, rawValue = null;
            List<EditingPhraseSpec>? reportEditing = null;   // PICTURE EDITING phrases (§13.18.40.2)
            SignSpec? ownSign = null;
            bool justified = false, blankWhenZero = false, groupIndicate = false, staysLoud = false;
            var columns = new List<ReportColumnSpec>();
            ReportFieldSource? source = null;
            ReportLineModel? opened = null;
            Core.ReportSumClauseContext? sumClause = null;
            Core.ConditionContext? ownCond = null;
            var varyings = new List<ReportVaryingModel>();

            foreach (var clause in ge.reportGroupClause())
            {
                if (clause.reportTypeClause()?.reportGroupType() is { } t)
                    BindGroupType(t, group, model);
                else if (clause.reportLineClause() is { } lc)
                {
                    var ops = lc.reportLineOperand();
                    if (ops.Length > 1)
                    {
                        // The multiple LINE clause (§13.18.35.3 SR10) is GR9-equivalent to LINE + a simple OCCURS —
                        // it stages LOUD with the report-group OCCURS repetition family.
                        Edition.Error(DiagnosticCatalog.ReportMultipleLine, $"RD '{model.Name}': a multiple LINE clause "
                            + "(ISO §13.18.35.3 SR10 — vertical repetition) is not yet implemented");
                        staysLoud = true;
                    }
                    var op = ops[0];
                    if (op.NEXT() is not null)
                        Edition.Error(DiagnosticCatalog.ReportLineNextPage, $"RD '{model.Name}': LINE … NEXT PAGE (ISO §13.18.35) is "
                            + "not yet implemented");
                    else if (op.PLUSWORD() is not null)
                        opened = new ReportLineModel(ReportLineKindModel.Relative, int.Parse(op.integerLiteral().GetText()));
                    else
                        opened = new ReportLineModel(ReportLineKindModel.Absolute, int.Parse(op.integerLiteral().GetText()));
                }
                else if (clause.reportNextGroupClause() is not null)
                    Edition.Error(DiagnosticCatalog.ReportNextGroupClause, $"RD '{model.Name}': the NEXT GROUP clause (ISO §13.18.37) is "
                        + "not yet implemented");
                else if (clause.reportColumnClause() is { } cc)
                    foreach (var op in cc.reportColumnOperand())
                        columns.Add(new ReportColumnSpec(op.PLUSWORD() is not null, int.Parse(op.integerLiteral().GetText())));
                else if (clause.reportSourceClause() is { } sc)
                    source = BindSourceClause(sc, model);
                else if (clause.reportSumClause() is { } sm)
                    sumClause = sm;
                else if (clause.reportGroupIndicateClause() is not null)
                    groupIndicate = true;
                else if (clause.reportPresentWhenClause() is { } pw)
                {
                    ownCond = pw.condition();
                    // A FUNCTION inside condition-1 would need the UDF activation-hoist protocol, which is a
                    // statement-context mechanism — staged loud, never a silent mis-hoist.
                    if (HasToken(ownCond, CobolLexer.FUNCTION))
                    {
                        Edition.Error(DiagnosticCatalog.ReportConditionFunction, $"RD '{model.Name}': a FUNCTION reference inside a "
                            + "PRESENT WHEN condition (ISO §13.18.41) is not yet implemented");
                        ownCond = null;
                    }
                }
                else if (clause.reportVaryingClause() is { } vy)
                    foreach (var spec in vy.reportVaryingSpec())
                        varyings.Add(new ReportVaryingModel
                        {
                            Name = spec.cobolWord().GetText(),
                            FromCtx = spec.FROM() is not null ? spec.arithmeticExpression(0) : null,
                            ByCtx = spec.BY() is not null
                                ? spec.arithmeticExpression(spec.FROM() is not null ? 1 : 0) : null,
                        });
                else if (clause.pictureClause()?.PIC_STRING() is { } pic)
                {
                    picText = pic.GetText();
                    reportEditing = BuildEditingSpecs(clause.pictureClause());
                }
                else if (clause.usageClause() is { } usage)
                    usageText = UsageKeyword(usage);
                else if (clause.signClause() is { } sign)
                    ownSign = new SignSpec(sign.LEADING() is not null, sign.SEPARATE() is not null);
                else if (clause.justifiedClause() is not null)
                    justified = true;
                else if (clause.blankWhenZeroClause() is not null)
                    blankWhenZero = true;
                else if (clause.occursClause() is not null)
                {
                    Edition.Error(DiagnosticCatalog.ReportOccursInGroup, $"RD '{model.Name}': OCCURS in a report group description "
                        + "(ISO §13.18.38 repeating entries) is not yet implemented");
                    staysLoud = true;
                }
                else if (clause.valueClause() is { } value)
                    rawValue = ExtractValue(value);
            }

            // GROUP INDICATE shall not share an entry with PRESENT WHEN (ISO §13.15.3 SR17 — GROUP INDICATE IS
            // a fixed-condition PRESENT WHEN, §13.18.29.4 GR1).
            if (groupIndicate && ownCond is not null)
                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': the GROUP "
                    + "INDICATE clause shall not be specified in an entry in which the PRESENT WHEN clause is "
                    + "specified (ISO §13.15.3 SR17)");
            if (groupIndicate && columns.Any(c => c.Relative))
                Edition.Error(DiagnosticCatalog.ReportIndicateRelativeColumn, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': GROUP "
                    + "INDICATE on an entry with a relative (PLUS) COLUMN operand (ISO §13.18.29 / §13.18.14) is "
                    + "not yet implemented");

            // VARYING (§13.18.64): SR1 — the entry shall also contain OCCURS or a multiple LINE or multiple
            // COLUMN clause (the OCCURS / multiple-LINE vehicles staged loud above ride their own 0899).
            if (varyings.Count > 0)
            {
                if (!staysLoud && columns.Count <= 1)
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': a VARYING "
                        + "clause requires the entry to also contain an OCCURS clause or a multiple LINE or "
                        + "multiple COLUMN clause (ISO §13.18.64.3 SR1)");
                foreach (var v in varyings)
                {
                    // SR3: data-name-1 shall not be referenced in arithmetic-expression-1 of the same clause.
                    if (v.FromCtx is not null && varyings.Any(o => HasWord(v.FromCtx, o.Name)))
                        Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' VARYING '{v.Name}': the counter shall "
                            + "not be referenced in the FROM expression of the same VARYING clause (ISO "
                            + "§13.18.64.3 SR3)");
                    // SR3 permits the counter in arithmetic-expression-2 — that leg is staged loud (the
                    // expression would need to bind against the compose-local counter).
                    if (v.ByCtx is not null && varyings.Any(o => HasWord(v.ByCtx, o.Name)))
                        Edition.Error(DiagnosticCatalog.ReportVaryingCounterInExpression, $"RD '{model.Name}' VARYING '{v.Name}': a "
                            + "VARYING counter referenced inside the BY expression (ISO §13.18.64.3 SR3) is not "
                            + "yet implemented");
                }
            }

            if (opened is not null)
            {
                line = opened;
                group.Lines.Add(line);
                // The line's PRESENT WHEN chain: every ancestor condition + this entry's own (§13.18.41 GR2b).
                foreach (var (_, c) in chain) if (c is not null) line.PresentWhenCtxs.Add(c);
                if (ownCond is not null) line.PresentWhenCtxs.Add(ownCond);
                lineChainDepth = chain.Count + 1;   // this entry's frame is pushed below
            }

            // A SUM entry establishes a counter whether or not it is printable (§13.18.54.4 GR1/GR3); its FULL
            // chain governs the GR10 print/reset suppression (§13.18.41.4 GR3g).
            ReportSumModel? sum = null;
            if (sumClause is not null)
            {
                sum = BindSumClause(sumClause, entryName, picText, group, model);
                foreach (var (_, c) in chain) if (c is not null) sum.PresentWhenCtxs.Add(c);
                if (ownCond is not null) sum.PresentWhenCtxs.Add(ownCond);
            }

            if (columns.Count > 0)
            {
                int col = columns[0].Value;
                if (line is null)
                {
                    Edition.Error(DiagnosticCatalog.ReportColumnWithoutLine, $"RD '{model.Name}': a COLUMN clause with no LINE clause in "
                        + "effect (ISO §13.18.14 — a printable item belongs to a report line)");
                    chain.Add((level, ownCond));
                    continue;
                }
                // The printable item (§13.18.14): a SYNTHETIC DataItem carrying the PICTURE so the emitter's ONE
                // MOVE conversion path renders the §13.18.53.4 GR1 implicit MOVE. A printable item is a
                // USAGE-DISPLAY elementary item; its numeric face stores its character IMAGE (StoreAsImage).
                string itemWhere = $"RD '{model.Name}' printable item '{entryName ?? "FILLER"}'";
                var pic = picText is not null
                    ? PictureAnalyzer.Analyze(picText, PictureAnalyzer.ParseUsage(usageText, Edition, itemWhere), Edition,
                        itemWhere, ownSign, currencies: CurrencySigns, blankWhenZero: blankWhenZero, editing: reportEditing)
                    : null;
                if (pic is null)
                {
                    Edition.Error(DiagnosticCatalog.ReportItemMissingPicture, $"RD '{model.Name}': printable item at COLUMN {col} has no "
                        + "PICTURE clause (ISO §13.16 — an elementary printable item requires one)");
                    chain.Add((level, ownCond));
                    continue;
                }
                if (pic.Usage is not Usage.Display)
                    Edition.Error(DiagnosticCatalog.ReportNonDisplayItem, $"RD '{model.Name}': a non-DISPLAY printable item at COLUMN "
                        + $"{col} (ISO §13.15 — printable items are DISPLAY) is not supported");
                var item = new DataItem
                {
                    Level = level,
                    DeclaredAt = Edition.Cursor,
                    CobolName = entryName,
                    CsName = "_rptItem" + _uidCounter,
                    Pic = pic,
                    OwnSign = ownSign,
                    Justified = justified,
                    BlankWhenZero = blankWhenZero,
                };
                item.Uid = _uidCounter++;
                if (pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                    MarkImageForced(item);      // the collected image fact — compose wants the printable CHARACTER image
                ReportFieldSource src = sum is not null ? new FieldSumSource(sum.Id)
                    : source
                    ?? (rawValue is not null ? new FieldValueSource(rawValue)
                        : new FieldValueSource("SPACE"));   // no VALUE/SOURCE/SUM ⇒ spaces (ISO §13.15 — empty item)
                // SOURCE naming the entry's own VARYING counter (§13.18.64.4 GR4 NOTE — a counter is a source item).
                if (src is FieldDataSource { Qualifiers.Count: 0 } fd)
                {
                    int vi = varyings.FindIndex(v => v.Name.Equals(fd.Name, StringComparison.OrdinalIgnoreCase));
                    if (vi >= 0) src = new FieldVaryingSource(vi);
                }
                var field = new ReportFieldModel
                {
                    Columns = columns, PrintItem = item, Source = src, GroupIndicate = groupIndicate,
                };
                // The field-local chain: conditions strictly BELOW the line entry (its own chain gates the line).
                for (int ci = Math.Min(lineChainDepth, chain.Count); ci < chain.Count; ci++)
                    if (chain[ci].Cond is { } c) field.PresentWhenCtxs.Add(c);
                if (ownCond is not null && opened is null) field.PresentWhenCtxs.Add(ownCond);
                field.Varyings.AddRange(varyings);
                line.Fields.Add(field);
            }

            chain.Add((level, ownCond));
        }
    }

    /// <summary>True when <paramref name="tree"/> contains a terminal of <paramref name="tokenType"/>.</summary>
    private static bool HasToken(Antlr4.Runtime.Tree.IParseTree tree, int tokenType)
    {
        if (tree is Antlr4.Runtime.Tree.ITerminalNode t) return t.Symbol.Type == tokenType;
        for (int i = 0; i < tree.ChildCount; i++)
            if (HasToken(tree.GetChild(i), tokenType)) return true;
        return false;
    }

    /// <summary>True when <paramref name="tree"/> contains a word terminal spelled <paramref name="word"/>
    /// (case-insensitive) — the VARYING-counter / report-section-name reference scans.</summary>
    private static bool HasWord(Antlr4.Runtime.Tree.IParseTree tree, string word)
    {
        if (tree is Antlr4.Runtime.Tree.ITerminalNode t)
            return t.GetText().Equals(word, StringComparison.OrdinalIgnoreCase);
        for (int i = 0; i < tree.ChildCount; i++)
            if (HasWord(tree.GetChild(i), word)) return true;
        return false;
    }

    /// <summary>Map a TYPE clause (ISO §13.18.57 Format 2 + the SR9 abbreviations) onto the group model,
    /// capturing the CH/CF control operand.</summary>
    private void BindGroupType(Core.ReportGroupTypeContext t, ReportGroupModel group, ReportModel model)
    {
        if (t.RH() is not null || (t.REPORT() is not null && t.HEADING() is not null))
            group.Kind = ReportGroupKindModel.ReportHeading;
        else if (t.PH() is not null || (t.PAGE() is not null && t.HEADING() is not null))
            group.Kind = ReportGroupKindModel.PageHeading;
        else if (t.CH() is not null || (t.CONTROL() is not null && t.HEADING() is not null))
            group.Kind = ReportGroupKindModel.ControlHeading;
        else if (t.DE() is not null || t.DETAIL() is not null)
            group.Kind = ReportGroupKindModel.Detail;
        else if (t.CF() is not null || (t.CONTROL() is not null && t.FOOTING() is not null))
            group.Kind = ReportGroupKindModel.ControlFooting;
        else if (t.PF() is not null || (t.PAGE() is not null && t.FOOTING() is not null))
            group.Kind = ReportGroupKindModel.PageFooting;
        else
            group.Kind = ReportGroupKindModel.ReportFooting;

        if (group.Kind is ReportGroupKindModel.ControlHeading or ReportGroupKindModel.ControlFooting)
        {
            if (t.FINAL() is not null) group.ControlFinal = true;
            else if (t.dataReference() is { } dref) group.ControlName = dref.cobolWord()?.GetText() ?? dref.GetText();
            // Omitted operand: legal only with a one-operand CONTROL clause (§13.18.57.3 SR11) — resolved later.
        }
        // PH/PF require a PAGE clause (§13.18.57.3 SR12).
        if (group.Kind is ReportGroupKindModel.PageHeading or ReportGroupKindModel.PageFooting && !model.Paged)
            Edition.Error(DiagnosticCatalog.ReportPageTypeRequiresPage, $"RD '{model.Name}': TYPE {group.Kind} requires a PAGE clause that "
                + "defines the page limit (ISO §13.18.57.3 SR12)");
    }

    /// <summary>Bind a SOURCE clause (ISO §13.18.53): a LINE-COUNTER/PAGE-COUNTER register (§8.4.3.15 SR1 — the
    /// only report-section reference position), or a data reference captured as base + qualifiers. Subscripted /
    /// reference-modified operands stage loud (no corpus surface).</summary>
    private ReportFieldSource? BindSourceClause(Core.ReportSourceClauseContext sc, ReportModel model)
    {
        var dref = sc.dataReference();
        if (dref.LINE_COUNTER() is not null || dref.PAGE_COUNTER() is not null)
        {
            // A report-name qualifier naming a DIFFERENT report's counter is legal (§8.4.3.15 SR2) — staged.
            if (dref.cobolWord() is { } q && !q.GetText().Equals(model.Name, StringComparison.OrdinalIgnoreCase))
                Edition.Error(DiagnosticCatalog.ReportSourceOtherReportCounter, $"RD '{model.Name}': SOURCE {dref.GetText()} — a counter of "
                    + "another report (ISO §8.4.3.15 SR2) is not yet implemented");
            return new FieldCounterSource(dref.PAGE_COUNTER() is not null);
        }
        foreach (var sfx in dref.dataReferenceSuffix())
            if (sfx.subscriptPart() is not null || sfx.refModPart() is not null)
            {
                Edition.Error(DiagnosticCatalog.ReportSourceSubscripted, $"RD '{model.Name}': SOURCE {dref.GetText()} — a subscripted or "
                    + "reference-modified SOURCE operand (ISO §13.18.53) is not yet implemented");
                return null;
            }
        var (b, qls) = KeyReference(dref);
        return new FieldDataSource(b, qls);
    }

    /// <summary>Bind a SUM clause (ISO §13.18.54) into a <see cref="ReportSumModel"/>: the counter id (the
    /// entry's data-name, GR5, else synthesized), the addend names, UPON details, and the RESET operand. The
    /// counter's scale derives from the entry's PICTURE (GR1).</summary>
    private ReportSumModel BindSumClause(
        Core.ReportSumClauseContext sm, string? entryName, string? picText, ReportGroupModel group, ReportModel model)
    {
        // Scale-derivation analysis (GR1) — threads the edition + the program currency symbol like every other
        // Analyze site (a custom §12.3.7 currency symbol in a SUM counter's PICTURE must classify, not error).
        string sumWhere = $"RD '{model.Name}' SUM counter '{entryName ?? "FILLER"}'";
        var pic = picText is not null
            ? PictureAnalyzer.Analyze(picText, Usage.Display, Edition, sumWhere, currencies: CurrencySigns)
            : null;
        var sum = new ReportSumModel
        {
            Id = entryName ?? $"__SUM{_sumCounterId++}",
            Scale = pic?.Scale ?? 0,
            PrintedIn = group,
            // Preserve an external-float / national-edited SkeletonGate for the post-bind GateData report-Sums walk
            // (this PicInfo is otherwise discarded — only Scale is used — so the 0900 would drop; DEVLOG 740).
            SkeletonGate = pic?.SkeletonGate,
            SkeletonWhere = sumWhere,
        };
        foreach (var op in sm.sumOperand())
        {
            if (op.reportName() is not null)
                Edition.Error(DiagnosticCatalog.ReportSumCrossReport, $"RD '{model.Name}': SUM … OF report-name (a cross-report sum, "
                    + "ISO §13.18.54.3 SR4g) is not yet implemented");
            var (b, q) = KeyReference(op.dataReference());
            sum.AddendNames.Add((b, q));
        }
        foreach (var up in sm.dataReference())
            sum.UponDetails.Add(up.cobolWord()?.GetText() ?? up.GetText());   // UPON detail-names (GR7c2)
        if (sm.reportSumReset() is { } reset)
        {
            if (reset.FINAL() is not null) sum.ResetFinal = true;
            else if (reset.dataReference() is { } rref) sum.ResetName = rref.cobolWord()?.GetText() ?? rref.GetText();
        }
        model.Sums.Add(sum);
        return sum;
    }

    /// <summary>Post-build resolution for every report (the <c>ResolveFiles</c> pattern — runs after the storage
    /// forest is complete): the owning FILE (§13.18.46), SOURCE / CONTROL / SUM-addend data items, CH/CF control
    /// levels (§13.18.57.3 SR10/SR11), RESET levels, and the report's line width.</summary>
    internal void ResolveReports()
    {
        foreach (var model in Reports)
        {
            // The owning file: the FD whose REPORT(S) clause names this report (ISO §13.18.46.4 GR1; §13.14
            // requires each report-name be named in exactly one REPORT clause of an FD).
            model.File = Files.FirstOrDefault(f =>
                f.ReportNames.Any(rn => rn.Equals(model.Name, StringComparison.OrdinalIgnoreCase)));
            if (model.File is null)
                Edition.Error(DiagnosticCatalog.ReportNotInFile, $"RD '{model.Name}' is not named in any file description entry's "
                    + "REPORT clause (ISO §13.18.46 / §13.14)");
            else if (model.File.ReportNames.Count > 1)
                Edition.Error(DiagnosticCatalog.ReportMultipleOnFile, $"file '{model.File.CobolName}': multiple reports on one file "
                    + "(REPORTS ARE …, ISO §13.18.46) are not yet implemented");

            foreach (var ctl in model.Controls)
                if (!ctl.IsFinal && ctl.Name is { } cn)
                {
                    ctl.Item = LookupQualified(cn, ctl.Qualifiers);
                    if (ctl.Item is null)
                        Edition.Error(DiagnosticCatalog.ReportControlOperandUnresolved, $"RD '{model.Name}': CONTROL operand '{cn}' does not "
                            + "resolve to a data item (ISO §13.18.16.3 SR3)");
                }

            foreach (var group in model.Groups)
            {
                // CH/CF control level (§13.18.57.3 SR10/SR11): match the operand against the CONTROL hierarchy;
                // an omitted operand selects the sole control.
                if (group.Kind is ReportGroupKindModel.ControlHeading or ReportGroupKindModel.ControlFooting)
                {
                    group.ControlLevel = group.ControlFinal
                        ? model.Controls.FindIndex(c => c.IsFinal)
                        : group.ControlName is { } gcn
                            ? model.Controls.FindIndex(c => gcn.Equals(c.Name, StringComparison.OrdinalIgnoreCase))
                            : model.Controls.Count == 1 ? 0 : -1;
                    if (group.ControlLevel < 0)
                        Edition.Error(DiagnosticCatalog.ReportControlTypeOperand, $"RD '{model.Name}': the TYPE C{(group.Kind == ReportGroupKindModel.ControlHeading ? "H" : "F")} operand "
                            + "shall be an operand of the CONTROL clause (ISO §13.18.57.3 SR10/SR11)");
                }
                foreach (var ln in group.Lines)
                    foreach (var f in ln.Fields)
                        if (f.Source is FieldDataSource ds)
                        {
                            ds.Item = LookupQualified(ds.Name, ds.Qualifiers);
                            if (ds.Item is null)
                                Edition.Error(DiagnosticCatalog.ReportSourceOperandUnresolved, $"RD '{model.Name}': SOURCE '{ds.Name}' does not "
                                    + "resolve to a data item (ISO §13.18.53.3 SR4)");
                        }
            }

            foreach (var sum in model.Sums)
            {
                foreach (var (an, aq) in sum.AddendNames)
                {
                    if (model.Sums.Any(s => s.Id.Equals(an, StringComparison.OrdinalIgnoreCase)))
                    {
                        // A report-section addend (a rolled total, §13.18.54.4 GR6) — staged loud.
                        Edition.Error(DiagnosticCatalog.ReportSumRolledTotal, $"RD '{model.Name}': SUM addend '{an}' names another sum "
                            + "counter (rolled totals, ISO §13.18.54.4 GR6) — not yet implemented");
                        continue;
                    }
                    if (LookupQualified(an, aq) is { } item) sum.Addends.Add(item);
                    else Edition.Error(DiagnosticCatalog.ReportSumAddendUnresolved, $"RD '{model.Name}': SUM addend '{an}' does not resolve "
                        + "to a data item outside the report section (ISO §13.18.54.3 SR5)");
                }
                if (sum.ResetFinal)
                    sum.ResetLevel = model.Controls.FindIndex(c => c.IsFinal);
                else if (sum.ResetName is { } rn)
                    sum.ResetLevel = model.Controls.FindIndex(c => rn.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                if ((sum.ResetFinal || sum.ResetName is not null) && sum.ResetLevel < 0)
                    Edition.Error(DiagnosticCatalog.ReportResetNotControlOperand, $"RD '{model.Name}': RESET ON '{sum.ResetName ?? "FINAL"}' is "
                        + "not an operand of the CONTROL clause (ISO §13.18.54.3 SR8)");
            }

            // PRESENT WHEN SR16 (§13.15.3): condition-1 shall not reference a sum counter, LINE-COUNTER,
            // PAGE-COUNTER, or another report section data item. Scanned over each DISTINCT captured condition
            // (an entry's condition appears in every subordinate chain) against this RD's report-section names.
            CheckConditionOperands(model);

            // VARYING SR2 (§13.18.64.3): data-name-1 shall not be defined elsewhere in the source element.
            foreach (var g in model.Groups)
                foreach (var ln in g.Lines)
                    foreach (var f in ln.Fields)
                        foreach (var v in f.Varyings)
                            if (ByName.ContainsKey(v.Name))
                                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' VARYING '{v.Name}': the counter "
                                    + "data-name shall not be defined elsewhere in the source element (ISO "
                                    + "§13.18.64.3 SR2)");

            // Line width: the FD's fixed RECORD CONTAINS, else the widest NOMINAL field extent — absolute
            // operands at column + width − 1; relative (PLUS) operands walked against the line's horizontal
            // counter with every item present (§13.18.14.4 GR7–GR9; presentation-time absence only SHRINKS the
            // occupied extent, so the all-present walk is the width bound).
            int widest = 1;
            foreach (var g in model.Groups)
                foreach (var ln in g.Lines)
                {
                    int hc = 0;
                    foreach (var f in ln.Fields)
                        foreach (var spec in f.Columns)
                        {
                            int left = spec.Relative ? hc + spec.Value : spec.Value;
                            hc = left + f.PrintItem.DisplayTextWidth - 1;   // GR9 — the rightmost column becomes the counter
                            widest = Math.Max(widest, hc);
                        }
                }
            model.LineWidth = model.File?.RecordContains ?? widest;
        }
    }

    /// <summary>The §13.15.3 SR16 scan: no PRESENT WHEN condition of <paramref name="model"/> may reference
    /// LINE-COUNTER, PAGE-COUNTER, a sum counter, or another report section data item (group / printable-entry
    /// names). Token-level scan over each distinct captured condition context.</summary>
    private void CheckConditionOperands(ReportModel model)
    {
        var conds = new HashSet<Core.ConditionContext>(ReferenceEqualityComparer.Instance);
        foreach (var g in model.Groups)
            foreach (var ln in g.Lines)
            {
                foreach (var c in ln.PresentWhenCtxs) conds.Add(c);
                foreach (var f in ln.Fields)
                    foreach (var c in f.PresentWhenCtxs) conds.Add(c);
            }
        foreach (var s in model.Sums)
            foreach (var c in s.PresentWhenCtxs) conds.Add(c);
        if (conds.Count == 0) return;

        // A name also declared in ordinary storage resolves THERE (never to the report item), so it is not an
        // SR16 reference — only report-section-exclusive names are scanned (no textual false positives).
        var names = new List<string>();
        foreach (var g in model.Groups)
        {
            if (g.Name is { } gn && !ByName.ContainsKey(gn)) names.Add(gn);
            foreach (var ln in g.Lines)
                foreach (var f in ln.Fields)
                    if (f.PrintItem.CobolName is { } fn && !ByName.ContainsKey(fn)) names.Add(fn);
        }
        foreach (var s in model.Sums) if (!ByName.ContainsKey(s.Id)) names.Add(s.Id);

        foreach (var cond in conds)
        {
            if (HasToken(cond, CobolLexer.LINE_COUNTER) || HasToken(cond, CobolLexer.PAGE_COUNTER))
                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}': a PRESENT WHEN condition shall not "
                    + "reference LINE-COUNTER or PAGE-COUNTER (ISO §13.15.3 SR16)");
            foreach (var n in names)
                if (HasWord(cond, n))
                {
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}': a PRESENT WHEN condition shall not "
                        + $"reference the report section data item '{n}' (ISO §13.15.3 SR16)");
                    break;
                }
        }
    }

    /// <summary>Resolve a (possibly IN/OF-qualified) data-name against the storage forest: the first
    /// <see cref="ByName"/> candidate whose ancestor chain matches every qualifier in written
    /// (innermost→outermost) order, skips allowed (ISO §8.4.2.2 Qualification).</summary>
    private DataItem? LookupQualified(string name, IReadOnlyList<string> qualifiers)
    {
        if (!ByName.TryGetValue(name, out var list) || list.Count == 0) return null;
        if (qualifiers.Count == 0) return list[0];
        foreach (var cand in list)
        {
            int qi = 0;
            for (DataItem? a = cand.Parent; a is not null && qi < qualifiers.Count; a = a.Parent)
                if (string.Equals(a.CobolName, qualifiers[qi], StringComparison.OrdinalIgnoreCase)) qi++;
            if (qi == qualifiers.Count) return cand;
        }
        return null;
    }
}
