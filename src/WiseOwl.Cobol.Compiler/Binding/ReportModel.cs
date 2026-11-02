// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Compiler.Oo;
using ReportNextGroup = WiseOwl.Cobol.Runtime.IO.ReportNextGroup;
using ReportNextGroupKind = WiseOwl.Cobol.Runtime.IO.ReportNextGroupKind;
using WiseOwl.Cobol.Runtime;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  REPORT SECTION binding (ISO/IEC 1989:2023 §13.6 report section / §13.14 report description / §13.15 report
//  group description; REPORT_WRITER_DESIGN §3). Each RD becomes a ReportModel — page geometry with the
//  §13.18.39.4 GR3 defaults applied, the CONTROL hierarchy, and the report groups as LINE-clause-built line
//  lists of printable fields. A printable item is carried as a SYNTHETIC DataItem (PicInfo + flags, never added
//  to the storage forest — report groups are not data storage): the emitter then renders each SOURCE/VALUE
//  through the ONE MOVE conversion path (CSharpEmitter.ConvertSource), which IS §13.18.53.4 GR1's implicit MOVE.
//  Legal-but-unimplemented clauses stage LOUD here (Edition.Error, COBOL0899) — never silently dropped (§1.4).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>One report description entry (ISO §13.14): identity, owning file (resolved post-build from the FD's
/// REPORT clause, §13.18.46), the §13.18.39.4 page regions (GR3 defaults applied), the §13.18.16 control
/// hierarchy (major→minor; FINAL first when present), the report groups, and the SUM counters.</summary>
public sealed class ReportModel
{
    public required string Name { get; init; }

    /// <summary>The report description entry's own source position — the cursor every post-build rule about the report
    /// (<c>DataBinder.ResolveReports</c>) is reported at when no finer position is known, so an RD diagnostic is never
    /// printed without a file and line (kb/Work PB2034).</summary>
    public WiseOwl.Cobol.Editions.DiagnosticCursor At { get; init; }

    /// <summary>The report file whose FD names this report in a REPORT(S) clause (ISO §13.18.46), resolved
    /// post-build; a report named by NO file description entry is a bind error.</summary>
    public FileModel? File { get; set; }

    /// <summary>True when the RD has a PAGE clause (§13.18.39.4 GR2a — absent ⇒ one page of indefinite length;
    /// the page-fit/advance machinery is then inert).</summary>
    public bool Paged { get; set; }

    // The §13.18.39.4 GR2 page regions (GR3 defaults applied by the binder; meaningful only when Paged).
    public int PageLimit { get; set; }

    /// <summary>⛔ THE ONE VERTICAL OPERAND LIMIT — "the page limit, or 9999 if the report is not divided into
    /// pages". The standard writes that sentence for the LINE clause (§13.18.35.3 SR3) and again for the NEXT
    /// GROUP clause (§13.18.37.3 SR1); both screens read it here, with <see cref="VerticalLimitWords"/> naming it
    /// in their diagnostics.</summary>
    public int VerticalLimit => Paged ? PageLimit : 9999;

    /// <summary>What <see cref="VerticalLimit"/> is, in a diagnostic's words.</summary>
    public string VerticalLimitWords => Paged ? $"the page limit {PageLimit}"
        : "9999, the limit for a report that is not divided into pages";
    public int Heading { get; set; }
    public int FirstDetail { get; set; }
    public int LastControlHeading { get; set; }
    public int LastDetail { get; set; }
    public int Footing { get; set; }

    /// <summary>The page width — integer-2 of the PAGE clause (ISO §13.18.39.4 GR2b, "the maximum number of print
    /// columns that may be accommodated in any line of the report"), or 999 when it is omitted (GR5, "a value of
    /// 999 is assumed for the page width"). It is the bound of the COLUMN clause's own operands (§13.18.14.3 SR6,
    /// COBOL2710) and of the line the engine places printable items in (§13.18.14.4 GR5, EC-REPORT-PAGE-WIDTH).
    /// Independent of <see cref="Paged"/>: a PAGE clause may give the width alone.</summary>
    public int PageWidth { get; set; } = DefaultPageWidth;

    /// <summary>The PAGE clause wrote integer-2 (so <see cref="PageWidth"/> is the program's, not GR5's default).</summary>
    public bool PageWidthWritten { get; set; }

    /// <summary>§13.18.39.4 GR5's assumed page width.</summary>
    public const int DefaultPageWidth = 999;

    /// <summary>The report line width: the record size the FD's RECORD clause establishes in any of its three formats
    /// (<see cref="FileModel.RecordClause"/>, upper operand) less the CODE characters, else the widest field extent
    /// (column + image width − 1) over the report (the §13.18.39.4 GR5 page-width default 999 is a MAXIMUM, not
    /// a record length). A line wider than this is cut when recorded. Computed post-build.</summary>
    public int LineWidth { get; set; } = 1;

    /// <summary>The RD's CODE clause (ISO §13.18.12), or null — the characters every logical record this report
    /// writes begins with (GR1).</summary>
    public ReportCodeModel? Code { get; set; }

    /// <summary>The RD wrote a CODE clause — true even when its operand was refused, so §13.18.12.3 SR3's "specified
    /// for each report" is asked of what was WRITTEN and never double-reports a refused operand.</summary>
    public bool CodeWritten { get; set; }

    /// <summary>The CONTROL hierarchy in major→minor order (ISO §13.18.16.4 GR1; FINAL, if present, first — GR2).</summary>
    public List<ReportControlModel> Controls { get; } = [];

    /// <summary>The report group description entries as WRITTEN, in source order (§13.15) — every entry the RD
    /// is followed by, whether or not it produced a line, a printable field or a counter. The screens whose rule
    /// is about what was written (§13.15.3 SR16's PRESENT WHEN operands, the report-section name set) read this
    /// and never the bound products, which exist only for the entries that make one (kb/Work PB1289).</summary>
    public IReadOnlyList<Core.ReportGroupEntryContext> WrittenEntries { get; init; } = [];

    /// <summary>⛔ THE ONE LIST OF THE DATA-NAMES THIS REPORT DECLARES (kb/Work PB1046): every WRITTEN entry that has a
    /// data-name, with the names of the levels above it (<see cref="ReportNamedEntry.Qualification"/>) and whether it
    /// establishes a sum counter. Read off <see cref="WrittenEntries"/> once, because the questions asked of "the report
    /// section's names" — which names no ordinary item also carries (§13.18.16.3 SR2, §13.15.3 SR16), which entry a
    /// procedure division or other-section reference reaches (§13.8.6.2.3), whether a word is a sum counter (§8.4.2.3.3
    /// SR8) — are about NAMES the programmer declared, never about the bound products (lines, fields, counters), which
    /// exist only for the entries that make one (kb/Work PB1289).</summary>
    public IReadOnlyList<ReportNamedEntry> NamedEntries => _namedEntries ??= ReportNamedEntry.ReadAll(WrittenEntries);
    private IReadOnlyList<ReportNamedEntry>? _namedEntries;

    /// <summary>The report groups in declaration order.</summary>
    public List<ReportGroupModel> Groups { get; } = [];

    /// <summary>The SUM counters of this report (ISO §13.18.54), one per OCCURRENCE, in counter-id order (the
    /// index of a counter in this list IS its <see cref="ReportSumModel.Id"/> once the description is bound —
    /// <c>DataBinder.SealSumCounters</c>).</summary>
    public List<ReportSumModel> Sums { get; } = [];

    /// <summary>The SUM ENTRIES of this report, one <see cref="ReportSumFamily"/> per entry containing a SUM clause
    /// (ISO §13.18.54.4 GR1), in the order their counter-id blocks were reserved.</summary>
    public List<ReportSumFamily> SumFamilies { get; } = [];

    /// <summary>EVERY entry of this report that carries a value a rolled total can add (ISO §13.18.54.4 GR6): the
    /// SUM entries (<see cref="SumFamilies"/>' families, the same objects) and the entries with a SOURCE or VALUE
    /// clause, printable or not. The one list SUM data-name-1 (§13.18.54.3 SR4) is resolved against.</summary>
    public List<ReportEntryFamily> EntryFamilies { get; } = [];

    /// <summary>The occurrences of those entries, report-wide in binding order — the rows the procedure-phase binder
    /// gives a value and the emitter a presence slot (<see cref="ReportItemOccurrence"/>).</summary>
    public List<ReportItemOccurrence> ItemOccurrences { get; } = [];

    /// <summary>The data-names the report's VARYING clauses define (§13.18.64.3 SR2), once per WRITTEN entry — a
    /// repeating entry's replays share one declaration. Filled by <c>ScreenReportVaryingClauses</c>, which asks SR2's
    /// "not defined elsewhere in the source element" of each name once the whole source element is described;
    /// <see cref="HasVarying"/> is what the FLAG-02 pass asks.</summary>
    public List<string> VaryingNames { get; } = [];

    /// <summary>The report writes a VARYING clause somewhere (§13.18.64) — including a group entry's, which no
    /// printable field of its own carries.</summary>
    public bool HasVarying => VaryingNames.Count > 0;

    /// <summary>This report's index within its program unit — backs the emitted engine field name
    /// (<c>__RPT_{CsIndex}</c>).</summary>
    public int CsIndex { get; set; }

    /// <summary>A compilation-unique identity (drawn from the declaring unit's disjoint uid band). It names the
    /// members every program that can see this report emits for it — the §14.9.49.4 GR4 USE BEFORE REPORTING
    /// selector in particular, whose name must agree between a contained program and its containers.</summary>
    public required int Uid { get; init; }

    /// <summary>The RD entry specifies the GLOBAL clause (ISO §13.18.27.3 SR1 e)): the report-name is a global
    /// name, and so is every data-name subordinate to it (§13.18.27.4 GR1) — its report groups and its sum
    /// counters — visible to every program the declaring program contains, directly or indirectly (GR2).</summary>
    public bool IsGlobal { get; set; }

    /// <summary>This report's PAGE-COUNTER AS A DATA ITEM (ISO §8.4.3.15.4 GR1 — "temporary unsigned integer
    /// data items of class and category numeric, which are maintained for each report"): the implicitly-defined
    /// register a procedure division RECEIVING reference resolves to (§8.4.3.15.3 SR1, kb/Work PB429). Off
    /// ByName/Roots like the SUM counter's register and the OCCURS DYNAMIC CAPACITY register — its value IS the
    /// engine's, so it allocates no storage. LINE-COUNTER has no such register: SR3 bars it from the receiving
    /// side, and the sending side of both counters is <c>BoundReportCounterRef</c>.</summary>
    public required DataItem PageCounterRegister { get; init; }
}

/// <summary>One report group description entry that carries a data-name (ISO §13.15.3 SR7's "entry-name"):
/// <paramref name="Name"/>, the names of the levels it is subordinate to INNERMOST FIRST ending at its 01 report group
/// (a null for an unnamed level — FILLER — as <see cref="ReportEntryFamily.Qualification"/> keeps them), and whether the
/// entry contains a SUM clause and so names a sum counter (§13.18.54.4 GR5). Group entries, intermediate groups and
/// the 01 itself are here as well as the printable items: all of them are "report items" in §13.8.6.2.3's sense.</summary>
public sealed record ReportNamedEntry(string Name, IReadOnlyList<string?> Qualification, bool IsSumCounter)
{
    /// <summary>The named entries of <paramref name="written"/> (a report's entries in source order), each with the
    /// level stack above it — the same level-number walk <c>BindReportEntry</c> keeps for its chain.</summary>
    internal static IReadOnlyList<ReportNamedEntry> ReadAll(IReadOnlyList<Core.ReportGroupEntryContext> written)
    {
        var entries = new List<ReportNamedEntry>();
        var above = new List<(int Level, string? Name)>();
        foreach (var ge in written)
        {
            int.TryParse(ge.levelNumber().GetText(), out int level);
            while (above.Count > 0 && above[^1].Level >= level) above.RemoveAt(above.Count - 1);
            string? name = ge.dataName().NameOrNull();
            if (name is not null)
                entries.Add(new ReportNamedEntry(name, [.. Enumerable.Reverse(above).Select(a => a.Name)],
                    ge.reportGroupClause().Any(c => c.reportSumClause() is not null)));
            above.Add((level, name));
        }
        return entries;
    }
}

/// <summary>The RD CODE clause (ISO §13.18.12.2 — <c>CODE IS {literal-1 | identifier-1}</c>): the characters "automatically
/// placed in the first characters of each logical record written to the report file for this report" (GR1), which are
/// "not included in the descriptions of the lines in the report, but are included in the logical record size" (GR2).
/// Exactly one of <see cref="Literal"/> (an alphanumeric literal's characters) and <see cref="Operand"/> (an
/// identifier, evaluated by the engine at each body group — GR3) is set.</summary>
public sealed class ReportCodeModel
{
    /// <summary>literal-1's characters, decoded (concatenation folded, hexadecimal format decoded); null for identifier-1.</summary>
    public string? Literal { get; init; }

    /// <summary>identifier-1 AS WRITTEN — base word, qualifiers, subscripts and reference modification (§13.18.12.2) —
    /// the SOURCE clause's own written-reference form (kb/Work PB1129 × PB1292); null for literal-1. The BASE item it
    /// names is <see cref="Item"/> (the data-phase SR2 screen's subject) and the value the engine reads is
    /// <see cref="Value"/> (procedure phase, the ONE sending-operand resolution).</summary>
    public Core.DataReferenceContext? Reference { get; init; }

    /// <summary>The clause's operand as written, for diagnostics.</summary>
    public required string Written { get; init; }

    /// <summary>How many character positions the code occupies at the start of every logical record — literal-1's
    /// length, or identifier-1's (fixed: SR2 excludes every variable-length shape). 0 until resolved.</summary>
    public int Length { get; set; }

    /// <summary>The resolved base item of identifier-1 (data bind); null for literal-1 or an unresolved operand.</summary>
    public DataItem? Item { get; set; }

    /// <summary>identifier-1 as the place the engine reads at each evaluation (§13.18.12.4 GR3), resolved in the
    /// procedure phase; null for literal-1 or a refused operand.</summary>
    public Place? Value { get; set; }
}

/// <summary>One CONTROL clause operand (ISO §13.18.16): FINAL or a (possibly qualified) data-name resolved
/// post-build to its item.</summary>
public sealed class ReportControlModel
{
    public bool IsFinal { get; init; }
    public ReportControlRef? Operand { get; init; }
    public DataItem? Item { get; set; }

    /// <summary>The operand as written, for diagnostics and emitted comments (FINAL has no data reference).</summary>
    public string Display => IsFinal ? "FINAL" : Operand?.ToString() ?? "?";
}

/// <summary>
/// ONE operand of the CONTROL clause AS WRITTEN (ISO §13.18.16.3): the data-name, its IN/OF qualifier words in
/// written order (§8.4.2.2), and — SR4 — the optional reference modification, whose leftmost-position and length
/// are integer literals.
/// <para>⛔ THE SAME WRITTEN REFERENCE IS HOW TWO OTHER CLAUSES NAME A CONTROL LEVEL, so the "is this one of the
/// CONTROL clause's operands" test is written down ONCE, here (kb/Work PB205): §13.18.57.3 SR10 — "Data-name-1
/// and data-name-2 may be qualified and reference-modified. If data-name-1 or data-name-2 is reference-modified,
/// leftmost-position and length shall be integer literals. Each data-name-1, data-name-2, and FINAL, if
/// specified, shall be the same as one of the operands of the CONTROL clause of the corresponding report
/// description entry." — and §13.18.54.3 SR8, the same sentence for SUM … RESET ON data-name-3. All three sites
/// previously kept only the base word (TYPE, RESET) or the base word plus qualifiers (CONTROL) and compared by
/// NAME, so <c>CX(1:3)</c> and <c>CX(4:3)</c> were the same operand and the minor control footing silently bound
/// to the major level — the three-arm form of this repo's two-arm-dispatch defect.</para>
/// <para>Equality is over the WRITTEN reference, which is what SR6 makes unique ("Data-name-1 shall be unique in
/// any given CONTROL clause") — and unique it must be TEXTUALLY, because SR6's own second sentence permits two
/// operands to "refer to the same physical data item or to overlapping data items", so the referenced item
/// cannot be what distinguishes them.</para>
/// </summary>
/// <param name="Name">The base data-name.</param>
/// <param name="Qualifiers">The IN/OF qualifier words, innermost first (§8.4.2.2).</param>
/// <param name="RefModStart">The reference modification's leftmost-position integer literal; null when the
/// operand is not reference-modified.</param>
/// <param name="RefModLength">Its length integer literal; null both when there is no ref-mod and for
/// §8.4.3.3.2's omitted, bracketed length (the slice then runs to the rightmost position, §8.4.3.3.4 GR5c).</param>
public sealed record ReportControlRef(
    string Name, IReadOnlyList<string> Qualifiers, int? RefModStart, int? RefModLength)
{
    /// <summary>The identity ISO §13.18.57.3 SR10 requires — "the same as one of the operands of the CONTROL
    /// clause" — which §13.18.54.3 SR8 asks of its own operand in its own words ("Data-name-3 or FINAL shall be
    /// an operand of the CONTROL clause of the current report description"). The record's synthesized equality
    /// cannot serve — <see cref="Qualifiers"/> is a list, compared by reference — and COBOL words are
    /// case-insensitive (§8.2), so the test is spelled out.</summary>
    public bool SameOperandAs(ReportControlRef other)
    {
        if (!CobolNames.Same(Name, other.Name)) return false;
        if (RefModStart != other.RefModStart || RefModLength != other.RefModLength) return false;
        if (Qualifiers.Count != other.Qualifiers.Count) return false;
        for (int i = 0; i < Qualifiers.Count; i++)
            if (!CobolNames.Same(Qualifiers[i], other.Qualifiers[i])) return false;
        return true;
    }

    /// <summary>The operand as written — the form every diagnostic about it quotes.</summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder(Name);
        foreach (var q in Qualifiers) sb.Append(" OF ").Append(q);
        if (RefModStart is { } s) sb.Append('(').Append(s).Append(':').Append(RefModLength?.ToString() ?? "").Append(')');
        return sb.ToString();
    }
}

