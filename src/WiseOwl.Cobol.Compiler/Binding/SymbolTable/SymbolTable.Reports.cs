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
/// to the nearest declaring source element where the class is ranked so (§2023:8.4.6.2.1 GR3). The class's own funnel decides what
/// two or more candidates mean (§2023:8.4.2.2.3 SR1's ambiguity, worded at the reference), so it is no identification until the funnel
/// has one, and listing step 7b records an occurrence only for a single candidate (as for <see cref="ProcedureCandidates"/>).</summary>
internal sealed record ReportCandidates<T>(IReadOnlyList<T> Hits, IReadOnlyList<SymbolId> Ids) : ICandidateEntries;

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

    /// <summary>The inherited report-names a declaration of the spelling in a NEARER source element hides, whatever its class
    /// (§1985:X.1.3.8: a reference is "to the object which that program describes" "even when it is a different type of user-defined
    /// word"; §1985:X.1.3.8.2 GR3a; §2023:8.4.6.2.1 GR3a; kb/Work PB5973): a reference to the report-name itself names the nearer
    /// declaration. The report stays in <see cref="_visibleReports"/>: its report-name still QUALIFIES (GR1 applies qualification
    /// over the whole set before GR3 ranks it), and its groups and sum counters are global names of their own.</summary>
    private readonly HashSet<CobolName> _hiddenReportNames = [];

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
        Enter(NameClass.Report, model.Name, PositionOf(model.At), model);
        EnterReportEntries(model.WrittenEntries);
        _data.State.Reports.Declare(model);
        _ownReports.TryAdd(Key(model.Name), model);
        _visibleReports.TryAdd(Key(model.Name), model);
    }

    /// <summary>Make a container's GLOBAL report visible here (ISO §2023:13.18.27.4 GR1/GR2): its report-name, its report groups
    /// (reached through the report) and its sum counters (GR1 — "All data-names subordinate to a global name are global names").
    /// Called nearest container first, so a report this element declares — or a nearer container already supplied — hides the
    /// farther one (§2023:8.4.6.2). False when the report-name is hidden that way; a nearer declaration of ANOTHER class hides it
    /// once every tier is in place (<see cref="HideShadowedReports"/>).
    /// <para>⛔ EVERY SUM COUNTER JOINS, whatever this element declares (kb/Work PB5973). The set a reference is resolved over is
    /// "all names which are defined in program B and all global names" of its containers, and "the normal rules for qualification
    /// ... are applied until one or more objects is identified" (§1985:X.1.3.8.2 GR1) BEFORE the nearer one is chosen (GR3a). A
    /// counter used to be left out when this element declared a data-name of its spelling, so `TOT OF CFG`, which only the
    /// container's counter answers (GR2), was refused; the nearer data-name now wins at the reference, where the qualifiers are
    /// known (<c>CounterResolver.SumCounterFamilyFor</c>).</para></summary>
    internal bool InheritReport(ReportModel report, int depth)
    {
        if (!_visibleReports.TryAdd(Key(report.Name), report)) return false;
        _data.State.Reports.Inherit(report, depth);
        foreach (var family in report.SumFamilies)
            if (family.Name is { } sn) DeclareSumCounter(sn, report, family);
        return true;
    }

    /// <summary>Hides each inherited report-name that a declaration of its spelling in a NEARER source element hides, whatever that
    /// declaration's class (§1985:X.1.3.8.2 GR3a; §2023:8.4.6.2.1 GR3a; kb/Work PB5973): a contained program's own `01 R1` is what
    /// `INITIATE R1` names there, never its container's GLOBAL report R1. Run once every tier is in place
    /// (<c>InheritStep.DropShadowed</c>), as the inherited file-names are (<see cref="HideShadowedFiles"/>).</summary>
    internal void HideShadowedReports(Func<string, int, bool> declaredNearerThan)
    {
        foreach (var (name, report) in _visibleReports)
            if (_data.State.Reports.Depth(report) is > 0 and var depth && declaredNearerThan(name.Word, depth))
                _hiddenReportNames.Add(name);
    }

    /// <summary>The depth of the report a reference to the report-name <paramref name="name"/> names here (0 for this element's own,
    /// n for a container's GLOBAL one), or null when no report-name of the spelling is visible or a nearer declaration hides it.</summary>
    private int? ReportNameDepth(CobolName name) =>
        !_hiddenReportNames.Contains(name) && _visibleReports.TryGetValue(name, out var report) ? _data.State.Reports.Depth(report) : null;

    /// <summary>The depth of the containing program's GLOBAL report <paramref name="name"/> names when a nearer declaration of the spelling
    /// hides its report-name here (<see cref="HideShadowedReports"/>), or null when no such report is hidden: the question
    /// <see cref="Declarations.ReportNameHiddenBy"/> asks before it names the hiding declaration (kb/Work PB5976).</summary>
    private int? HiddenReportDepth(string name) =>
        _hiddenReportNames.Contains(Key(name)) && _visibleReports.TryGetValue(Key(name), out var report) ? _data.State.Reports.Depth(report) : null;

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
        EnterReportEntries(rd.reportGroupEntry());
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

    /// <summary>Enter each report group description entry of one RD entry, named or FILLER, once (both the pre-bind walk of the data
    /// division and the report's declaration reach them): its entry stands at the entry's level-number, where the data map places it.
    /// A reference to a report group description entry's data-name competes with the ordinary data-names for uniqueness
    /// (§2023:8.4.2.2.3 SR1, kb/Work PB5775), so its class is <see cref="NameClass.Data"/>; the entry's symbol is the written entry, of which the report
    /// group, a sum counter's family, a report entry's location and a SUM operand's family are each a view.</summary>
    private void EnterReportEntries(IReadOnlyList<Core.ReportGroupEntryContext> entries)
    {
        foreach (var entry in entries)
            if (!_entries.IdOf(entry).IsSet)
                Enter(NameClass.Data, entry.dataName().NameOrNull() ?? "FILLER", PositionOf(entry.Start), entry);
    }

    /// <summary>The classes of the REPORT SECTION's names, which <see cref="Identify(ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>
    /// resolves from the written context (a qualified reference is read from the context's children, never its glued text).</summary>
    private static bool IsReportClass(NameClass @class) => @class is NameClass.Report or NameClass.ReportGroup
        or NameClass.SumCounter or NameClass.ReportVarying or NameClass.ReportEntry or NameClass.ReportSumOperand;

    /// <summary>The symbol of a report-section class the written context names here, or null.</summary>
    private (object? Symbol, SymbolId Id) ReportSymbolNamed(ParserRuleContext written, NameClass wanted, NameReach reach, ReportSite site) => wanted switch
    {
        NameClass.Report => WordOf(written) is { } key ? Entered(ReportNamed(key, reach)) : default,
        NameClass.ReportGroup => (ReportGroupsNamed(written, reach), default),
        NameClass.SumCounter => written is Core.DataReferenceContext dref ? (SumCountersReached(dref, site.Report), default) : default,
        NameClass.ReportVarying => WordOf(written) is { } key && site.Varyings is { } varyings
            && VaryingCounterNamed(key, varyings) is { } counter ? (counter, VaryingCounterEntry(counter)) : default,
        NameClass.ReportEntry => written is Core.DataReferenceContext dref ? (ReportEntriesWritten(dref), default) : default,
        NameClass.ReportSumOperand => written is Core.DataReferenceContext dref ? (RolledEntriesNamed(dref), default) : default,
        _ => throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "not a report-section class"),
    };

    /// <summary>The report a report-name denotes here: this element's own (<see cref="NameReach.Own"/>), else every visible one
    /// unless a nearer declaration of the spelling hides it, which a qualifier position (<see cref="NameReach.Qualifier"/>, the
    /// report-name of LINE-COUNTER OF and PAGE-COUNTER OF) reaches all the same (§1985:X.1.3.8.2 GR1; kb/Work PB5973 review R1-1).</summary>
    private ReportModel? ReportNamed(CobolName key, NameReach reach) => reach switch
    {
        NameReach.Own => _ownReports.GetValueOrDefault(key),
        NameReach.Visible when _hiddenReportNames.Contains(key) => null,
        _ => _visibleReports.GetValueOrDefault(key),
    };

    /// <summary>The candidates <paramref name="hits"/>, each with the entry of the written entry it is a view of.</summary>
    private ReportCandidates<T>? CandidatesOf<T>(IReadOnlyList<T> hits, Func<T, Core.ReportGroupEntryContext> written) =>
        hits.Count == 0 ? null : new(hits, [.. hits.Select(h => _entries.IdOf(written(h)))]);

    /// <summary>The entry of a VARYING counter: its declaring word, data-name-1 of the VARYING clause of the entry that declares it
    /// (<see cref="VaryingCounterNames.Declare"/>). A counter is made per enclosing occurrence, so the declaration is its word.</summary>
    private SymbolId VaryingCounterEntry(ReportVaryingModel counter)
    {
        var key = Key(counter.Name);
        foreach (var clause in counter.Entry.reportGroupBody().reportGroupClause())
            if (clause.reportVaryingClause() is { } varying)
                foreach (var spec in varying.reportVaryingSpec())
                    if (spec.cobolWord() is { } word && Key(word.GetText()) == key)
                        return _entries.IdOf(word);
        return default;
    }

    /// <summary>⛔ THE ONE LOOKUP OF A WRITTEN REPORT-GROUP REFERENCE (kb/Work PB365): every report group the written head names
    /// (a qualified reference restricts the candidate reports to the one its report-name qualifier names, §2023:8.4.2.2.2 Format 1's
    /// file-report-qualifier). <see cref="NameReach.Visible"/> (a procedure division reference) reads every report this element can
    /// name and keeps the nearest declaring source element's candidates (§2023:8.4.6.2.1 GR3, <see cref="ReportScope.NearestInScope{T}"/>);
    /// <see cref="NameReach.Own"/> (the SUM clause's UPON operand, a data division site) reads this element's own reports. The
    /// head and the qualifier are read by the one decomposition of a written report-group reference,
    /// <see cref="ReportGroupResolution.Words"/>, which the funnel's diagnostic reads too: a <c>reportGroupReference</c> (GENERATE,
    /// USE BEFORE REPORTING) or the <c>dataReference</c> of an UPON operand. Null when no group answers; <c>ReportGroupResolution.Resolve</c> decides what several mean.</summary>
    private ReportCandidates<(ReportModel Report, ReportGroupModel Group)>? ReportGroupsNamed(ParserRuleContext written, NameReach reach)
    {
        if (ReportGroupResolution.Words(written) is not (var head, var qualifier)) return null;
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
        return CandidatesOf(found, static f => f.Group.Entry!);
    }

    /// <summary>⛔ THE ONE NAME-AND-QUALIFIER LOOKUP OF A SUM COUNTER REFERENCE (kb/Work PB5310 review R1-1), behind the P6 Step-6
    /// watermark gate (the counters exist once <see cref="PassPhase.FilesResolved"/> has run): the SUM entries carrying the written
    /// name — only those of <paramref name="within"/> when a report's own clause writes the reference (§2023:13.18.53.3 SR4,
    /// "defined in the current report") — that the written qualifiers reach through the ONE §2023:8.4.2.2.3 SR4 walk
    /// (<see cref="QualifierWalk"/>, the report-name last), narrowed to the nearest declaring source element
    /// (§2023:8.4.6.2.1 GR3; §1985:X.1.3.8.2 GR3a at COBOL-85), so a counter of this element's own report hides a same-named counter
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
        return SumCountersNamed(name, ReferenceResolver.ReadWritten(dref).Qualifiers, within) is { } hits
            ? CandidatesOf(hits, static h => h.Family.Entry) ?? new([], [])
            : null;
    }

    /// <summary>The walk of <see cref="SumCountersReached"/> over a name and its qualifiers as read.</summary>
    private List<(ReportModel Report, ReportSumFamily Family)>? SumCountersNamed(string name, IReadOnlyList<string> qualifiers, ReportModel? within)
    {
        if (!_sumCounters.TryGetValue(Key(name), out var homonyms)) return null;
        IEnumerable<(ReportModel Report, ReportSumFamily Family)> eligible =
            within is null ? homonyms : homonyms.Where(h => ReferenceEquals(h.Report, within));
        if (!eligible.Any()) return null;
        return _data.State.Reports.NearestInScope(eligible.Where(h => QualifierWalk(h.Family.Qualification, qualifiers,
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
    /// <para>The operand is the clause's written name as the model keeps it (<see cref="WrittenDataName"/>; DESIGN-listing.md section
    /// 6.7.1, "Step 6b, remainder slice 2").</para></summary>
    internal DataItem? SumCounterOperand85(WrittenDataName operand, string subject, out bool ambiguityReported)
    {
        ambiguityReported = false;
        var (name, qualifiers) = operand;
        var edition = _data.Edition;
        if (edition.DialectLevel >= 2002 || SumCountersNamed(name, qualifiers, null) is not { } matches) return null;
        if (matches.Count > 1)
        {
            // The ONE verdict of the reference (kb/Work PB5627): the caller returns without asking the section rule, which would
            // name the same operand a second time under a 2023 clause.
            ambiguityReported = true;
            edition.Error(Editions.Diagnostics.DiagnosticCatalog.ReportSumCounterReference, $"{subject}: the reference is ambiguous — {matches.Count} "
                + $"entries establish a sum counter named '{name}'. A reference shall uniquely identify one resource ("
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
        if (dref.cobolWord()?.GetText() is not { } name || !_reportEntryLocations.TryGetValue(Key(name), out var locs)) return null;
        var qualifiers = ReferenceResolver.ReadWritten(dref).Qualifiers;
        List<ReportEntryLocation> located = [.. locs.Where(l =>
        {
            int q = 0;
            foreach (var outer in l.Qualification)
                if (q < qualifiers.Count && CobolNames.Same(outer, qualifiers[q])) q++;
            return q == qualifiers.Count;
        })];
        return CandidatesOf(located, static l => l.Entry);
    }

    /// <summary>⛔ A SUM CLAUSE'S DATA-NAME-1 — A ROLLED TOTAL (ISO §2023:13.18.54.3 SR4; kb/Work PB1294): the entries of this element's
    /// own report sections that carry a value (a SUM, SOURCE or VALUE clause, <see cref="ReportModel.EntryFamilies"/>) the written
    /// reference names, the qualifiers consumed against each entry's own hierarchy and the REPORT-name last (§2023:8.4.2.2.3 SR4,
    /// §2023:8.4.2.2.2 Format 1), exactly as a sum counter's reference is (kb/Work PB1454). Null when none; the SUM clause's binder
    /// words several (<c>ReportSectionBinder.ScreenRolledAddend</c>).</summary>
    private ReportCandidates<ReportEntryFamily>? RolledEntriesNamed(Core.DataReferenceContext dref)
    {
        if (dref.cobolWord()?.GetText() is not { } name) return null;
        var qualifiers = ReferenceResolver.ReadWritten(dref).Qualifiers;
        List<ReportEntryFamily> hits = [.. _data.State.Reports.Declared.SelectMany(r => r.EntryFamilies)
            .Where(f => CobolNames.Same(f.Name, name)
                && QualifierWalk(f.Qualification, qualifiers, q => CobolNames.Same(q, f.Report.Name)))];
        return CandidatesOf(hits, static f => f.Entry);
    }

    /// <summary>⛔ THE ONE LOOKUP OF A WRITTEN NAME AMONG THE REPORT SECTION'S ENTRIES, for the references §2023:13.8.6.2.3 bars
    /// (kb/Work PB1046): every named entry of every report this source element can name — its own and the GLOBAL ones of its
    /// containers (<see cref="ReportScope.Visible"/>, ISO §2023:13.18.27.4 GR1/GR2) — that the written <paramref name="qualifiers"/>
    /// reach through the ONE §2023:8.4.2.2.3 SR4 qualifier walk (<see cref="QualifierWalk"/>, the report-name last,
    /// §2023:8.4.2.2.2 Format 1's file-report-qualifier), narrowed to the nearest declaring source element (§2023:8.4.6.2.1 GR3).
    /// The data division's lookups ask it with the written text they hold; its answers leave the table as facts
    /// (<see cref="Declarations.ReportEntriesNamed"/>, <see cref="NearestInNameSet"/>), never as entries.</summary>
    private List<(ReportModel Report, ReportNamedEntry Entry)> EntriesNamed(string name, IReadOnlyList<string> qualifiers)
    {
        var hits = new List<(ReportModel Report, ReportNamedEntry Entry)>();
        foreach (var report in _data.State.Reports.Visible)
            foreach (var entry in report.NamedEntries)
                if (CobolNames.Same(entry.Name, name)
                    && QualifierWalk(entry.Qualification, qualifiers, q => CobolNames.Same(q, report.Name)))
                    hits.Add((report, entry));
        return _data.State.Reports.NearestInScope(hits, h => h.Report);
    }

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
            if (!DeclaresOwnDataName(entry.Name) && !names.Contains(entry.Name, CobolNames.Comparer))
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
    internal VaryingCounterNames VaryingCountersOf(ReportModel model) => new(this, model);

    /// <summary>The walk of <see cref="VaryingCountersOf"/>: the counters of the entries above the current one, and of the current
    /// one, by name. A counter declared without conflict joins <see cref="ReportModel.VaryingNames"/> once.</summary>
    internal sealed class VaryingCounterNames(SymbolTable table, ReportModel model)
    {
        private readonly List<(int Level, CobolName Name)> _enclosing = [];
        private readonly List<CobolName> _own = [];

        /// <summary>The walk reaches an entry of <paramref name="level"/>: the entries at that level or deeper are no longer above it.</summary>
        public void BeginEntry(int level)
        {
            _enclosing.RemoveAll(c => c.Level >= level);
            _own.Clear();
        }

        /// <summary>Declare one data-name-1 of the current entry's VARYING clause, <paramref name="word"/>: the counter's entry stands at
        /// it (a refused second declaration is a declaration too, and is entered).</summary>
        public VaryingCounterDeclaration Declare(Core.CobolWordContext word)
        {
            string name = word.GetText();
            table.Enter(NameClass.ReportVarying, name, PositionOf(word.Start), word);
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
