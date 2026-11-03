// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>One WRITTEN level-88 entry and the entry it immediately follows (ISO §13.16.3 SR24's own subject), captured at
/// bind time because SOURCE ORDER is a fact only <c>BindEntries</c> has. <paramref name="Variable"/> null = the 88 follows no
/// entry describing a data item.</summary>
internal readonly record struct ConditionAssociation(string Name, DataItem? Variable, DiagnosticCursor At);

/// <summary>
/// What the declaration walk recorded about the CLAUSES an entry's own source wrote, for the passes that screen them after the
/// forest is built (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296 M6): a value slice of
/// <see cref="DataBindState"/> whose one writer is the data binder's entry walk (<c>BindEntries</c>, the clause-operand
/// capture) — plus <see cref="LapsedTypeMembers"/>, written once by <see cref="Passes.ExpandTypesPass"/> — and which the
/// screening passes only read. Each fact is a question the post-forest passes cannot re-derive from the tree they hold: the
/// source order of a level-88, whether an entry WROTE a clause or only acquired it, which operand a capture already refused.
/// </summary>
public sealed class ClauseFacts
{
    private readonly HashSet<DataItem> _signClauseWritten = [];
    private readonly HashSet<DataItem> _valueClauseWritten = [];
    private readonly HashSet<string> _refusedClauseOperands = new(StringComparer.Ordinal);
    private readonly List<ConditionAssociation> _conditionAssociations = [];

    /// <summary>The data description entries whose OWN source wrote a SIGN clause — the subjects §13.18.52.3 SR1/SR2 speak
    /// about (a copy through a TYPE clause or a GR5-assumed ancestor clause is not one, so it is screened once where it is
    /// declared). Filled at entry bind; read once by <see cref="Passes.CheckSignClausesPass.Run"/>.</summary>
    internal IReadOnlySet<DataItem> SignClauseWritten => _signClauseWritten;

    /// <summary>The entries that WROTE a VALUE clause (data-item formats 1 and 2) — the subject of §13.16.3 SR10, "The VALUE
    /// clause shall not be specified for data items of class index, message-tag, object, or pointer". Kept apart from
    /// <see cref="DataItem.RawValue"/> because a refused operand (`VALUE NULL`, COBOL2576) stores no value while the clause is
    /// still specified, and the acquired-usage arm is asked after the forest is built.</summary>
    internal IReadOnlySet<DataItem> ValueClauseWritten => _valueClauseWritten;

    /// <summary>Clause operands whose written shape <c>DataBinder.ScreenClauseOperandShape</c> already refused, keyed by their
    /// source text — read by the clause-operand resolvers so one fault draws one verdict. A written reference always contains a
    /// space or a parenthesis where a data-name cannot, so the key can never collide with a real one.</summary>
    internal IReadOnlySet<string> RefusedClauseOperands => _refusedClauseOperands;

    /// <summary>Every written level-88 and the entry it follows, in source order — read by
    /// <see cref="Passes.CheckConditionNameAssociationsPass.Run"/>.</summary>
    internal IReadOnlyList<ConditionAssociation> ConditionAssociations => _conditionAssociations;

    /// <summary>The members of every type declaration a TYPE clause references, by name — the "other definition of the
    /// name" of §8.4.2.2.1 GR4 ("Any other definition of the name is subordinate to a type declaration entry for which the
    /// type-name is not referenced in any TYPE clause"). Built ONCE, when <see cref="Passes.ExpandTypesPass.Run"/> has completed every declaration; null until then
    /// (a SAME AS operand resolved while the declarations are being completed walks them live, since the copies inside a
    /// declaration are still arriving). Written once.</summary>
    internal IReadOnlyDictionary<string, List<DataItem>>? LapsedTypeMembers
    {
        get;
        set => field = field is null ? value : throw new InvalidOperationException("ClauseFacts.LapsedTypeMembers is built once, by ExpandTypesPass (kb/Work PB2296)");
    }

    internal void RecordSignClauseWritten(DataItem item) => _signClauseWritten.Add(item);
    internal void RecordValueClauseWritten(DataItem item) => _valueClauseWritten.Add(item);
    internal void RecordRefusedClauseOperand(string written) => _refusedClauseOperands.Add(written);
    internal void RecordConditionAssociation(ConditionAssociation association) => _conditionAssociations.Add(association);
}