/// <summary>A report group's TYPE (ISO §13.18.57 Format 2).</summary>
public enum ReportGroupKindModel { ReportHeading, PageHeading, ControlHeading, Detail, ControlFooting, PageFooting, ReportFooting }

/// <summary>One report group description (ISO §13.15): its 01-level name (a detail's GENERATE handle,
/// §14.9.16 SR1), TYPE, CH/CF control association, and the LINE-built report lines.</summary>
public sealed class ReportGroupModel
{
    public string? Name { get; set; }
    public ReportGroupKindModel Kind { get; set; } = ReportGroupKindModel.Detail;

    /// <summary>The level 1 entry that opened the group — where a diagnostic about the group as a whole stands
    /// (§13.18.57.3 SR13–SR15, <c>ScreenReportGroupCensus</c>).</summary>
    public CobolParserCore.ReportGroupEntryContext? Entry { get; init; }

    /// <summary>The CH/CF control operand as written (§13.18.57.3 SR10 — qualifiable AND reference-modifiable);
    /// null when omitted (legal only with a one-operand CONTROL clause, SR11), for FINAL, or for a non-control
    /// group.</summary>
    public ReportControlRef? ControlOperand { get; set; }
    public bool ControlFinal { get; set; }

    /// <summary>A control heading written with the OR PAGE phrase (§13.18.57.2): it is printed "in addition after each
    /// page advance, following any page heading" (§13.18.57.4 GR6 c)) — the engine's <c>ReportGroup.OrPage</c>.</summary>
    public bool OrPage { get; set; }

