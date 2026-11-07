// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>
/// The report description entries ONE source element can name, and what a reference to them asks (docs/rearchitecture/
/// DESIGN-architecture-review.md §8.3 (1), kb/Work PB2357, step 7 of the <see cref="DataBinder"/> extract): the facts
/// <see cref="ReportSectionBinder"/> produces, read by every later phase — the post-build resolvers, the procedure
/// division's name resolution, the storage passes and the emitters. <see cref="ReportSectionBinder"/> is the only writer;
/// a reader holds this and cannot reach the binder's screens.
/// </summary>
public sealed class ReportScope
{
    /// <summary>The program unit's report description entries, in source order (ISO §2023:13.8 REPORT SECTION).
    /// (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<ReportModel> Declared => _declared;
    private readonly List<ReportModel> _declared = [];

    /// <summary>⛔ THE REPORTS A PROCEDURE DIVISION REFERENCE CAN NAME — this unit's own report description
    /// entries, then the GLOBAL ones of its containers, nearest container first (ISO §2023:13.18.27.4 GR1/GR2; kb/Work
    /// PB369). Every NAME resolution (a report-name, a report group, a sum counter, a LINE-/PAGE-COUNTER
    /// qualifier, a USE BEFORE REPORTING operand) reads THIS list; <see cref="Declared"/> stays the list of reports
    /// this unit DECLARES — the ones it constructs, owns the storage of, and validates. A container's report is
    /// never re-declared here: the entry is the container's own <see cref="ReportModel"/>, reached at run time
    /// through the <c>__outer</c> chain <see cref="Depth"/> measures.</summary>
    public IReadOnlyList<ReportModel> Visible => _inherited.Count == 0 ? _declared : _visible;
    private readonly List<ReportModel> _visible = [];
    private readonly Dictionary<ReportModel, int> _inherited = new(ReferenceEqualityComparer.Instance);

    /// <summary>REPORT SECTION <b>sum counter</b> names (case-insensitive) → every SUM ENTRY that carries the name —
    /// its <see cref="ReportSumFamily"/>, whose occurrences a subscript selects (kb/Work PB1271) — with its owning
    /// report (ISO §2023:13.18.54.4 GR5 — "If a data-name immediately follows the level number in the
    /// entry containing the SUM clause, the data-name is the name of the sum counter, not the name of the
    /// associated printable item"). The counter is IMPLICITLY defined by its entry and is NOT in
    /// <see cref="ForestSlice.ByName"/>; <see cref="ReferenceResolver"/> consults this map to build a
    /// <see cref="Model.ReportSumCounterPlace"/> so §2023:13.18.54.4 GR12 — "It is permissible for procedure division
    /// statements to alter the content of sum counters" — has somewhere to land (kb/Work PB840).
    /// <para>⛔ A LIST PER NAME, NOT ONE ENTRY (kb/Work PB882). GR1 gives every ENTRY its own counter and nothing
    /// makes two entries' data-names distinct: §2023:8.4.2.2.1's uniqueness requirement bites on a REFERENCE, not on a
    /// declaration nobody names. A scalar column here would be the same last-write-wins defect the runtime just
    /// shed; instead a duplicated name is legal to DECLARE and, unless a report-name qualifier resolves it,
    /// diagnosed where it is REFERENCED.</para>
    /// (READ-ONLY view; <see cref="DataBindState.SumCounters"/> carries the P6 Step-6 watermark gate.)</summary>
    internal IReadOnlyDictionary<string, List<(ReportModel Report, ReportSumFamily Family)>> SumCounters => _sumCounters;
    private readonly Dictionary<string, List<(ReportModel Report, ReportSumFamily Family)>> _sumCounters
        = new(CobolNames.Comparer);

    /// <summary>How many containment levels out the storage of <paramref name="report"/> lives: 0 for a report this
    /// unit declares, n for a GLOBAL report inherited from its n-th container (the length of the <c>__outer</c>
    /// chain a reference renders through).</summary>
    public int Depth(ReportModel report) => _inherited.TryGetValue(report, out int depth) ? depth : 0;

    /// <summary>ISO §2023:8.4.6.2.1 rule 3 — when a name is declared both in this source element and as a global name
    /// of a container (or in two containers), "the item in source element B is the referenced item", else the one
    /// in the nearest containing element: of <paramref name="candidates"/> keep those whose report lives at the
    /// smallest <see cref="Depth"/>. The ONE application of the rule to a report-scoped name — a report
    /// group (<c>ReportGroupResolution.Resolve</c>), a sum counter (<c>CounterResolver.SumCounterFor</c>) and an
    /// unqualified LINE-/PAGE-COUNTER (<c>ReportWriterBinder.CounterReportOf</c>) — so ambiguity (§2023:8.4.2.2.3 SR1)
    /// is asked only among candidates of one source element.</summary>
    internal List<T> NearestInScope<T>(IEnumerable<T> candidates, Func<T, ReportModel> reportOf)
    {
        var nearest = new List<T>();
        int best = int.MaxValue;
        foreach (var c in candidates)
        {
            int depth = Depth(reportOf(c));
            if (depth < best) { nearest.Clear(); best = depth; }
            if (depth == best) nearest.Add(c);
        }
        return nearest;
    }

