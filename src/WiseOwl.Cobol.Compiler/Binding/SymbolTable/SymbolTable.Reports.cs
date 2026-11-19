// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Passes;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using ReportEntryLocation = DataBinder.ReportEntryLocation;

/// <summary>Where a name is written when it is written in a REPORT SECTION clause: the report description the clause belongs to
/// (<see cref="Report"/>, null in the procedure division and everywhere else) and the VARYING counters of the entry being bound
/// (<see cref="Varyings"/>, null outside a report entry's clause). The two scope the report-section names: a SOURCE operand's
/// sum counters are those "defined in the current report" (ISO §2023:13.18.53.3 SR4), and a VARYING counter "may be referenced
/// only within the current entry or a subordinate entry" (§2023:13.18.64.3 SR2). The caller states it, as a procedure-name's
/// caller states its <see cref="ProcedureSite"/>: the cursor is the report clause binder's, not the table's.</summary>
internal readonly record struct ReportSite(ReportModel? Report, IReadOnlyList<ReportVaryingModel>? Varyings);

/// <summary>What a report-section name identified when the name, as written, may reach several entries (docs/rearchitecture/
/// DESIGN-listing.md section 6.7.1, "Step 6b, slice 5"): every candidate the written qualifiers reach, in declaration order, narrowed
/// to the nearest declaring source element where the class is ranked so (§2023:8.4.6.2.1 3)). The class's own funnel decides what
/// two or more candidates mean (§2023:8.4.2.2.3 SR1's ambiguity, worded at the reference), so it is no identification until the funnel
/// has one, and listing step 7b records an occurrence only for a single candidate (as for <see cref="ProcedureCandidates"/>).</summary>
internal sealed record ReportCandidates<T>(IReadOnlyList<T> Hits);

/// <summary>What a VARYING clause's data-name-1 is, declared in one report group description entry (ISO §2023:13.18.64.3 SR2).</summary>
internal enum VaryingCounterDeclaration
{
    /// <summary>Declared: no other counter of the entry, and none of an entry above it, carries the name.</summary>
    Declared,
    /// <summary>A second counter of the same name in the same entry.</summary>
    TwiceInEntry,
    /// <summary>A counter of an entry this one is subordinate to carries the name.</summary>
    DeclaredAbove,
}

public sealed partial class SymbolTable
{
    // ── The report-names, the sum counters and the report group description entries (docs/rearchitecture/DESIGN-listing.md
    //    section 6.7.1, "Step 6b, slice 5"): the REPORT SECTION's names, private to the table; every reference is
    //    Identify(written, NameClass.Report / ReportGroup / SumCounter / ReportVarying / ReportEntry / ReportSumOperand, ...). ──

    /// <summary>This element's own report-names (its RD entries), the first of a spelling: a second RD of one report-name is
    /// refused at its declaration (§2023:8.3.2.2, <see cref="UserWordKinds.DeclaredUnqualifiable"/>, kb/Work PB5307).</summary>
    private readonly Dictionary<CobolName, ReportModel> _ownReports = [];

    /// <summary>⛔ THE REPORTS A PROCEDURE DIVISION REFERENCE CAN NAME, by report-name: this element's own, then the GLOBAL ones of
    /// its containers, nearest container first (ISO §2023:13.18.27.4 GR1/GR2; kb/Work PB369), a name already here hiding a farther
    /// one (§2023:8.4.6.2). <see cref="ReportScope.Visible"/> keeps the same reports in order.</summary>
    private readonly Dictionary<CobolName, ReportModel> _visibleReports = [];