    /// <summary>The resolved control LEVEL (the index into <see cref="ReportModel.Controls"/>); −1 until
    /// resolved / for non-control groups.</summary>
    public int ControlLevel { get; set; } = -1;

    public List<ReportLineModel> Lines { get; } = [];

    /// <summary>The group's NEXT GROUP clause as written on its level 1 entry (ISO §13.18.37; §13.15.3 SR6), null
    /// when none — captured during the entry walk because the TYPE clause the syntax rules depend on may follow it
    /// in the same entry, and bound by <c>BindNextGroupClauses</c> once the group is complete.</summary>
    public CobolParserCore.ReportNextGroupClauseContext? NextGroupClause { get; set; }

    /// <summary>The bound NEXT GROUP clause — the runtime's own record, which the emitter writes verbatim, so the
    /// engine and the model cannot disagree about its shape (ISO §13.18.37.2).</summary>
    public WiseOwl.Cobol.Runtime.IO.ReportNextGroup? NextGroup { get; set; }
}

/// <summary>The LINE clause form of one report line (ISO §13.18.35; the NEXT PAGE phrase is a flag on the line,
/// <see cref="ReportLineModel.NextPage"/>, not a kind) — plus the STEP placement a later occurrence of a VERTICALLY repeating entry takes (§13.18.38.4 GR12c/GR12d).
/// The names and the meanings are the runtime <c>ReportLineKind</c>'s; the model is what the emitter copies.</summary>
public enum ReportLineKindModel { Absolute, Relative, Step }

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

    /// <summary>The WRITTEN report group description entry whose LINE clause opened this line — the same entry for
    /// every replay of a repeating entry, so the line-set rules of §13.18.35.3 SR6 report once per pair of entries.
    /// Null only before <c>BindReportEntry</c> has stamped the line it made.</summary>
    public CobolParserCore.ReportGroupEntryContext? Entry { get; set; }

    /// <summary>This line's step-anchor slot (ISO §13.18.38.4 GR12c/GR12d), 0 when the line neither seeds one
    /// nor steps from one. A non-Step line with an anchor SEEDS it with the page line it lands on; a
    /// <see cref="ReportLineKindModel.Step"/> line places at anchor + <see cref="Value"/>.</summary>
    public int Anchor { get; init; }

    /// <summary>A Step line's own written integer-2 — the fallback when the first occurrence of this line was
    /// absent under a PRESENT WHEN clause, so its anchor was never seeded (§13.18.41.4 GR2b).</summary>
    public int RelativeBase { get; init; }

    /// <summary>A Step line's §13.18.35.4 GR4c page-fit contribution: integer-3 when this line OPENS an
    /// occurrence, 0 otherwise — "the vertical interval between successive occurrences is added into the trial
    /// sum once for each occurrence beyond the first". Absolute and relative lines compute their own.</summary>
    public int TrialInterval { get; init; }

    /// <summary>The line's LINE clause carries the NEXT PAGE phrase (ISO §13.18.35.2 Format 1 — <c>integer-1 ON
    /// NEXT PAGE</c>, or the bare <c>ON NEXT PAGE</c> operand). Set on the FIRST occurrence only: §13.18.35.3 SR7
    /// allows the phrase only in the group's first LINE clause, and SR10a only with a multiple LINE clause's first
    /// operand, so a later occurrence of the same clause places as the phrase-less form. The engine reads it on
    /// the group's first PRESENT line (§13.18.35.4 GR4a / GR5a).</summary>
    public bool NextPage { get; init; }

    /// <summary>The PRESENT WHEN condition chain (01 → line entry) as captured parse contexts (§13.18.41).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    /// <summary>The bound chain (AND-composed by the emitter); parallel to <see cref="PresentWhenCtxs"/>.</summary>
    public List<BoundCondition> PresentWhen { get; } = [];

    /// <summary>The OCCURS … DEPENDING presence tests this LINE inherits (ISO §13.18.38.4 GR13), outermost
    /// repeating entry first — empty unless the line lies inside a VERTICALLY repeating entry with the
    /// DEPENDING phrase. GR13 makes such an OCCURS "have the same effect as an OCCURS clause with no TO or
    /// DEPENDING phrases and with an integer-2 equal to the current value of data-name-1", so a repetition past
    /// that count does not EXIST — on the vertical axis that means its whole report line is absent, not blank.
    /// The emitter composes it into the same delegate the PRESENT WHEN chain feeds, which is what makes
    /// §13.18.35.4 GR4c ("If any of the LINE clauses used in computing the trial sum are subject to a PRESENT
    /// WHEN clause or to an OCCURS clause with the DEPENDING phrase, these clauses are taken into account")
    /// true for both suppressors at once.</summary>
    public List<ReportRepetitionGuard> RepetitionGuards { get; } = [];
}

/// <summary>How ONE placement of a printable item fixes its leftmost column.</summary>
public enum ReportColumnKindModel
{
    /// <summary>COLUMN NUMBER integer-1 — the leftmost character stands in that column (ISO §13.18.14.4 GR1/GR2).</summary>
    Absolute,
    /// <summary>COLUMN PLUS integer-2 — integer-2 columns right of the line's horizontal counter (GR8).</summary>
    Relative,
    /// <summary>The FIRST repetition of a STEP'd repeating entry (ISO §13.18.38.4 GR12): placed like
    /// <see cref="Relative"/>, and its leftmost column is REMEMBERED in the entry's step anchor.</summary>
    AnchorSeed,
    /// <summary>A later repetition of a STEP'd repeating entry: "integer-3 columns to the right of the column
    /// they occupy in the preceding occurrence" (ISO §13.18.38.4 GR12b) — the anchor advanced by integer-3. The
    /// anchor, not the horizontal counter, is the datum, because GR12 measures from the PRECEDING OCCURRENCE'S
    /// LEFTMOST while the counter holds the last placed item's RIGHTMOST (§13.18.14.4 GR9).</summary>
    AnchorStep,
}

/// <summary>The COLUMN clause's alignment word (ISO §13.18.14.2 Format 1, §13.18.14.4 GR6 b)–d)): which edge of the
/// printable item integer-1 names. LEFT is assumed when none is written and an operand is absolute (§13.18.14.3 SR9).</summary>
public enum ReportColumnAlignment { Left, Center, Right }

