// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The UsageInheritancePass pass: row 4 of the per-unit resolve pipeline (TypesExpanded to UsageResolved).
/// </summary>
internal sealed class UsageInheritancePass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "UsageInheritancePass";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.TypesExpanded;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>
    /// ⛔ THE USAGE-INHERITANCE PASS (P5.11e, DESIGN-data-model §2.7) — ISO §13.18.60.4 GR1, run ONCE over the
    /// completed forest: <i>"If the USAGE clause is specified or implied at a group level, it applies only to
    /// each elementary item in the group. Unless the GROUP-USAGE clause is also specified or implied, the USAGE
    /// clause applies only to each elementary item in the group and not to the group itself."</i>
    ///
    /// <para><b>THE INVARIANT THIS PASS EXISTS TO HOLD.</b> GR1 does not say a group's usage <i>influences</i>
    /// its leaves; it says the clause <i>applies to</i> each of them. So an elementary item that acquired its
    /// usage from a group shall be THE SAME ITEM the written-clause spelling produces — same representation,
    /// same width, same syntax-rule verdicts, same diagnostics. <c>01 G USAGE u. 05 A ⟨pic⟩.</c> ≡
    /// <c>01 G. 05 A ⟨pic⟩ USAGE u.</c> That equivalence is a drift test (<c>UsageInheritanceTests</c>), it
    /// enumerates the <see cref="Usage"/> enum, and it is what makes the NEXT usage automatic.</para>
    ///
    /// <para><b>Why it is ONE walk over ONE derivation (kb/Work PB495).</b> The pass used to be two — a
    /// <c>ResolveIndexItems</c> that shed a hand-listed set of synthesized profiles from group headers, and an
    /// <c>InheritUsageClauses</c> that applied a DIFFERENT hand-listed set (<c>Binary or Packed or Comp5</c>) to
    /// leaves. Neither list was derived from anything, the two disagreed, and every usage in neither fell
    /// through in SILENCE: a picture-less leaf under a group-level BINARY-SHORT or FLOAT-LONG became a
    /// ZERO-LENGTH item (a MOVE into it then NRE'd the emitter), a group-level FLOAT-SHORT left its synthesized
    /// float profile ON the group so the record struct emitted a scalar <c>float</c> and every member reference
    /// became a raw Roslyn CS1061, and a PICTURE-bearing leaf under a group-level BINARY-LONG or INDEX kept its
    /// usage-display width with no diagnostic at all. There is now ONE derivation of an elementary item's
    /// effective usage — its own clause, else the nearest enclosing one — and three consumers of it: the group
    /// header's shed, the leaf's representation, and the screens (§13.18.60.3 SR2/SR3/SR5/SR12/SR20 and
    /// §13.16.3 SR8), each of which is the SAME code the written-clause path runs.</para>
    ///
    /// <para>Runs BEFORE <see cref="InheritSignClausesPass.Run"/> — a non-DISPLAY item takes the BinaryMinus sign form
    /// regardless of any inherited SIGN clause (§13.18.52 applies only to usage-display items).</para>
    ///
    /// <para><b>⛔ EVERY WRITTEN TREE, THE TYPE DECLARATIONS TOO (kb/Work PB3682).</b> A TYPEDEF template is off
    /// <see cref="ForestSlice.Roots"/> but its clauses are source (§4.3), and §13.18.57.4 GR1 makes a TYPE subject's description
    /// "as though the data description identified by type-name-1 had been coded in place", so the rules this walk asks
    /// of a group's USAGE and GROUP-USAGE hold of the declaration whether or not anything references it. Walked over
    /// the roots alone, an unused template's <c>GROUP-USAGE NATIONAL</c> over a <c>GROUP-USAGE BIT</c> subgroup
    /// (§13.18.29.3 SR3), a group USAGE its subordinate contradicts (§13.18.60.3 SR2) or a leaf the group's usage
    /// cannot describe (§13.18.60.3 SR3) compiled clean while the byte-identical storage group was refused, and a
    /// used one was refused only through the copies its subjects held, once per reference and naming the subject.
    /// The templates are walked after the roots, and a COPY of a description (an entry a TYPE or SAME AS clause
    /// produced) takes the walk's application but not its screens where the fact is the declaration's own
    /// (<see cref="DataBinder.AnsweredByTypeDeclaration"/>): one defect, reported once, where it is written. A template's
    /// leaves also take the usage their group hands down, as a TYPE subject's do, so a template and a subject of one
    /// declaration are one description to the §8.5.3.1 comparison (<c>StrongTypeModel</c>).</para>
    /// </summary>
    public void Run(DataBinder binder)
    {
        foreach (var root in binder.State.Forest.Roots) binder.UsageInheritanceWalk(root, default);
        foreach (var declaration in binder.AllTypeDecls().ToList()) binder.UsageInheritanceWalk(declaration, default);
    }
}