    /// <summary>This unit's own report <paramref name="model"/>, declared by its RD entry.</summary>
    internal void Declare(ReportModel model) => _declared.Add(model);

    /// <summary>Make a container's GLOBAL report visible here at <paramref name="depth"/> (ISO §2023:13.18.27.4 GR1/GR2),
    /// nearest container first, so a name this unit declares — or a nearer container already supplied — hides the
    /// farther one (§2023:8.4.6.2). Returns false when the report-name is hidden.</summary>
    internal bool Inherit(ReportModel report, int depth)
    {
        if (Visible.Any(r => CobolNames.Same(r.Name, report.Name))) return false;
        if (_inherited.Count == 0) _visible.AddRange(_declared);
        _visible.Add(report);
        _inherited[report] = depth;
        return true;
    }

    /// <summary>Record <paramref name="family"/>'s sum counter under <paramref name="name"/>: a list per name, a
    /// duplicated name being legal to declare (kb/Work PB882).</summary>
    internal void AddSumCounter(string name, ReportModel report, ReportSumFamily family)
    {
        if (!_sumCounters.TryGetValue(name, out var homonyms)) _sumCounters[name] = homonyms = [];
        homonyms.Add((report, family));
    }

    /// <summary>The §2023:13.15.3 SR16 scan (<c>ReportSectionBinder.CheckConditionOperands</c>) refused this PRESENT WHEN
    /// condition and named its rule; the procedure phase does not bind it (one verdict per refusal, kb/Work PB4226).</summary>
    internal bool PresentWhenRefused(Core.ConditionContext cond) => _refusedPresentWhen.Contains(cond);
    internal void RefusePresentWhen(Core.ConditionContext cond) => _refusedPresentWhen.Add(cond);
    private readonly HashSet<Core.ConditionContext> _refusedPresentWhen = new(ReferenceEqualityComparer.Instance);

    /// <summary>⛔ THE ONE LOOKUP OF A WRITTEN NAME AMONG THE REPORT SECTION'S ENTRIES, for the references §2023:13.8.6.2.3
    /// bars (kb/Work PB1046): every named entry of every report this source element can name — its own and the GLOBAL
    /// ones of its containers (<see cref="Visible"/>, ISO §2023:13.18.27.4 GR1/GR2) — that the written
    /// <paramref name="qualifiers"/> reach. The walk is the ONE §2023:8.4.2.2.3 SR4 qualifier walk a sum counter's
    /// qualification goes through (<see cref="DataBinder.QualifierWalk"/>): the named levels above the entry, innermost first,
    /// then the report-name last (§2023:8.4.2.2.2 Format 1's file-report-qualifier). Candidates of more than one source
    /// element are narrowed to the nearest, as every report-scoped name is (§2023:8.4.6.2.1 rule 3,
    /// <see cref="NearestInScope{T}"/>). Empty when the word names no report entry under those qualifiers.</summary>
    internal List<(ReportModel Report, ReportNamedEntry Entry)> EntriesNamed(string name, IReadOnlyList<string> qualifiers)
    {
        var hits = new List<(ReportModel Report, ReportNamedEntry Entry)>();
        foreach (var report in Visible)
            foreach (var entry in report.NamedEntries)
                if (CobolNames.Same(entry.Name, name)
                    && DataBinder.QualifierWalk(entry.Qualification, qualifiers, q => CobolNames.Same(q, report.Name)))
                    hits.Add((report, entry));
        return NearestInScope(hits, h => h.Report);
    }

    /// <summary>⛔ ISO §2023:8.4.6.2.1 3) a)-b) FOR A REPORT ENTRY AGAINST THE DATA ITEMS OF A FARTHER SOURCE ELEMENT (kb/Work
    /// PB4196): true when the written reference reaches a report entry declared in a source element NEARER than
    /// <paramref name="declaringDepth"/>, the depth (<see cref="ForestSlice.DeclaringDepth"/>) of the data items it also reaches. "If
    /// the name is declared in source element B, the item in source element B is the referenced item" (a), otherwise "the
    /// item in the containing source element" (b): the report entry is the referenced item and the container's GLOBAL data
    /// item of the spelling is not, however many of them there are. A report entry is a data-name of the source element
    /// whose REPORT SECTION declares it (§2023:8.4.2.2.3 SR1 counts it with the ordinary ones, kb/Work PB3232), so the data-name
    /// lookup <see cref="Model.SymbolTable.NearestDeclaringData"/> asks this at the tier it narrowed to and leaves the
    /// set EMPTY, and the callers' own report-item refusal (<see cref="ReportSectionBinder.ReportEntryReferenceRefused"/>, §2023:13.8.6.2.3) names
    /// the entry. Free for a unit that inherits no global data or has no report (depth 0 is a unit's own item, which no
    /// entry is nearer than).</summary>
    internal bool NearerEntryHides(string name, IReadOnlyList<string> qualifiers, int declaringDepth) =>
        declaringDepth > 0 && Visible.Count != 0
        && EntriesNamed(name, qualifiers) is [var (report, _), ..] && Depth(report) < declaringDepth;
}