/// <summary>One placement of a printable item: a COLUMN clause operand (ISO §13.18.14 Format 1) or, for a
/// repetition of a STEP'd repeating entry, its step-anchor placement (§13.18.38.4 GR12).</summary>
/// <param name="Kind">Which datum fixes the leftmost column.</param>
/// <param name="Value">integer-1 / integer-2 / integer-3, per <paramref name="Kind"/>.</param>
/// <param name="AnchorId">The step anchor's compose-local register id; 0 for the two COLUMN-clause kinds.</param>
/// <param name="Alignment">Which edge of the item <paramref name="Value"/> names (§13.18.14.4 GR6 b)–d)); only an
/// <see cref="ReportColumnKindModel.Absolute"/> operand can carry anything but the default (§13.18.14.3 SR9).</param>
public readonly record struct ReportColumnSpec(
    ReportColumnKindModel Kind, int Value, int AnchorId = 0, ReportColumnAlignment Alignment = ReportColumnAlignment.Left)
{
    /// <summary>A COLUMN clause operand as written: absolute, or relative (PLUS), with the clause's alignment word
    /// (<see cref="ReportColumnAlignment.Left"/> when none is written — §13.18.14.3 SR9).</summary>
    public ReportColumnSpec(bool relative, int value, ReportColumnAlignment alignment = ReportColumnAlignment.Left)
        : this(relative ? ReportColumnKindModel.Relative : ReportColumnKindModel.Absolute, value, 0, alignment) { }

    /// <summary>⛔ THE ONE LEFTMOST-COLUMN COMPUTATION of an ABSOLUTE operand (ISO §13.18.14.4 GR6 b)–d)), read by
    /// the compose emitter's placement and by the bind-time line-width walk, so the two cannot place an item in
    /// different columns (kb/Work PB1220). LEFT: integer-1. RIGHT: integer-1 − printable-size + 1. CENTER, odd
    /// printable-size: integer-1 − ((printable-size − 1) / 2); even: (integer-1 − (printable-size / 2)) + 1 — both
    /// are integer-1 − (printable-size − 1) / 2 in truncating integer arithmetic.</summary>
    public int AbsoluteLeftmost(int printableSize) => Alignment switch
    {
        ReportColumnAlignment.Right => Value - printableSize + 1,
        ReportColumnAlignment.Center => Value - (printableSize - 1) / 2,
        _ => Value,
    };

    /// <summary>The rightmost column of an ABSOLUTE operand — GR6 b) integer-1 + printable-size − 1 for LEFT,
    /// integer-1 for RIGHT, integer-1 + (printable-size / 2) for CENTER — which GR9 makes the horizontal counter.
    /// It is the leftmost column plus the item's extent in every case (checked against GR6 d) 1. and 2.).</summary>
    public int AbsoluteRightmost(int printableSize) => AbsoluteLeftmost(printableSize) + printableSize - 1;

    /// <summary>True when the placement is NOT a fixed column number — i.e. the line needs the §13.18.14.4 GR7
    /// horizontal counter. <see cref="ReportColumnKindModel.AnchorSeed"/> reads the counter; its
    /// <see cref="ReportColumnKindModel.AnchorStep"/> siblings read the anchor the seed wrote, and a step can
    /// never appear on a line without its seed, so grouping all three here is exact.</summary>
    public bool Relative => Kind is not ReportColumnKindModel.Absolute;
}

/// <summary>
/// A report group description entry's OCCURS clause — ISO §13.18.38 FORMAT 3 (report-writer):
/// <c>OCCURS [ integer-1 TO ] integer-2 TIMES [ DEPENDING ON data-name-1 ] [ STEP integer-3 ]</c>.
/// <para>The entry is a REPEATING ENTRY: §13.18.38.4 GR10 makes it "define integer-2 distinct report items",
/// GR11 gives every repetition the same PICTURE/VALUE/JUSTIFIED/BLANK WHEN ZERO/GROUP INDICATE effect, and GR12
/// makes the STEP phrase the interval between successive occurrences. The binder REPLAYS the entry's subtree
/// once per repetition (REPORT_WRITER_DESIGN §3.4), which is what §13.18.63.4 GR21's import of GR9
/// ("causes every occurrence of the associated data item to be assigned the specified value") asks for.</para>
/// </summary>
/// <param name="Min">integer-1 (the DEPENDING minimum); 0 when the TO phrase is absent.</param>
/// <param name="Max">integer-2 — the number of repetitions the entry defines.</param>
/// <param name="DependingName">data-name-1 of the DEPENDING phrase, else null.</param>
/// <param name="DependingQualifiers">its IN/OF qualifier words (§8.4.2.2).</param>
/// <param name="Step">integer-3 of the STEP phrase, else null.</param>
public sealed record ReportOccursSpec(
    int Min, int Max, string? DependingName, IReadOnlyList<string> DependingQualifiers, int? Step)
{
    /// <summary>data-name-1, resolved post-build (the SOURCE-operand pattern) — the §13.18.38.4 GR13 count.</summary>
    public DataItem? DependingItem { get; set; }

    /// <summary>⛔ THE AXIS DECIDES WHICH HALF OF GR10 AND GR12 APPLIES, AND A STEP DISPLACES ON THAT AXIS ONLY.
    /// §13.18.38.4 GR10a/GR10b and GR12a/GR12b are the COLUMN (horizontal) arms; GR10c/GR10d and GR12c/GR12d are
    /// the LINE (vertical) arms. An entry that contains, or has subordinate to it, a LINE clause repeats
    /// VERTICALLY; every other repeating entry repeats horizontally. Writing it down once is what keeps a
    /// vertical entry's integer-3 out of the horizontal displacement and vice versa.</summary>
    public ReportRepetitionAxis Axis { get; init; } = ReportRepetitionAxis.Horizontal;

    /// <summary>Where data-name-1 was written — its post-build resolution reports there.</summary>
    public Editions.DiagnosticCursor DependingAt { get; init; }
}

/// <summary>The axis a report-group repeating entry repeats on (ISO §13.18.38.4 GR10/GR12; see
/// <see cref="ReportOccursSpec.Axis"/>).</summary>
public enum ReportRepetitionAxis { Horizontal, Vertical }

/// <summary>
/// ONE repetition's presence test for a repeating entry with the DEPENDING phrase (ISO §13.18.38.4 GR13 with
/// §13.18.63.4 GR22 — "The value of literal-1 is used for the content of the printable item whenever it is
/// printed, except that a GROUP INDICATE, PRESENT WHEN, or OCCURS clause with the DEPENDING phrase may suppress
/// the appearance of the item").
/// <para>GR13: data-name-1 "is evaluated just before the processing for the first LINE clause of the report
/// group. If the value of data-name-1 is not in the range integer-1 to (integer-2 - 1), the report group is
/// processed as though the OCCURS clause had been written without the TO and DEPENDING phrases" — i.e. all
/// integer-2 repetitions appear — "If the value of data-name-1 is in the range integer-1 to (integer-2 - 1), the
/// OCCURS clause has the same effect as an OCCURS clause with no TO or DEPENDING phrases and with an integer-2
/// equal to the current value of data-name-1" — i.e. that many appear. So repetition <paramref name="Ordinal"/>
/// is present exactly when <c>Ordinal &lt; (inRange ? data-name-1 : integer-2)</c>.</para>
/// <para>⛔ Suppression here is NOT absence for VALUE purposes: §13.18.63.4 GR23's last sentence — "If any of the
/// printable items are suppressed as a result of a PRESENT WHEN clause or an OCCURS clause with a DEPENDING
/// phrase, VALUE operands are nevertheless assigned to them, even though they are not printed" — which the
/// bind-time replay satisfies by construction: every repetition is bound, only its PLACEMENT is guarded.</para>
/// </summary>
/// <param name="Spec">The repeating entry's OCCURS clause.</param>
/// <param name="Ordinal">This repetition's zero-based ordinal within that entry.</param>
public sealed record ReportRepetitionGuard(ReportOccursSpec Spec, int Ordinal);

/// <summary>One report VARYING counter (ISO §13.18.64) — the temporary integer data item an ENTRY containing a
/// VARYING clause establishes (GR1). It is declared by its ENTRY, not by the printable leaf that happens to sit
/// under it, so a group entry's counter exists and a subordinate entry can name it (§13.18.64.3 SR2: "This
/// definition of data-name-1 may be referenced only within the current entry or a subordinate entry"; SR3 lets a
/// subordinate entry's FROM and BY name it too). One instance is made per WRITTEN entry per enclosing repetition,
/// because GR3's "first occurrence" restarts with every occurrence of an enclosing repeating entry
/// (kb/Work PB1306): its occurrences are the repetitions of the declaring entry within ONE enclosing occurrence.
/// <para>The counter name, the FROM/BY expressions as captured parse contexts (bound to <see cref="From"/>/
/// <see cref="By"/> in the procedure phase through the ONE expression binder, with <see cref="Enclosing"/> and
/// <see cref="Group"/> in scope; null = the GR3 default 1).</para></summary>
public sealed class ReportVaryingModel
{
    public required string Name { get; init; }
    public CobolParserCore.ArithmeticExpressionContext? FromCtx { get; init; }
    public CobolParserCore.ArithmeticExpressionContext? ByCtx { get; init; }
    public BoundExpr? From { get; set; }
    public BoundExpr? By { get; set; }

    /// <summary>The compilation-unique identity of the counter, the ordinal of its compose-local <see cref="Local"/>.</summary>
    public required int Uid { get; init; }

    /// <summary>The compose-local holding the counter, by its identity (<see cref="MethodLocal"/>, kb/Work PB4321): the
    /// local a reference to this counter reads (<see cref="BoundReportVaryingRef.Counter"/>), named by the backend that
    /// declares it.</summary>
    public MethodLocal Local => new StatementLocal(StatementLocalRole.ReportVaryingCounter, Uid);

    /// <summary>The report group description entry whose VARYING clause declares the counter.</summary>
    public required CobolParserCore.ReportGroupEntryContext Entry { get; init; }

    /// <summary>Every counter of the declaring entry, in clause order (§13.18.64.2 — one clause names several).</summary>
    public IReadOnlyList<ReportVaryingModel> Group { get; set; } = [];

    /// <summary>The counters of the entries ENCLOSING the declaring entry, outermost first — the names §13.18.64.3 SR3
    /// lets this counter's FROM and BY reference ("arithmetic-expression-1 or arithmetic-expression-2 of a VARYING
    /// clause in a subordinate entry").</summary>
    public IReadOnlyList<ReportVaryingModel> Enclosing { get; init; } = [];

