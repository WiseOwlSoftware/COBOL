// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;

namespace WiseOwl.Cobol.Binding;

/// <summary>⛔ THE ONE WALK OVER THE VALUE NODES A REPORT DESCRIPTION HOLDS (kb/Work PB6349): the PRESENT WHEN conditions, the
/// SOURCE operands, the SUM addends and the VARYING FROM / BY expressions that a GENERATE or TERMINATE evaluates (ISO
/// §2023:14.9.16.4 GR1, §2023:13.18.53.4 GR1/GR2, §2023:13.18.54.4 GR3/GR7, §2023:13.18.41.4 GR2). They live in the report
/// MODEL, not in the statement's own bound properties: <see cref="BoundGenerate"/> holds the model by reference, and the
/// generated <see cref="BoundStatementTree.OwnValueParts"/> does not enter it, because the model is a mutable graph that
/// cycles back to itself (an item occurrence names its family, which names the report and its group). A consumer that asks
/// "does any operand this statement evaluates carry X" reads <see cref="EvaluatedBy"/> beside the statement's own value
/// parts and descends each node with <see cref="BoundValueWalk"/>; it keeps no report-specific arm, so the report's operands
/// answer every question the statement's own do (EC-ARGUMENT-FUNCTION, EC-PROGRAM-NOT-FOUND, …).
///
/// <para>The graph is walked by CONTAINMENT: each model node is entered once (a reference-equality set), so a node reached by two
/// paths (a SUM entry's occurrence is held by the report's occurrence list, its family and the counter) or by a cycle is read once.
/// The edges NOT followed are the references that name another part of the report description rather than hold one:
/// <c>Family</c>, <c>Report</c>, <c>Group</c> and <c>PrintedIn</c> point back up the containment, and
/// <c>ReportSumTerm.Upon</c> names the detail of ANOTHER report that a SUM UPON triggers on, whose own operands that report's
/// GENERATE evaluates. <c>ReportValueWalkDriftTests</c> populates every property of every model type with a sentinel node and fails
/// when the walk does not read it and the property is not one of those named references, so a model property added tomorrow
/// cannot be skipped.</para>
/// </summary>
internal static class ReportValueWalk
{
    /// <summary>The value nodes the report description evaluates when <paramref name="statement"/> runs: every operand of the
    /// report a GENERATE names (its groups, counters and item occurrences — the whole description, because a GENERATE prints
    /// the control and page groups around the detail, §2023:14.9.16.4 GR4/GR5) and of each report a TERMINATE names
    /// (§2023:14.9.46.4 GR3b, GR3c), plus the SUM terms of other reports that name a detail of that report in UPON, which a GENERATE
    /// of the detail adds (§2023:13.18.54.4 GR7c.2). A statement that presents no report evaluates none.</summary>
    internal static IEnumerable<object> EvaluatedBy(BoundStatement statement) => statement switch
    {
        BoundGenerate g => ValueNodes(g.Report),
        BoundTerminate t => t.Reports.SelectMany(ValueNodes),
        _ => [],
    };

    /// <summary>Every <see cref="BoundExpr"/>, <see cref="BoundCondition"/>, <see cref="BoundOperand"/> and
    /// <see cref="BoundBoolExpr"/> the model node <paramref name="model"/> holds, directly or through the parts of the
    /// description under it, each model node entered once.</summary>
    internal static IEnumerable<object> ValueNodes(object model)
    {
        var entered = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<object>();
        pending.Push(model);
        while (pending.Count > 0)
        {
            object node = pending.Pop();
            if (!entered.Add(node)) continue;
            foreach (object held in Holds(node))
            {
                if (held is BoundExpr or BoundCondition or BoundOperand or BoundBoolExpr) yield return held;
                else pending.Push(held);
            }
        }
    }

    /// <summary>What one model node holds directly: value nodes and the model nodes under it. A node of any other kind holds
    /// nothing. <c>ReportValueWalkDriftTests</c> makes the list complete.</summary>
    internal static IEnumerable<object> Holds(object node) => node switch
    {
        ReportModel r => Of(r.Groups, r.Sums, r.SumFamilies, r.EntryFamilies, r.ItemOccurrences, r.UponTriggeredTerms, r.Code),
        ReportCodeModel c => StepParts(c.Steps),
        ReportGroupModel g => Of(g.Lines),
        ReportLineModel l => Of(l.PresentWhen, l.Fields, l.PresentCounters),
        ReportFieldModel f => Of(f.PresentWhen, f.Sources, f.PresentCounters, f.Varyings),
        FieldReferenceSource s => Of(s.Value).Concat(StepParts(s.Steps)),
        FieldComputeSource s => Of(s.Value).Concat(StepParts(s.Steps)),
        ReportFieldSource s => StepParts(s.Steps),
        ReportVaryingUse u => Of(u.Counter),
        ReportVaryingModel v => Of(v.From, v.By, v.Group, v.Enclosing),
        ReportEntryFamily family => Of(family.Occurrences),
        ReportItemOccurrence o => Of(o.Sum, o.Source, o.Varyings, o.PresentCounters, o.PresentWhen, o.Value),
        ReportSumModel s => Of(s.Terms, s.PresentWhen, s.PresentCounters),
        ReportSumTerm t => Of(t.Addends),
        ReportSumAddend a => Of(a.Value, a.RolledFrom),
        _ => [],
    };

    /// <summary>The value nodes of the operand-evaluation steps a clause runs before each read — the function activation or the
    /// §2023:15.4 subscript store that computes an operand (kb/Work PB4517) — taken from each step and every statement nested
    /// in it. A step is bound outside any statement of the procedure division and runs inside the GENERATE or TERMINATE that
    /// evaluates the clause, so its function references are references of that statement.</summary>
    private static IEnumerable<object> StepParts(IReadOnlyList<BoundStatement>? steps) =>
        steps is null ? [] : steps.SelectMany(StatementParts);

    private static IEnumerable<object> StatementParts(BoundStatement step) =>
        step.OwnValueParts().Concat(step.StatementChildren().SelectMany(StatementParts));

    /// <summary>The non-null items of each argument, a list contributing its items and a single node itself.</summary>
    private static IEnumerable<object> Of(params object?[] parts)
    {
        foreach (object? part in parts)
        {
            if (part is null) continue;
            if (part is System.Collections.IEnumerable items)
                foreach (object? item in items)
                {
                    if (item is not null) yield return item;
                }
            else yield return part;
        }
    }
}