    /// <summary>REPORT SECTION <b>sum counter</b> names → every SUM ENTRY that carries the name, with its owning report (ISO
    /// §2023:13.18.54.4 GR5 — "If a data-name immediately follows the level number in the entry containing the SUM clause, the
    /// data-name is the name of the sum counter, not the name of the associated printable item"). The counter is IMPLICITLY
    /// defined by its entry and is in no data-name table; GR12 ("It is permissible for procedure division statements to alter the
    /// content of sum counters") is exercised through <see cref="Identify(ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>
    /// (kb/Work PB840).
    /// <para>⛔ A LIST PER NAME, NOT ONE ENTRY (kb/Work PB882). GR1 gives every ENTRY its own counter and nothing makes two
    /// entries' data-names distinct: §2023:8.4.2.2.1's uniqueness requirement bites on a REFERENCE, not on a declaration nobody
    /// names. A duplicated name is legal to DECLARE and, unless a report-name qualifier resolves it, diagnosed where it is
    /// REFERENCED.</para></summary>
    private readonly Dictionary<CobolName, List<(ReportModel Report, ReportSumFamily Family)>> _sumCounters = [];

    /// <summary>Every named report group description entry of this element, by name, as written in its RD entries (kb/Work PB1226):
    /// a constant entry's length phrase may name one before the report section is bound (§2023:13.10.3 SR11).</summary>
    private readonly Dictionary<CobolName, List<ReportEntryLocation>> _reportEntryLocations = [];

    /// <summary>This element's own report <paramref name="model"/>, declared by its RD entry: its report-name joins the table and
    /// the report the scope, in the order written.</summary>
    internal void Declare(ReportModel model)
    {
        _data.State.Reports.Declare(model);
        _ownReports.TryAdd(Key(model.Name), model);
        _visibleReports.TryAdd(Key(model.Name), model);
    }

    /// <summary>Make a container's GLOBAL report visible here (ISO §2023:13.18.27.4 GR1/GR2): its report-name, its report groups
    /// (reached through the report) and its sum counters (GR1 — "All data-names subordinate to a global name are global names").
    /// Called nearest container first, so a name this element declares — or a nearer container already supplied — hides the
    /// farther one (§2023:8.4.6.2). False when the report-name is hidden.</summary>
    internal bool InheritReport(ReportModel report, int depth)
    {
        if (!_visibleReports.TryAdd(Key(report.Name), report)) return false;
        _data.State.Reports.Inherit(report, depth);
        foreach (var family in report.SumFamilies)
        {
            // A sum counter is a data-name subordinate to the report (GR1). A LOCAL declaration of the same name hides it; a
            // same-named counter of this element's own reports is a homonym the report-name qualifier resolves, exactly as
            // between two reports of one program (kb/Work PB882).
            if (family.Name is not { } sn || _data.State.Forest.ByName.ContainsKey(sn)) continue;
            DeclareSumCounter(sn, report, family);
        }
        return true;
    }

    /// <summary>Record <paramref name="family"/>'s sum counter under <paramref name="name"/>: a list per name, a duplicated name
    /// being legal to declare (kb/Work PB882).</summary>
    internal void DeclareSumCounter(string name, ReportModel report, ReportSumFamily family)
    {
        if (!_sumCounters.TryGetValue(Key(name), out var homonyms)) _sumCounters[Key(name)] = homonyms = [];
        homonyms.Add((report, family));
    }