    /// <summary>The BY expression names a counter of the SAME entry (§13.18.64.3 SR3: "may be referenced in
    /// arithmetic-expression-2 of the same VARYING clause"), so occurrence n is not FROM + n × BY: each step adds a
    /// BY evaluated against the counters as the previous step left them (GR3 b), a running recurrence.</summary>
    public bool Recurrent { get; init; }

    /// <summary>The FROM/BY expressions have been bound (they are shared by every field the counter reaches).</summary>
    public bool Bound { get; set; }
}

/// <summary>A VARYING counter as one printable field sees it: the counter, and the occurrence number of its DECLARING
/// entry this field's placement lies in (§13.18.64.4 GR3 — the first occurrence takes FROM, each later one adds BY).
/// <paramref name="PerPlacement"/> is set when the field's OWN entry declares the counter and repeats through a
/// multiple COLUMN clause: placement <c>j</c> of the field is then occurrence <c>Ordinal + j</c>. Every other use is
/// one fixed occurrence — the field lies inside ONE occurrence of the declaring entry.</summary>
public sealed record ReportVaryingUse(ReportVaryingModel Counter, int Ordinal, bool PerPlacement);

/// <summary>One PRINTABLE item (an entry with a COLUMN clause, ISO §13.18.14): its column operands (one per
/// repetition — a multiple COLUMN clause is a repeating entry, §13.15.4 GR3), the synthetic
/// <see cref="DataItem"/> carrying its PICTURE/JUSTIFIED/BLANK WHEN ZERO, its value-source OPERAND LIST, the
/// GROUP INDICATE flag (§13.18.28), its field-local PRESENT WHEN chain (the conditions BELOW the line entry —
/// the line's own chain already gates the whole line), and the entry's VARYING counters (§13.18.64).</summary>
public sealed class ReportFieldModel
{
    public required IReadOnlyList<ReportColumnSpec> Columns { get; init; }
    public required DataItem PrintItem { get; init; }

    /// <summary>The WRITTEN report group description entry this field is a placement of — one entry for every
    /// replay of a repeating entry. The arrangement rules of the line (§13.18.14.3 SR7, SR8) are about entries as
    /// written, so they report once per pair of entries and skip two placements of the same one.</summary>
    public CobolParserCore.ReportGroupEntryContext? Entry { get; init; }

    /// <summary>⛔ THE VALUE / SOURCE OPERAND LIST — ONE ENTRY PER WRITTEN OPERAND, NEVER A SCALAR (kb/Work
    /// PB506). ISO §13.18.63.2 format 4 is <c>{ literal-1 } …</c> and §13.18.53.2 is
    /// <c>{ identifier-1 / arithmetic-expression-1 } …</c>: BOTH clauses take an operand LIST, and the standard
    /// then writes the same two rules twice — §13.18.63.3 SR35 / §13.18.53.3 SR6 (a multi-operand clause
    /// requires a repeating entry and an operand count that matches its repetitions) and §13.18.63.4 GR23 /
    /// §13.18.53.4 GR4 ("successive operands are assigned to successive repeating printable items, horizontally
    /// and then vertically, as applicable, in that hierarchy. If no further operands remain, assignment begins
    /// again from the first operand"). A single-operand clause is a one-element list, so the cycling rule below
    /// is the ONE reader for both shapes and the model cannot answer "which operand prints in repetition n"
    /// two different ways.</summary>
    public required IReadOnlyList<ReportFieldSource> Sources { get; init; }

    /// <summary>The entry carries a GROUP INDICATE clause — ISO §13.18.28.4 GR1: "the same effect as a PRESENT
    /// WHEN clause" whose condition is the report engine's per-detail-group state, so the emitter ANDs it into
    /// this item's presence test beside <see cref="PresentWhen"/> (kb/Work PB1244).</summary>
    public bool GroupIndicate { get; init; }

    /// <summary>The operand that supplies repetition <paramref name="rep"/> (0-based) — ISO §13.18.63.4 GR23 /
    /// §13.18.53.4 GR4: successive operands to successive repeating printable items, wrapping to the first when
    /// no further operands remain. GR23's last sentence ("If any of the printable items are suppressed … VALUE
    /// operands are nevertheless assigned to them") is honoured BY CONSTRUCTION: the index is the repetition
    /// ORDINAL, never a count of the items actually placed, so a PRESENT WHEN that suppresses the entry cannot
    /// shift the assignment.</summary>
    public ReportFieldSource SourceAt(int rep) => Sources[rep % Sources.Count];

    /// <summary>PRESENT WHEN conditions strictly below the line entry, down to this entry (§13.18.41.4 GR2b).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    public List<BoundCondition> PresentWhen { get; } = [];

    /// <summary>The VARYING counters in scope at this field (§13.18.64.3 SR2) — those of its own entry and of every
    /// entry above it, outermost first, each with the occurrence of its declaring entry the field lies in. Empty
    /// outside any VARYING entry.</summary>
    public List<ReportVaryingUse> Varyings { get; } = [];

    /// <summary>The OCCURS … DEPENDING presence tests this placement inherits, outermost repeating entry first
    /// (ISO §13.18.38.4 GR13 / §13.18.63.4 GR22) — empty unless the item lies inside a repeating entry with the
    /// DEPENDING phrase. A nested repetition contributes one guard per enclosing level; all shall hold.</summary>
    public List<ReportRepetitionGuard> RepetitionGuards { get; } = [];

    /// <summary>
    /// How many PLACEMENTS of this entry precede this field's first one — the ordinal of its first repeating
    /// printable item, so placement <c>j</c> of this field is repetition <c>RepetitionOrdinal + j</c> of the
    /// entry. An entry repeats through a multiple COLUMN clause (the operands of ONE field) and/or through an
    /// OCCURS clause (§13.18.38 Format 3 — a REPLAY producing one field per repetition), and the two compose, so
    /// the ordinal has to be counted per entry rather than read off either vehicle.
    /// <para>It is what ISO §13.18.64.4 GR3 counts ("For the first occurrence, the value of
    /// arithmetic-expression-1 is moved to data-name-1 … For the second and subsequent occurrences, the value of
    /// arithmetic-expression-2 is added"), and what §13.18.63.4 GR23 counts when a VALUE clause has more than one
    /// operand ("successive operands are assigned to successive repeating printable items").</para>
    /// </summary>
    public int RepetitionOrdinal { get; init; }
}

/// <summary>A printable item's value source, by clause kind.</summary>
public abstract record ReportFieldSource;

/// <summary>ONE format-4 VALUE clause operand (raw operand text — figurative word or literal; ISO §13.18.63.2
/// format 4). A multi-operand clause contributes one of these PER OPERAND to
/// <see cref="ReportFieldModel.Sources"/> — the raw text of the operands is never glued together.</summary>
public sealed record FieldValueSource(string Raw) : ReportFieldSource;

/// <summary>A SOURCE clause <b>identifier-1</b> (ISO §13.18.53.2), kept as the WRITTEN REFERENCE and bound in the
/// PROCEDURE phase through the ONE operand binder a MOVE's sending operand takes (<c>ExpressionBinder.FieldOperand</c>)
/// — which is §13.18.53.4 GR1's "sending operand of an implicit MOVE statement" said as code. That one binder is the
/// only place that knows every shape an identifier-1 can have: a subscript, a reference-modification, a LINE-COUNTER
/// or PAGE-COUNTER of this or another report (§8.4.2.2.3 SR9/SR10), a sum counter of the current report (§13.18.53.3
/// SR4), a VARYING counter in scope (§13.18.64.3 SR2) and a constant-name (§13.10.3 SR2). (kb/Work PB1292 × PB1306 ×
/// PB1316 × PB1456 — before it the identifier arm captured name + qualifiers at data bind and looked the name up in
/// ordinary storage, so each of those shapes was a separate staged refusal of legal source.)
/// <para><see cref="Value"/> is null until that bind; a screen that already named its rule sets
/// <see cref="Rejected"/>, so the rejected words are never bound or reported twice (the
/// <see cref="FieldComputeSource.Rejected"/> discipline).</para></summary>
public sealed record FieldReferenceSource(Core.DataReferenceContext Ref, string Written) : ReportFieldSource
{
    /// <summary>The bound sending operand (procedure phase).</summary>
    public BoundOperand? Value { get; set; }

    /// <summary>A screen has already rejected this operand and named its rule.</summary>
    public bool Rejected { get; set; }
}

