// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>
/// The report description entries ONE source element can name, and what a reference to them asks (docs/rearchitecture/
/// DESIGN-architecture-review.md §8.3 (1), kb/Work PB2357, step 7 of the <see cref="DataBinder"/> extract): the facts
/// <see cref="ReportSectionBinder"/> produces, read by every later phase — the post-build resolvers, the procedure
/// division's name resolution, the storage passes and the emitters. <see cref="ReportSectionBinder"/> is the only writer
/// (through <see cref="SymbolTable"/>, which keys the report-names, the sum counters and the report entries: every lookup of a
/// written name among them is the table's, docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 5"); a reader holds
/// this and cannot reach the binder's screens.
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
    /// qualifier, a USE BEFORE REPORTING operand) reads THIS set of reports, through the symbol table; <see cref="Declared"/> stays the list of reports
    /// this unit DECLARES — the ones it constructs, owns the storage of, and validates. A container's report is
    /// never re-declared here: the entry is the container's own <see cref="ReportModel"/>, reached at run time
    /// through the <c>__outer</c> chain <see cref="Depth"/> measures.</summary>
    public IReadOnlyList<ReportModel> Visible => _inherited.Count == 0 ? _declared : _visible;
    private readonly List<ReportModel> _visible = [];

    private readonly Dictionary<ReportModel, int> _inherited = new(ReferenceEqualityComparer.Instance);

    /// <summary>How many containment levels out the storage of <paramref name="report"/> lives: 0 for a report this
    /// unit declares, n for a GLOBAL report inherited from its n-th container (the length of the <c>__outer</c>
    /// chain a reference renders through).</summary>
    public int Depth(ReportModel report) => _inherited.TryGetValue(report, out int depth) ? depth : 0;

    /// <summary>ISO §2023:8.4.6.2.1 GR3 — when a name is declared both in this source element and as a global name
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

    /// <summary>This unit's own report <paramref name="model"/>, declared by its RD entry (through <see cref="SymbolTable.Declare(ReportModel)"/>,
    /// which keys its report-name).</summary>
    internal void Declare(ReportModel model) => _declared.Add(model);

    /// <summary>Make a container's GLOBAL report visible here at <paramref name="depth"/> (ISO §2023:13.18.27.4 GR1/GR2), after
    /// every report this unit can already name. Whether its report-name is hidden (§2023:8.4.6.2) is the symbol table's question
    /// (<see cref="SymbolTable.InheritReport"/>, the one caller), asked before.</summary>
    internal void Inherit(ReportModel report, int depth)
    {
        if (_inherited.Count == 0) _visible.AddRange(_declared);
        _visible.Add(report);
        _inherited[report] = depth;
    }

    /// <summary>The §2023:13.15.3 SR16 scan (<c>ReportSectionBinder.CheckConditionOperands</c>) refused this PRESENT WHEN
    /// condition and named its rule; the procedure phase does not bind it (one verdict per refusal, kb/Work PB4226).</summary>
    internal bool PresentWhenRefused(Core.ConditionContext cond) => _refusedPresentWhen.Contains(cond);
    internal void RefusePresentWhen(Core.ConditionContext cond) => _refusedPresentWhen.Add(cond);
    private readonly HashSet<Core.ConditionContext> _refusedPresentWhen = new(ReferenceEqualityComparer.Instance);
}