    /// <summary>Every named report group description entry of one RD entry joins the table (kb/Work PB1226), before any entry of
    /// the data division binds, so a constant's length phrase can name one.</summary>
    internal void DeclareReportEntries(Core.ReportDescriptionEntryContext rd)
    {
        if (rd.reportName()?.GetText() is not { } report) return;
        var entries = rd.reportGroupEntry();
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].dataName().NameOrNull() is not { } name) continue;
            if (!_reportEntryLocations.TryGetValue(Key(name), out var list)) _reportEntryLocations[Key(name)] = list = [];
            if (!list.Any(l => ReferenceEquals(l.Entry, entries[i])))
                list.Add(new ReportEntryLocation(entries, i, report));
        }
    }

    /// <summary>The classes of the REPORT SECTION's names, which <see cref="Identify(ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>
    /// resolves from the written context (a qualified reference is read from the context's children, never its glued text).</summary>
    private static bool IsReportClass(NameClass @class) => @class is NameClass.Report or NameClass.ReportGroup
        or NameClass.SumCounter or NameClass.ReportVarying or NameClass.ReportEntry or NameClass.ReportSumOperand;

    /// <summary>The symbol of a report-section class the written context names here, or null.</summary>
    private object? ReportSymbolNamed(ParserRuleContext written, NameClass wanted, NameReach reach, ReportSite site) => wanted switch
    {
        NameClass.Report => WordOf(written) is { } key
            ? (reach == NameReach.Own ? _ownReports : _visibleReports).GetValueOrDefault(key) : null,
        NameClass.ReportGroup => ReportGroupsNamed(written, reach),
        NameClass.SumCounter => written is Core.DataReferenceContext dref ? SumCountersReached(dref, site.Report) : null,
        NameClass.ReportVarying => WordOf(written) is { } key && site.Varyings is { } varyings ? VaryingCounterNamed(key, varyings) : null,
        NameClass.ReportEntry => written is Core.DataReferenceContext dref ? ReportEntriesWritten(dref) : null,
        NameClass.ReportSumOperand => written is Core.DataReferenceContext dref ? RolledEntriesNamed(dref) : null,
        _ => throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "not a report-section class"),
    };

    /// <summary>⛔ THE ONE LOOKUP OF A WRITTEN REPORT-GROUP REFERENCE (kb/Work PB365): every report group the written head names
    /// (a qualified reference restricts the candidate reports to the one its report-name qualifier names, §2023:8.4.2.2.2 Format 1's
    /// file-report-qualifier). <see cref="NameReach.Visible"/> (a procedure division reference) reads every report this element can
    /// name and keeps the nearest declaring source element's candidates (§2023:8.4.6.2.1 rule 3, <see cref="ReportScope.NearestInScope{T}"/>);
    /// <see cref="NameReach.Own"/> (the SUM clause's UPON operand, a data division site) reads this element's own reports. The
    /// head and the qualifier are read from the context's children: a <c>reportGroupReference</c> (GENERATE, USE BEFORE REPORTING)
    /// or the <c>dataReference</c> of an UPON operand, which <c>ReportSectionBinder.UponDetailRef</c> has screened to one
    /// qualifier at most. Null when no group answers; <c>ReportGroupResolution.Resolve</c> decides what several mean.</summary>
    private ReportCandidates<(ReportModel Report, ReportGroupModel Group)>? ReportGroupsNamed(ParserRuleContext written, NameReach reach)
    {
        string head;
        string? qualifier;
        switch (written)
        {
            case Core.ReportGroupReferenceContext r:
                (head, qualifier) = (r.cobolWord().GetText(), r.reportName()?.GetText());
                break;
            case Core.DataReferenceContext d:
                var (name, quals) = DataBinder.KeyReference(d);
                (head, qualifier) = (name, quals.Count == 1 ? quals[0] : null);
                break;
            default:
                return null;
        }
        var reports = reach == NameReach.Own ? _data.State.Reports.Declared : _data.State.Reports.Visible;
        var found = new List<(ReportModel Report, ReportGroupModel Group)>();
        foreach (var r in reports)
        {
            if (qualifier is not null && !CobolNames.Same(r.Name, qualifier)) continue;
            foreach (var g in r.Groups)
                if (g.Name is not null && CobolNames.Same(head, g.Name))
                    found.Add((r, g));
        }
        if (reach == NameReach.Visible) found = _data.State.Reports.NearestInScope(found, f => f.Report);
        return found.Count == 0 ? null : new(found);
    }

    /// <summary>⛔ THE ONE NAME-AND-QUALIFIER LOOKUP OF A SUM COUNTER REFERENCE (kb/Work PB5310 review R1-1), behind the P6 Step-6
    /// watermark gate (the counters exist once <see cref="PassPhase.FilesResolved"/> has run): the SUM entries carrying the written
    /// name — only those of <paramref name="within"/> when a report's own clause writes the reference (§2023:13.18.53.3 SR4,
    /// "defined in the current report") — that the written qualifiers reach through the ONE §2023:8.4.2.2.3 SR4 walk
    /// (<see cref="DataBinder.QualifierWalk"/>, the report-name last), narrowed to the nearest declaring source element
    /// (§2023:8.4.6.2.1 rule 3; §1985:X.1.3.8.2 GR3a at COBOL-85), so a counter of this element's own report hides a same-named counter
    /// of a container's GLOBAL report. Null when the name is no eligible counter's at all; empty when it is, but the qualifiers reach
    /// none (<c>CounterResolver.SumCounterFamilyFor</c> words each). The register alternatives (LINAGE-COUNTER, LINE-COUNTER,
    /// PAGE-COUNTER), whose <c>cobolWord</c> is a qualifier, name no counter.
    /// <para>Every data-name reference asks it first (a sum counter resolves before the ordinary lookup), so a word no counter carries
    /// costs no allocation: the reference's qualifiers are read only for a name the table holds (DESIGN-listing.md section 9's `None`
    /// row, measured by step 6b slice 5).</para></summary>
    private ReportCandidates<(ReportModel Report, ReportSumFamily Family)>? SumCountersReached(Core.DataReferenceContext dref, ReportModel? within)
    {
        _data.State.Require(PassPhase.FilesResolved, "SumCounters");
        if (_sumCounters.Count == 0 || ReferenceResolver.WrittenWordOf(dref) is not { } name || !_sumCounters.ContainsKey(Key(name))) return null;
        return SumCountersNamed(name, ReferenceResolver.ReadWritten(dref).Qualifiers, within) is { } hits ? new(hits) : null;
    }

    /// <summary>The walk of <see cref="SumCountersReached"/> over a name and its qualifiers as read.</summary>
    private List<(ReportModel Report, ReportSumFamily Family)>? SumCountersNamed(string name, IReadOnlyList<string> qualifiers, ReportModel? within)
    {
        if (!_sumCounters.TryGetValue(Key(name), out var homonyms)) return null;
        IEnumerable<(ReportModel Report, ReportSumFamily Family)> eligible =
            within is null ? homonyms : homonyms.Where(h => ReferenceEquals(h.Report, within));
        if (!eligible.Any()) return null;
        return _data.State.Reports.NearestInScope(eligible.Where(h => DataBinder.QualifierWalk(h.Family.Qualification, qualifiers,
            q => CobolNames.Same(q, h.Report.Name))), h => h.Report);
    }

    /// <summary>⛔ THE COBOL-85 READING OF A SUM COUNTER NAMED BY A CLAUSE OF ANOTHER SECTION OF THE DATA DIVISION (kb/Work PB5310):
    /// §1985:XIII.3.9.3 SR10d — "Data-name-1 may be referenced only if the entry defines a sum counter" — restricts which report entries
    /// a reference may name and says nothing of where it may be written, so at COBOL-85 a sum counter is a data-name of the program
    /// wherever a clause prints one (an OCCURS DEPENDING ON object that "must describe an integer", §1985:VI.5.8.3 SR6, may be one).
    /// Returns the counter's register (<see cref="ReportSumFamily.Register"/>, which the place builder answers with the counter's
    /// place) when the edition is COBOL-85 and the written reference names exactly one sum counter of a non-repeating entry, by the
    /// one walk a procedure division reference takes (<see cref="SumCountersNamed"/>: the qualifier walk, then the nearest declaring
    /// program, §1985:X.1.3.8.2 GR3a); null otherwise, leaving the caller's own verdict. Two counters of one program the qualifiers do not
    /// tell apart are §2023:8.4.2.2.3 SR1's ambiguity, reported here. Asked only where the ordinary lookup found no data item.
    /// <para>The operand reaches the table as TEXT because the data division's clause operands are held as text (the clause models,
    /// <c>DataBinder.ResolveClauseOperand</c>'s callers): it becomes a <see cref="Identify(ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>
    /// reference when the data-name class carries the written word (kb/Work PB5932's dated file <c>2026-11-19-step-6b-slice-5</c>).</para></summary>
    internal DataItem? SumCounterOperand85(string name, IReadOnlyList<string> qualifiers, string subject, out bool ambiguityReported)
    {
        ambiguityReported = false;
        var edition = _data.Edition;
        if (edition.DialectLevel >= 2002 || SumCountersNamed(name, qualifiers, null) is not { } matches) return null;
        if (matches.Count > 1)
        {
            // The ONE verdict of the reference (kb/Work PB5627): the caller returns without asking the section rule, which would
            // name the same operand a second time under a 2023 clause.
            ambiguityReported = true;
            edition.Error(Editions.Diagnostics.DiagnosticCatalog.ReportSumCounterReference, $"{subject}: the reference is ambiguous — {matches.Count} "
                + $"entries establish a sum counter named '{name}'. A reference shall uniquely identify one resource (ISO "
                + "§1985:IV.4.3.8.1 SR3); qualify it by report group entry or report-name.");
            return null;
        }
        // A COBOL-85 report entry has no OCCURS clause, so a counter is never a table there; the guard keeps the 2002 shapes out.
        return matches is [var (_, family)] && family.Register.SubscriptArity == 0 ? family.Register : null;
    }

    /// <summary>The VARYING counter in scope the word names (ISO §2023:13.18.64.4 GR3/GR4 — "this allows data-name-1 to be used as a
    /// source data item, as a subscript to a source data item …"), or null. The scope is the entry's and its enclosing entries'
    /// counters (§2023:13.18.64.3 SR2); a later counter of a spelling hides an earlier one, as the entry nearest the reference
    /// declares it last.</summary>
    private static ReportVaryingModel? VaryingCounterNamed(CobolName key, IReadOnlyList<ReportVaryingModel> varyings)
    {
        for (int i = varyings.Count - 1; i >= 0; i--)
            if (Key(varyings[i].Name) == key) return varyings[i];
        return null;
    }

    /// <summary>The report group description entries a written reference names, as the RD entries write them (kb/Work PB1226): each
    /// qualifier, in the order written, is a name the entry is subordinate to (§2023:8.4.2.2.3 SR4), the report-name last. A
    /// constant entry's length phrase names a report item so (§2023:13.10.3 SR11) before the report section is bound. Null when none.</summary>
    private ReportCandidates<ReportEntryLocation>? ReportEntriesWritten(Core.DataReferenceContext dref)
    {
        var (name, qualifiers) = DataBinder.KeyReference(dref);
        if (!_reportEntryLocations.TryGetValue(Key(name), out var locs)) return null;
        List<ReportEntryLocation> hits = [.. locs.Where(l =>
        {
            int q = 0;
            foreach (var outer in l.Qualification)
                if (q < qualifiers.Count && CobolNames.Same(outer, qualifiers[q])) q++;
            return q == qualifiers.Count;
        })];
        return hits.Count == 0 ? null : new(hits);
    }

    /// <summary>⛔ A SUM CLAUSE'S DATA-NAME-1 — A ROLLED TOTAL (ISO §2023:13.18.54.3 SR4; kb/Work PB1294): the entries of this element's
    /// own report sections that carry a value (a SUM, SOURCE or VALUE clause, <see cref="ReportModel.EntryFamilies"/>) the written
    /// reference names, the qualifiers consumed against each entry's own hierarchy and the REPORT-name last (§2023:8.4.2.2.3 SR4,
    /// §2023:8.4.2.2.2 Format 1), exactly as a sum counter's reference is (kb/Work PB1454). Null when none; the SUM clause's binder
    /// words several (<c>ReportSectionBinder.ScreenRolledAddend</c>).</summary>
    private ReportCandidates<ReportEntryFamily>? RolledEntriesNamed(Core.DataReferenceContext dref)
    {
        var (name, qualifiers) = DataBinder.KeyReference(dref);
        List<ReportEntryFamily> hits = [.. _data.State.Reports.Declared.SelectMany(r => r.EntryFamilies)
            .Where(f => CobolNames.Same(f.Name, name)
                && DataBinder.QualifierWalk(f.Qualification, qualifiers, q => CobolNames.Same(q, f.Report.Name)))];
        return hits.Count == 0 ? null : new(hits);
    }

    /// <summary>⛔ THE ONE LOOKUP OF A WRITTEN NAME AMONG THE REPORT SECTION'S ENTRIES, for the references §2023:13.8.6.2.3 bars
    /// (kb/Work PB1046): every named entry of every report this source element can name — its own and the GLOBAL ones of its
    /// containers (<see cref="ReportScope.Visible"/>, ISO §2023:13.18.27.4 GR1/GR2) — that the written <paramref name="qualifiers"/>
    /// reach through the ONE §2023:8.4.2.2.3 SR4 qualifier walk (<see cref="DataBinder.QualifierWalk"/>, the report-name last,
    /// §2023:8.4.2.2.2 Format 1's file-report-qualifier), narrowed to the nearest declaring source element (§2023:8.4.6.2.1 rule 3).
    /// The data division's lookups ask it with the written text they hold; its answers leave the table as facts
    /// (<see cref="Declarations.ReportEntriesNamed"/>, <see cref="NearerEntryHides"/>), never as entries.</summary>
    private List<(ReportModel Report, ReportNamedEntry Entry)> EntriesNamed(string name, IReadOnlyList<string> qualifiers)
    {
        var hits = new List<(ReportModel Report, ReportNamedEntry Entry)>();
        foreach (var report in _data.State.Reports.Visible)
            foreach (var entry in report.NamedEntries)
                if (CobolNames.Same(entry.Name, name)
                    && DataBinder.QualifierWalk(entry.Qualification, qualifiers, q => CobolNames.Same(q, report.Name)))
                    hits.Add((report, entry));
        return _data.State.Reports.NearestInScope(hits, h => h.Report);
    }

    /// <summary>⛔ ISO §2023:8.4.6.2.1 3) a)-b) FOR A REPORT ENTRY AGAINST THE DATA ITEMS OF A FARTHER SOURCE ELEMENT (kb/Work PB4196):
    /// true when the written reference reaches a report entry declared in a source element NEARER than
    /// <paramref name="declaringDepth"/>, the depth (<see cref="ForestSlice.DeclaringDepth"/>) of the data items it also reaches. "If
    /// the name is declared in source element B, the item in source element B is the referenced item" (a), otherwise "the item in
    /// the containing source element" (b): the report entry is the referenced item and the container's GLOBAL data item of the
    /// spelling is not. A report entry is a data-name of the source element whose REPORT SECTION declares it (§2023:8.4.2.2.3 SR1
    /// counts it with the ordinary ones, kb/Work PB3232), so the data-name lookup <see cref="NearestDeclaringData"/> asks this at the
    /// tier it narrowed to and leaves the set EMPTY, and the callers' own report-item refusal
    /// (<see cref="ReportSectionBinder.ReportEntryReferenceRefused"/>, §2023:13.8.6.2.3) names the entry. Free for a unit that inherits
    /// no global data or has no report.</summary>
    internal bool NearerEntryHides(string name, IReadOnlyList<string> qualifiers, int declaringDepth) =>
        declaringDepth > 0 && _data.State.Reports.Visible.Count != 0
        && EntriesNamed(name, qualifiers) is [var (report, _), ..] && _data.State.Reports.Depth(report) < declaringDepth;

    /// <summary>The report-section-exclusive names of one report: the data-name of EVERY report group description entry it writes
    /// (the group's own, an intermediate group entry's, a printable entry's, a sum counter's, an unprintable SOURCE entry's), each
    /// only when no data description entry of this element also declares it: a name also declared in ordinary storage resolves THERE
    /// (§2023:8.4.2.1), so it is no report-section reference. Read off the WRITTEN entries (<see cref="ReportModel.NamedEntries"/>):
    /// the set is about NAMES the programmer declared (kb/Work PB1289). The set §2023:13.18.16.3 SR2 asks of a CONTROL operand and
    /// §2023:13.15.3 SR16 of a PRESENT WHEN condition (kb/Work PB205).</summary>
    internal List<string> ReportSectionOnlyNames(ReportModel model)
    {
        var names = new List<string>();
        foreach (var entry in model.NamedEntries)
            if (!_data.State.Forest.DeclaresOwnDataName(entry.Name) && !names.Contains(entry.Name, CobolNames.Comparer))
                names.Add(entry.Name);
        return names;
    }

    /// <summary>The report-section-exclusive names of every report this element declares, each spelling once (the first written).</summary>
    internal List<string> ReportSectionOnlyNames() =>
        [.. _data.State.Reports.Declared.SelectMany(ReportSectionOnlyNames).Distinct(CobolNames.Comparer)];

    /// <summary>The words of <paramref name="words"/> that are ISO §2023:8.4.2.3.3 SR8's report section counters
    /// (<see cref="Declarations.IsReportSubscriptCounter"/>), each spelling once, in the order first written.</summary>
    internal IEnumerable<string> ReportSubscriptCountersIn(IEnumerable<string> words)
    {
        var seen = new HashSet<CobolName>();
        foreach (var word in words)
            if (Declared.IsReportSubscriptCounter(word) && seen.Add(Key(word)))
                yield return word;
    }

    /// <summary>The VARYING counters one report's entries declare, walked in entry order (ISO §2023:13.18.64.3 SR2): each counter's
    /// data-name-1 "shall not be defined elsewhere in the source element, except as data-name-1 in another VARYING clause of an entry
    /// not subordinate to the subject of the current entry".</summary>
    internal VaryingCounterNames VaryingCountersOf(ReportModel model) => new(model);

    /// <summary>The walk of <see cref="VaryingCountersOf"/>: the counters of the entries above the current one, and of the current
    /// one, by name. A counter declared without conflict joins <see cref="ReportModel.VaryingNames"/> once.</summary>
    internal sealed class VaryingCounterNames(ReportModel model)
    {
        private readonly List<(int Level, CobolName Name)> _enclosing = [];
        private readonly List<CobolName> _own = [];

        /// <summary>The walk reaches an entry of <paramref name="level"/>: the entries at that level or deeper are no longer above it.</summary>
        public void BeginEntry(int level)
        {
            _enclosing.RemoveAll(c => c.Level >= level);
            _own.Clear();
        }

        /// <summary>Declare one data-name-1 of the current entry's VARYING clause.</summary>
        public VaryingCounterDeclaration Declare(string name)
        {
            var key = Key(name);
            var verdict = _own.Contains(key) ? VaryingCounterDeclaration.TwiceInEntry
                : _enclosing.Any(c => c.Name == key) ? VaryingCounterDeclaration.DeclaredAbove
                : VaryingCounterDeclaration.Declared;
            if (verdict == VaryingCounterDeclaration.Declared && !model.VaryingNames.Contains(name, CobolNames.Comparer))
                model.VaryingNames.Add(name);
            _own.Add(key);
            return verdict;
        }

        /// <summary>The first counter of the current entry, as declared, that <paramref name="referenced"/> finds written (§2023:13.18.64.3
        /// SR3: "Data-name-1 shall not be referenced in arithmetic-expression-1 of the same VARYING clause").</summary>
        public string? OwnCounterWhere(Func<string, bool> referenced) =>
            _own.Select(static n => n.Word).FirstOrDefault(referenced);

        /// <summary>The current entry, of <paramref name="level"/>, is done: its counters are above the entries subordinate to it.</summary>
        public void EndEntry(int level) => _enclosing.AddRange(_own.Select(n => (level, n)));
    }
}