/// <summary>A SOURCE clause operand that is an <b>arithmetic-expression-1</b> (ISO §13.18.53.2), or an
/// identifier-1 written WITH the ROUNDED phrase — §13.18.53.3 SR5: "If identifier-1 is specified with the
/// ROUNDED phrase, it is considered to be an arithmetic-expression." Its rule is §13.18.53.4 GR2:
/// "Arithmetic-expression-1 specifies the operand of an implicit COMPUTE statement that is executed implicitly
/// whenever the associated item is printed. If the ROUNDED phrase is specified, the implicit COMPUTE statement
/// has the corresponding ROUNDED phrase." (kb/Work PB852.)
/// <para>The operand is kept as its PARSE TREE and bound in the PROCEDURE phase through the ONE expression
/// binder — the same route the SUM addend takes since kb/Work PB482, and for the same reason: a subscript may
/// be an index-name or an expression and has no value at data bind.</para></summary>
public sealed record FieldComputeSource(Core.ReportValueOperandContext Ctx, string Written) : ReportFieldSource
{
    /// <summary>The clause's ROUNDED phrase (§13.18.53.2's trailing <c>[ rounded-phrase ]</c>, §14.7.4), or null
    /// — in which case GR2's implicit COMPUTE has no ROUNDED phrase and the transfer truncates (§14.7.4.3 r2).</summary>
    public Core.RoundedPhraseContext? Rounded { get; init; }

    /// <summary>The bound expression (procedure phase); null when a screen rejected the operand.</summary>
    public BoundExpr? Value { get; set; }

    /// <summary>A screen has already rejected this operand and named its rule: no binding, no emission, and no
    /// second diagnostic about the same words (the <c>ReportSumAddend.Rejected</c> discipline).</summary>
    public bool Rejected { get; set; }

    /// <summary>The rounding mode the ROUNDED phrase selects (§14.7.4.3), resolved in the procedure phase
    /// through the ONE <c>ExpressionBinder.RoundingOf</c>.</summary>
    public WiseOwl.Cobol.Runtime.CobolRounding Rounding { get; set; } = WiseOwl.Cobol.Runtime.CobolRounding.Truncation;
}

/// <summary>The printable face of a SUM entry (ISO §13.18.54.4 GR4 — the sum counter acts as the source item).
/// <paramref name="CounterId"/> is the counter's ENTRY ORDINAL (GR1; <see cref="ReportSumModel.Id"/>), never a
/// data-name — two entries may legally carry the same one (kb/Work PB882).</summary>
public sealed record FieldSumSource(int CounterId) : ReportFieldSource;

/// <summary>ONE SUM addend written as <c>identifier-1</c> (ISO §13.18.54.3 SR1 — "Each data-name-1,
/// identifier-1 or arithmetic-expression-1 is an addend"), kept as the WRITTEN REFERENCE rather than a resolved
/// item. §8.4.3.1.2 Format 2 makes an identifier a <i>qualified-data-name-with-subscripts</i>, so the subscript
/// is part of the reference and can be evaluated only where the ordinary identifier machinery lives — the
/// procedure phase. <see cref="Ctx"/> is bound there through the ONE expression binder into <see cref="Value"/>;
/// <see cref="Name"/>/<see cref="Qualifiers"/> answer the DATA-phase question (which arm of SR4/SR5 this is)
/// and <see cref="Item"/> is what that lookup found.
/// <para>⛔ Before kb/Work PB482 the addend was captured by <c>KeyReference</c> — base word + qualifiers, with
/// the subscript and the reference modification dropped on the floor — so <c>SUM WS-CELL(2)</c> compiled and
/// then ABORTED at run time in the addend delegate, and <c>SUM WS-TXT(1:2)</c> silently summed the whole
/// item.</para></summary>
public sealed class ReportSumAddend
{
    /// <summary>The operand's parse tree — a <c>reportValueOperand</c>, i.e. an arithmetic expression whose
    /// degenerate case is the bare identifier form. Bound through the ONE <c>ExpressionBinder.BindExpr</c>.</summary>
    public required CobolParserCore.ReportValueOperandContext Ctx { get; init; }

    /// <summary>The bare <c>dataReference</c> this operand IS, when it is written as data-name-1 / identifier-1
    /// (ISO §13.18.54.3 SR1's first two addend forms); null when the operand is an arithmetic-expression-1, whose
    /// rules are SR6's rather than SR4/SR5's (kb/Work PB883).</summary>
    public CobolParserCore.DataReferenceContext? Reference { get; init; }

    /// <summary>True when the addend is written as arithmetic-expression-1 (SR1's third form) — the
    /// §13.18.54.4 GR3 COMPUTE-with-ON-SIZE-ERROR accumulation rather than GR3's ADD.</summary>
    public bool IsExpression => Reference is null;

    public required string Name { get; init; }
    public required IReadOnlyList<string> Qualifiers { get; init; }
    /// <summary>The operand exactly as written — what every diagnostic about it quotes.</summary>
    public required string Written { get; init; }
    /// <summary>The item the base name resolves to (the SR5 screen's subject); null when the addend was
    /// rejected or staged, in which case <see cref="Value"/> stays null and the emitter stays loud.</summary>
    public DataItem? Item { get; set; }
    /// <summary>A screen has already rejected this operand and named its rule: no resolution, no binding, no
    /// emission, and above all NO SECOND DIAGNOSTIC about the same words.</summary>
    public bool Rejected { get; set; }
    /// <summary>The addend's value, bound in the procedure phase through <c>ExpressionBinder.BindExpr</c> — the
    /// same route a procedure-division identifier takes, so subscripts (literal, index-name or expression) and
    /// qualification are resolved by the ONE machinery. Null when a screen already rejected the operand.</summary>
    public BoundExpr? Value { get; set; }

    /// <summary>⛔ THIS ADDEND IS DATA-NAME-1 — A ROLLED TOTAL (ISO §13.18.54.3 SR4, kb/Work PB1294): the name of an
    /// entry in the report section, whose value §13.18.54.4 GR6 adds when ITS group is processed (GR7 a)/b)) rather
    /// than when a GENERATE is executed, so it has no <see cref="Value"/> of its own to bind and the emitter's
    /// GENERATE-driven addition leaves it out. Set by the one resolution (<c>ResolveSumAddend</c>).</summary>
    public bool Rolled { get; set; }

    /// <summary>The occurrences of data-name-1 this addend adds into THE COUNTER OCCURRENCE THAT OWNS IT — §13.18.54.4
    /// GR8: "each occurrence of the addend is added into the corresponding occurrence of the sum counter" when both
    /// have the same number of levels of repetition, and when the addend has more, "forming the total of a complete
    /// table of occurrences of the addend at one or more levels into each occurrence of the sum counter". One
    /// addition per occurrence listed; a non-repeating addend is the one occurrence of every counter occurrence.</summary>
    public List<ReportItemOccurrence> RolledFrom { get; } = [];
}

/// <summary>ONE <c>SUM OF addend… [UPON data-name-2…]</c> group of a SUM clause (ISO §13.18.54.2 — the general
/// format's outer brace repeats, and §13.18.54.3 SR1 says so: "The whole clause is referred to as a SUM clause
/// even though the SUM keyword may appear more than once"). The UPON phrase belongs to ITS group: §13.18.54.4
/// GR7c2 adds an addend "whenever any GENERATE statement is executed for a detail referenced by the UPON
/// phrase", so two groups with different UPON lists accumulate on different GENERATEs into the ONE counter
/// §13.18.54.4 GR1 gives the entry.</summary>
public sealed class ReportSumTerm
{
    public List<ReportSumAddend> Addends { get; } = [];
    /// <summary>The group's UPON operands (GR7c2); empty = no UPON phrase (GR7c1 — every GENERATE).</summary>
    public List<ReportDetailRef> Upon { get; } = [];
}

/// <summary>An <c>UPON data-name-2</c> operand as written (ISO §13.18.54.3 SR7: "Data-name-2 shall be the name
/// of a detail. It may be qualified only by a report-name"). The qualifier is a REPORT-name — §8.4.2.2.2
/// Format 1's file-report-qualifier, the same one <c>GENERATE data-name OF report-name</c> writes — so the
/// operand resolves through the ONE report-group funnel, <see cref="ReportGroupResolution"/>, never by a bare
/// name scan. <see cref="Detail"/> is null until that resolution succeeds; a rejected operand stays null and
/// contributes no run-time filter entry.</summary>
public sealed record ReportDetailRef(string Name, string? Qualifier)
{
    public ReportGroupModel? Detail { get; set; }

    /// <summary>The report description the resolved <see cref="Detail"/> belongs to — the RD of the SUM entry itself
    /// for an unqualified operand, ANOTHER report's when SR7's report-name qualifier says so (§13.18.54.4 GR7 c) 2):
    /// the GENERATE that adds is then executed against that report's engine).</summary>
    public ReportModel? Owner { get; set; }

    /// <summary>The operand as written — the form every diagnostic about it quotes.</summary>
    public override string ToString() => Qualifier is null ? Name : $"{Name} OF {Qualifier}";
}

