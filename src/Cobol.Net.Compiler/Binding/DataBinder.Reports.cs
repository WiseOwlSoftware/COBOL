// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

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

/// <summary>One report line: its LINE clause and its printable fields in declaration order.</summary>
public sealed class ReportLineModel(ReportLineKindModel kind, int value)
{
    public ReportLineKindModel Kind { get; } = kind;
    public int Value { get; } = value;
    public List<ReportFieldModel> Fields { get; } = [];
}

/// <summary>One PRINTABLE item (an entry with a COLUMN clause, ISO §13.18.14): its column, the synthetic
/// <see cref="DataItem"/> carrying its PICTURE/JUSTIFIED/BLANK WHEN ZERO (so the emitter reuses the ONE MOVE
/// conversion — §13.18.53.4 GR1's implicit MOVE), its value source, and the GROUP INDICATE flag (§13.18.29).</summary>
public sealed class ReportFieldModel
{
    public int Column { get; init; }
    public required DataItem PrintItem { get; init; }
    public required ReportFieldSource Source { get; init; }
    public bool GroupIndicate { get; init; }
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
}

public sealed partial class DataBinder
{
    /// <summary>The program unit's report description entries, in source order (ISO §13.6 REPORT SECTION).</summary>
    public List<ReportModel> Reports { get; } = [];

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
            if (rd.reportName()?.GetText() is not { } name) continue;
            var model = new ReportModel { Name = name, CsIndex = Reports.Count };
            BindReportDescriptionClauses(rd, model);
            BindReportGroups(rd, model);
            Reports.Add(model);
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
                Edition.Error("COBOLNET0899", $"RD '{model.Name}': the GLOBAL clause on a report description "
                    + "(ISO §13.18.27) is not yet implemented — cross-program report visibility is staged");
            else if (clause.reportCodeClause() is not null)
                Edition.Error("COBOLNET0899", $"RD '{model.Name}': the CODE clause (ISO §13.18.12) is not yet "
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
    /// appends a printable field to the CURRENT line. Legal-but-unimplemented clauses stage loud (§1.4).</summary>
    private void BindReportGroups(Core.ReportDescriptionEntryContext rd, ReportModel model)
    {
        ReportGroupModel? group = null;
        ReportLineModel? line = null;
        foreach (var ge in rd.reportGroupEntry())
        {
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
                Edition.Error("COBOLNET0899", $"RD '{model.Name}': report group entry before any 01-level entry");
                continue;
            }

            // Clause capture for THIS entry (clauses may appear in any order within the entry — RW104A).
            string? picText = null, usageText = null, rawValue = null;
            SignSpec? ownSign = null;
            bool justified = false, blankWhenZero = false, groupIndicate = false;
            int? column = null;
            ReportFieldSource? source = null;
            ReportLineModel? opened = null;
            Core.ReportSumClauseContext? sumClause = null;

            foreach (var clause in ge.reportGroupClause())
            {
                if (clause.reportTypeClause()?.reportGroupType() is { } t)
                    BindGroupType(t, group, model);
                else if (clause.reportLineClause() is { } lc)
                {
                    if (lc.NEXT() is not null)
                        Edition.Error("COBOLNET0899", $"RD '{model.Name}': LINE … NEXT PAGE (ISO §13.18.35) is "
                            + "not yet implemented");
                    else if (lc.PLUSWORD() is not null)
                        opened = new ReportLineModel(ReportLineKindModel.Relative, int.Parse(lc.integerLiteral().GetText()));
                    else
                        opened = new ReportLineModel(ReportLineKindModel.Absolute, int.Parse(lc.integerLiteral().GetText()));
                }
                else if (clause.reportNextGroupClause() is not null)
                    Edition.Error("COBOLNET0899", $"RD '{model.Name}': the NEXT GROUP clause (ISO §13.18.37) is "
                        + "not yet implemented");
                else if (clause.reportColumnClause() is { } cc)
                    column = int.Parse(cc.integerLiteral().GetText());
                else if (clause.reportSourceClause() is { } sc)
                    source = BindSourceClause(sc, model);
                else if (clause.reportSumClause() is { } sm)
                    sumClause = sm;
                else if (clause.reportGroupIndicateClause() is not null)
                    groupIndicate = true;
                else if (clause.pictureClause()?.PIC_STRING() is { } pic)
                    picText = pic.GetText();
                else if (clause.usageClause() is { } usage)
                    usageText = UsageKeyword(usage);
                else if (clause.signClause() is { } sign)
                    ownSign = new SignSpec(sign.LEADING() is not null, sign.SEPARATE() is not null);
                else if (clause.justifiedClause() is not null)
                    justified = true;
                else if (clause.blankWhenZeroClause() is not null)
                    blankWhenZero = true;
                else if (clause.occursClause() is not null)
                    Edition.Error("COBOLNET0899", $"RD '{model.Name}': OCCURS in a report group description "
                        + "(ISO §13.18.38 repeating entries) is not yet implemented");
                else if (clause.valueClause() is { } value)
                    rawValue = ExtractValue(value);
            }

            if (opened is not null) { line = opened; group.Lines.Add(line); }

            // A SUM entry establishes a counter whether or not it is printable (§13.18.54.4 GR1/GR3).
            ReportSumModel? sum = null;
            if (sumClause is not null)
                sum = BindSumClause(sumClause, entryName, picText, group, model);

            if (column is { } col)
            {
                if (line is null)
                {
                    Edition.Error("COBOLNET0899", $"RD '{model.Name}': a COLUMN clause with no LINE clause in "
                        + "effect (ISO §13.18.14 — a printable item belongs to a report line)");
                    continue;
                }
                // The printable item (§13.18.14): a SYNTHETIC DataItem carrying the PICTURE so the emitter's ONE
                // MOVE conversion path renders the §13.18.53.4 GR1 implicit MOVE. A printable item is a
                // USAGE-DISPLAY elementary item; its numeric face stores its character IMAGE (StoreAsImage).
                var pic = picText is not null
                    ? PicInfo.Analyze(picText, PicInfo.ParseUsage(usageText), ownSign, CurrencyPicSymbol, blankWhenZero)
                    : null;
                if (pic is null)
                {
                    Edition.Error("COBOLNET0899", $"RD '{model.Name}': printable item at COLUMN {col} has no "
                        + "PICTURE clause (ISO §13.16 — an elementary printable item requires one)");
                    continue;
                }
                if (pic.Usage is not Usage.Display)
                    Edition.Error("COBOLNET0899", $"RD '{model.Name}': a non-DISPLAY printable item at COLUMN "
                        + $"{col} (ISO §13.15 — printable items are DISPLAY) is not supported");
                var item = new DataItem
                {
                    Level = level,
                    CobolName = entryName,
                    CsName = "_rptItem" + _uidCounter,
                    Pic = pic,
                    OwnSign = ownSign,
                    Justified = justified,
                    BlankWhenZero = blankWhenZero,
                };
                item.Uid = _uidCounter++;
                if (pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                    item.StoreAsImage = true;   // compose wants the printable CHARACTER image
                ReportFieldSource src = sum is not null ? new FieldSumSource(sum.Id)
                    : source
                    ?? (rawValue is not null ? new FieldValueSource(rawValue)
                        : new FieldValueSource("SPACE"));   // no VALUE/SOURCE/SUM ⇒ spaces (ISO §13.15 — empty item)
                line.Fields.Add(new ReportFieldModel
                {
                    Column = col, PrintItem = item, Source = src, GroupIndicate = groupIndicate,
                });
            }
        }
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
            Edition.Error("COBOLNET0899", $"RD '{model.Name}': TYPE {group.Kind} requires a PAGE clause that "
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
                Edition.Error("COBOLNET0899", $"RD '{model.Name}': SOURCE {dref.GetText()} — a counter of "
                    + "another report (ISO §8.4.3.15 SR2) is not yet implemented");
            return new FieldCounterSource(dref.PAGE_COUNTER() is not null);
        }
        foreach (var sfx in dref.dataReferenceSuffix())
            if (sfx.subscriptPart() is not null || sfx.refModPart() is not null)
            {
                Edition.Error("COBOLNET0899", $"RD '{model.Name}': SOURCE {dref.GetText()} — a subscripted or "
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
        var pic = picText is not null ? PicInfo.Analyze(picText, Usage.Display) : null;
        var sum = new ReportSumModel
        {
            Id = entryName ?? $"__SUM{_sumCounterId++}",
            Scale = pic?.Scale ?? 0,
            PrintedIn = group,
        };
        foreach (var op in sm.sumOperand())
        {
            if (op.reportName() is not null)
                Edition.Error("COBOLNET0899", $"RD '{model.Name}': SUM … OF report-name (a cross-report sum, "
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
    private void ResolveReports()
    {
        foreach (var model in Reports)
        {
            // The owning file: the FD whose REPORT(S) clause names this report (ISO §13.18.46.4 GR1; §13.14
            // requires each report-name be named in exactly one REPORT clause of an FD).
            model.File = Files.FirstOrDefault(f =>
                f.ReportNames.Any(rn => rn.Equals(model.Name, StringComparison.OrdinalIgnoreCase)));
            if (model.File is null)
                Edition.Error("COBOLNET0899", $"RD '{model.Name}' is not named in any file description entry's "
                    + "REPORT clause (ISO §13.18.46 / §13.14)");
            else if (model.File.ReportNames.Count > 1)
                Edition.Error("COBOLNET0899", $"file '{model.File.CobolName}': multiple reports on one file "
                    + "(REPORTS ARE …, ISO §13.18.46) are not yet implemented");

            foreach (var ctl in model.Controls)
                if (!ctl.IsFinal && ctl.Name is { } cn)
                {
                    ctl.Item = LookupQualified(cn, ctl.Qualifiers);
                    if (ctl.Item is null)
                        Edition.Error("COBOLNET0899", $"RD '{model.Name}': CONTROL operand '{cn}' does not "
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
                        Edition.Error("COBOLNET0899", $"RD '{model.Name}': the TYPE C{(group.Kind == ReportGroupKindModel.ControlHeading ? "H" : "F")} operand "
                            + "shall be an operand of the CONTROL clause (ISO §13.18.57.3 SR10/SR11)");
                }
                foreach (var ln in group.Lines)
                    foreach (var f in ln.Fields)
                        if (f.Source is FieldDataSource ds)
                        {
                            ds.Item = LookupQualified(ds.Name, ds.Qualifiers);
                            if (ds.Item is null)
                                Edition.Error("COBOLNET0899", $"RD '{model.Name}': SOURCE '{ds.Name}' does not "
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
                        Edition.Error("COBOLNET0899", $"RD '{model.Name}': SUM addend '{an}' names another sum "
                            + "counter (rolled totals, ISO §13.18.54.4 GR6) — not yet implemented");
                        continue;
                    }
                    if (LookupQualified(an, aq) is { } item) sum.Addends.Add(item);
                    else Edition.Error("COBOLNET0899", $"RD '{model.Name}': SUM addend '{an}' does not resolve "
                        + "to a data item outside the report section (ISO §13.18.54.3 SR5)");
                }
                if (sum.ResetFinal)
                    sum.ResetLevel = model.Controls.FindIndex(c => c.IsFinal);
                else if (sum.ResetName is { } rn)
                    sum.ResetLevel = model.Controls.FindIndex(c => rn.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                if ((sum.ResetFinal || sum.ResetName is not null) && sum.ResetLevel < 0)
                    Edition.Error("COBOLNET0899", $"RD '{model.Name}': RESET ON '{sum.ResetName ?? "FINAL"}' is "
                        + "not an operand of the CONTROL clause (ISO §13.18.54.3 SR8)");
            }

            // Line width: the FD's fixed RECORD CONTAINS, else the widest field extent (column + width − 1).
            int widest = 1;
            foreach (var g in model.Groups)
                foreach (var ln in g.Lines)
                    foreach (var f in ln.Fields)
                        widest = Math.Max(widest, f.Column + f.PrintItem.ImageWidth - 1);
            model.LineWidth = model.File?.RecordContains ?? widest;
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