/// <summary>
/// ⛔ ONE WRITTEN REPORT ENTRY THAT CARRIES A VALUE, AND THE GEOMETRY OF ITS REPETITION (kb/Work PB1294). ISO
/// §13.18.54.4 GR6 gives the three kinds of entry a SUM clause's data-name-1 can name one value each — an entry that
/// contains a SUM clause is "the corresponding sum counter", and one that has a SOURCE or VALUE clause is "the operand
/// of the SOURCE or VALUE clause" — so the entry is the unit a rolled total is resolved against, whether it prints
/// (a COLUMN clause) or not (§13.18.53.4 GR3: "the SOURCE clause causes no action, except where the entry is referred
/// to by means of a SUM clause").
/// <para>Its REPETITION geometry is the one a sum counter always had (<see cref="ReportSumFamily"/>, kb/Work PB1271):
/// §13.15.4 GR3's three vehicles — an OCCURS clause, a multiple LINE clause (§13.18.35.4 GR9) and a multiple COLUMN
/// clause (§13.18.14.4 GR12) — are all OCCURS levels, so an entry's occurrences form a table of
/// <see cref="Extents"/>, outermost level first. §13.18.54.4 GR8 maps an addend's occurrences onto a counter's by
/// those levels, which is why both kinds of entry carry them.</para>
/// </summary>
public abstract class ReportEntryFamily
{
    /// <summary>The entry's data-name — for a SUM entry GR5's name of the counter, "not the name of the associated
    /// printable item, if any" — or null for an unnamed entry (such an entry can be named by no clause).</summary>
    public string? Name { get; init; }

    /// <summary>The report description the entry is written in.</summary>
    public required ReportModel Report { get; init; }

    /// <summary>The report group description (the 01-level entry and everything under it) the entry is part of —
    /// §13.18.54.3 SR4 b)/c) ask whether data-name-1 and the subject share one, and SR4 f) asks its TYPE.</summary>
    public required ReportGroupModel Group { get; init; }

    /// <summary>The enclosing repeating entries' OCCURS clauses, outermost first (§13.18.38 Format 3, or a multiple
    /// LINE clause's §13.18.35.4 GR9 equivalent) — one table level each. They also answer what a table(ALL)
    /// argument asks of a level's CURRENT range: §15.3, "If the ALL subscript is associated with a data item
    /// described with an OCCURS DEPENDING ON clause, the range of values is determined by the object of the OCCURS
    /// DEPENDING ON clause", read by §13.18.38.4 GR13's own count (<see cref="Model.AllCount.ReportDepending"/>).</summary>
    public required IReadOnlyList<ReportOccursSpec> Repetitions { get; init; }

    /// <summary>The operand count of the entry's own multiple COLUMN clause — the innermost level — or 1 when the
    /// entry has no multiple COLUMN clause (§13.18.14.3 SR10 a) keeps an OCCURS clause out of such an entry).</summary>
    public required int Columns { get; init; }

    /// <summary>The occurrence count of each level, outermost first: each of <see cref="Repetitions"/>' integer-2,
    /// then <see cref="Columns"/> when it is a level. Empty for a non-repeating entry: one occurrence. Derived, so the
    /// levels, a sum register's synthetic OCCURS chain and the occurrence arithmetic read one answer.</summary>
    public IReadOnlyList<int> Extents =>
        _extents ??= [.. Repetitions.Select(r => r.Max), .. Columns > 1 ? [Columns] : (int[])[]];
    private IReadOnlyList<int>? _extents;

    /// <summary>How many occurrences the entry has — the product of <see cref="Extents"/> (1 for none).</summary>
    public int Count => Extents.Aggregate(1, (n, e) => n * e);

    /// <summary>⛔ THE ENTRY'S QUALIFICATION HIERARCHY (kb/Work PB1454): the data-names of the report group
    /// description entries it is subordinate to, INNERMOST FIRST and ending at the 01 report group, a null for an
    /// entry without a data-name (FILLER). ISO §8.4.2.2.3 SR4 — "Each data-name-2 shall be the name associated with a
    /// level number to which the item being qualified is subordinate" — so an entry is qualifiable by every named
    /// level above it, and by its REPORT as the outermost container (§8.4.2.2.2 Format 1's file-report-qualifier).
    /// Read by <see cref="DataBinder.QualifierWalk"/>, the ONE qualifier walk a data item's own ancestors go through.</summary>
    public IReadOnlyList<string?> Qualification { get; init; } = [];

    /// <summary>The entry's occurrences, one per replay of it and per operand of its own multiple COLUMN clause, in
    /// binding order (which is NOT row-major order: the replay binds siblings between repetitions).</summary>
    public List<ReportItemOccurrence> Occurrences { get; } = [];

    /// <summary>The row-major linear coordinate of <paramref name="coordinates"/> (zero-based, one per
    /// <see cref="Extents"/> level, outermost first) — the order a subscript list is written in (§8.4.2.3.3 SR3,
    /// "successively less inclusive dimensions"), and the arithmetic the engine's <c>CobolReport.SumOccurrence</c>
    /// does for a run-time subscript.</summary>
    public int Linear(IReadOnlyList<int> coordinates)
    {
        int linear = 0;
        for (int k = 0; k < Extents.Count; k++) linear = linear * Extents[k] + coordinates[k];
        return linear;
    }

    /// <summary>The inverse of <see cref="Linear"/>: the zero-based coordinates of occurrence <paramref name="linear"/>.</summary>
    public int[] CoordinatesOf(int linear)
    {
        var coordinates = new int[Extents.Count];
        for (int k = Extents.Count - 1; k >= 0; k--)
        {
            coordinates[k] = linear % Extents[k];
            linear /= Extents[k];
        }
        return coordinates;
    }
}

/// <summary>An entry that carries a SOURCE or VALUE clause and no SUM clause (ISO §13.18.54.4 GR6 — "the value added
/// is that of the operand of the SOURCE or VALUE clause"), printable or not.
/// <see cref="Category"/> is its PICTURE's category, which SR4's "numeric data item" asks.</summary>
public sealed class ReportSourceFamily : ReportEntryFamily
{
    /// <summary>The category of the entry's PICTURE; null when it has none or none that analyzed.</summary>
    public PicCategory? Category { get; set; }
}

/// <summary>
/// ⛔ ONE OCCURRENCE OF A VALUE-CARRYING REPORT ENTRY — what one rolled addition adds (kb/Work PB1294). It is the
/// entry's counter occurrence (<see cref="Sum"/>), or the operand of its SOURCE or VALUE clause for this occurrence
/// (<see cref="Source"/> — §13.18.63.4 GR23 / §13.18.53.4 GR4: successive operands to successive repeating items, so
/// occurrence n takes operand n mod count), and it carries the FULL presence chain §13.18.54.4 GR11 asks ("declared
/// to be absent as a result of a PRESENT WHEN clause or an OCCURS clause with the DEPENDING phrase"): every condition
/// from the 01 entry down to the entry, and this replay's DEPENDING tests.
/// </summary>
public sealed class ReportItemOccurrence
{
    public required ReportEntryFamily Family { get; init; }

    /// <summary>Zero-based, one per <see cref="ReportEntryFamily.Extents"/> level, outermost first.</summary>
    public required IReadOnlyList<int> Coordinates { get; init; }

    /// <summary>The counter occurrence, when the entry contains a SUM clause.</summary>
    public ReportSumModel? Sum { get; init; }

    /// <summary>The operand of the entry's SOURCE or VALUE clause for this occurrence, bound in the procedure phase
    /// with the rest of the report's clause operands; null for a SUM entry.</summary>
    public ReportFieldSource? Source { get; init; }

    /// <summary>The VARYING counters in scope at the entry (§13.18.64.3 SR2) — what its SOURCE operand may name.</summary>
    public List<ReportVaryingUse> Varyings { get; } = [];

    /// <summary>The SOURCE operand names one of those counters — whose value is the entry's own occurrence's
    /// (§13.18.64.4 GR3: FROM for the first, BY added for each later one), a compose-local when the entry prints. The
    /// emitter gives a rolled addition of such an operand the same locals for the occurrence it adds.</summary>
    public bool VaryingDependent { get; set; }

    // A SUM entry's chain IS its counter occurrence's (the same lists, so the engine's reset and a rolled addition
    // read one answer); a SOURCE / VALUE entry owns its own.
    private readonly List<CobolParserCore.ConditionContext> _presentWhenCtxs = [];
    private readonly List<BoundCondition> _presentWhen = [];
    private readonly List<ReportRepetitionGuard> _guards = [];

    /// <summary>The PRESENT WHEN conditions 01 → entry as captured parse contexts (§13.18.41.4 GR2b).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs => Sum?.PresentWhenCtxs ?? _presentWhenCtxs;

    /// <summary>The same chain bound in the procedure phase.</summary>
    public List<BoundCondition> PresentWhen => Sum?.PresentWhen ?? _presentWhen;

    /// <summary>This replay's OCCURS … DEPENDING presence tests (§13.18.38.4 GR13), outermost repeating entry first.</summary>
    public List<ReportRepetitionGuard> RepetitionGuards => Sum?.RepetitionGuards ?? _guards;

    /// <summary>The value one rolled addition adds, bound in the procedure phase: the counter occurrence's content, or
    /// the SOURCE / VALUE operand as a number. Null when the operand was refused (reported where it was).</summary>
    public BoundExpr? Value { get; set; }
}

/// <summary>
/// ⛔ ONE SUM ENTRY'S COUNTERS, AND THEY ARE A TABLE (kb/Work PB1271). ISO §13.18.54.4 GR1 gives "each entry
/// containing a SUM clause" an independent sum counter, and when that entry is a REPEATING entry the counter
/// repeats with it: GR8 a) adds each occurrence of a repeating addend "into the corresponding occurrence of the sum
/// counter", and GR10 suppresses the print and the reset of "the corresponding sum counter" of an absent
/// occurrence. The repetition vehicles are §13.15.4 GR3's three — an OCCURS clause (§13.18.38 Format 3), a multiple
/// LINE clause (§13.18.35.4 GR9: "functionally equivalent to … a simple OCCURS clause") and a multiple COLUMN clause
/// (§13.18.14.4 GR12: the same sentence) — and every one of them is an OCCURS level, so the counter's occurrences
/// form a table of <see cref="Extents"/>, one level per enclosing repetition, outermost first, plus, innermost,
/// the entry's own multiple COLUMN clause.
/// <para>So a procedure division reference subscripts it like any table element (§8.4.2.3.3 SR3 — one subscript
/// per OCCURS level; SR5 — "Each table element reference shall be subscripted"), which is why the family's
/// <see cref="Register"/> carries a synthetic ancestor chain with one <see cref="DataItem.Occurs"/> per extent: the
/// ordinary arity screen, <see cref="DataItem.SubscriptLevels"/> and the table(ALL) argument read it unchanged.
/// The single-register model this replaced shared ONE counter among a multiple COLUMN entry's printable items and
/// refused every subscripted reference "not defined".</para>
/// <para>The counter ids of a family are one CONTIGUOUS block, row-major over <see cref="Extents"/>, reserved when
/// the family is first seen: an occurrence's id is <see cref="BaseId"/> + its linear coordinate. Contiguity is
/// what lets a run-time subscript select the counter by arithmetic — the replay binds sibling entries between the
/// repetitions (A0 B0 A1 B1), so the ids could not be contiguous in binding order.</para>
/// </summary>
public sealed class ReportSumFamily : ReportEntryFamily
{
    /// <summary>The id of occurrence (1, 1, …): the first counter of this family's block.</summary>
    public required int BaseId { get; init; }

    /// <summary>The IMPLICITLY-DEFINED data item the counter is (ISO §13.18.54.4 GR1 — "a conceptual data item
    /// that behaves as a data item of the category numeric"), carrying the GR1 profile
    /// (<see cref="PicInfo.SumCounterItem"/>) and, for a repeating entry, the synthetic OCCURS chain of
    /// <see cref="Extents"/>. It is engine state, not storage — kept off <c>DataBinder.ByName</c>/<c>Roots</c> and
    /// reachable only through <c>DataBinder.SumCounters</c>, the resolver hook that builds a
    /// <see cref="Model.ReportSumCounterPlace"/> (the CAPACITY-register pattern). Every occurrence shares it: they
    /// are occurrences of ONE description.</summary>
    public required DataItem Register { get; init; }

    /// <summary>The counter's scale — the fractional digit count GR1 derives from the entry's PICTURE.</summary>
    public int Scale { get; init; }

    /// <summary>The COBOL-2002 PICTURE-shape introduction gate (a <c>Constructs.*</c> id) this counter's PICTURE
    /// carries — a floating-point numeric-edited (symbol E) or national-edited picture, from the ONE
    /// <c>VersionConformancePass.PictureConstructId</c>. The SUM-counter scale-derivation <c>Analyze</c> (GR1) is a
    /// DISTINCT call off <c>ConformanceForest</c>, so this preserves its gate for the post-bind
    /// <c>VersionConformancePass</c> GateData report-Sums walk (DEVLOG 740; else the 0900 below 2002 is dropped on
    /// this error path). Null when the picture is version-invariant (the normal numeric case).</summary>
    public string? SkeletonGate { get; init; }
    /// <summary>The exact where-string the SUM-counter <c>Analyze</c> used (<c>RD '…' SUM counter '…'</c>) — replayed
    /// verbatim by GateData when <see cref="SkeletonGate"/> fires, so the 0900 is byte-identical to the former site.</summary>
    public string SkeletonWhere { get; init; } = "";

    /// <summary>The id of the occurrence at <paramref name="coordinates"/> (zero-based, outermost first, one per
    /// <see cref="Extents"/> level): <see cref="BaseId"/> + the row-major linear coordinate — the same order a
    /// subscript list is written in (§8.4.2.3.3 SR3, "successively less inclusive dimensions"), and the same
    /// arithmetic the engine's <c>CobolReport.SumOccurrence</c> does for a run-time subscript.</summary>
    public int IdAt(IReadOnlyList<int> coordinates) => BaseId + Linear(coordinates);
}

/// <summary>One SUM counter — one OCCURRENCE of a <see cref="ReportSumFamily"/> (ISO §13.18.54): its identity
/// (GR1 — see <see cref="Id"/>), the addend TERMS (SR5's items OUTSIDE the report section, and SR4's data-name-1
/// rolled totals, which register with their addend's own group — <see cref="ReportSumAddend.RolledFrom"/>), each
/// carrying its own UPON detail names (GR7c2), the RESET operand
/// (GR2), and the presence tests that suppress its print and reset (GR10).</summary>
public sealed class ReportSumModel
{
    /// <summary>⛔ THE COUNTER'S IDENTITY IS THE ENTRY'S OCCURRENCE, NEVER ITS SPELLING (kb/Work PB882, PB1271).
    /// ISO §13.18.54.4 GR1: "Each entry containing a SUM clause establishes an independent sum counter and size
    /// error indicator", and a repeating entry establishes one per occurrence (<see cref="ReportSumFamily"/>). This
    /// is the occurrence's id within its report description — <see cref="ReportSumFamily.IdAt"/>, the index of this
    /// model in <see cref="ReportModel.Sums"/> and, at run time, of its counter in the engine's list. It used to be
    /// the entry's data-name, which made two entries that legally share a data-name share ONE counter: the second
    /// registration overwrote the first and both printable faces rendered the second total.</summary>
    public required int Id { get; init; }

    /// <summary>The SUM ENTRY this counter is an occurrence of — its name, its register, its scale and its
    /// qualification hierarchy are the entry's, shared by every occurrence.</summary>
    public required ReportSumFamily Family { get; init; }

    /// <summary>The clause's <c>SUM … [UPON …]</c> groups in written order — ONE counter per ENTRY (GR1),
    /// however many times the SUM keyword appears (SR1).</summary>
    public List<ReportSumTerm> Terms { get; } = [];

    /// <summary>The RESET ON operand as written (§13.18.54.3 SR8 — data-name-3 "may be qualified and
    /// reference-modified"; it "shall be an operand of the CONTROL clause of the current report description").
    /// Null for RESET ON FINAL and for no RESET phrase.</summary>
    public ReportControlRef? ResetOperand { get; set; }
    public bool ResetFinal { get; set; }

    /// <summary>The clause's ROUNDED phrase (§13.18.54.2's trailing <c>[ rounded-phrase ]</c>, §14.7.4), or null.
    /// §13.18.54.4 GR4: "If the ROUNDED phrase is specified …, the content of the sum counter is computed
    /// according to the general rules for the COMPUTE statement with the ROUNDED phrase" — the phrase governs the
    /// counter's delivery to the printable item, which is why §13.18.54.3 SR3 admits it only with a COLUMN
    /// clause. Null (no phrase) ⇒ the transfer truncates (§14.7.4.3 r2).</summary>
    public Core.RoundedPhraseContext? Rounded { get; set; }

    /// <summary>The rounding mode <see cref="Rounded"/> selects (§14.7.4.3), resolved in the procedure phase
    /// through the ONE <c>ExpressionBinder.RoundingOf</c>.</summary>
    public WiseOwl.Cobol.Runtime.CobolRounding Rounding { get; set; } = WiseOwl.Cobol.Runtime.CobolRounding.Truncation;
    /// <summary>The resolved RESET control level; −1 = no RESET phrase (reset where printed, GR2).</summary>
    public int ResetLevel { get; set; } = -1;
    /// <summary>The group whose processing end resets the counter when no RESET phrase is given (GR2).</summary>
    public required ReportGroupModel PrintedIn { get; init; }

    /// <summary>The SUM entry's FULL PRESENT WHEN chain (01 → entry). When any condition is false at a group
    /// presentation the counter is neither printed nor reset for that instance (ISO §13.18.41.4 GR3g /
    /// §13.18.54.4 GR10) — the engine consults the AND of these per presentation.</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    public List<BoundCondition> PresentWhen { get; } = [];

    /// <summary>⛔ GR10's OTHER HALF — the OCCURS … DEPENDING presence tests of THIS OCCURRENCE (ISO §13.18.38.4
    /// GR13), outermost repeating entry first. §13.18.54.4 GR10: "If the entry is associated with an absent data item
    /// as a result of a PRESENT WHEN clause or an OCCURS clause with the DEPENDING phrase, the corresponding sum
    /// counter is not printed and is not reset to zero for the current instance of the report group." The PRESENT
    /// WHEN half is <see cref="PresentWhen"/>; an occurrence the DEPENDING count excludes is absent exactly as its
    /// printable item is (<see cref="ReportFieldModel.RepetitionGuards"/>), and the two are ANDed into the one
    /// presence slot the engine's reset reads (kb/Work PB1271).</summary>
    public List<ReportRepetitionGuard> RepetitionGuards { get; } = [];
}
