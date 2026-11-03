// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckStrongTypeDeclarations pass: row 15 of the per-unit resolve pipeline (RedefinesClassified to StrongTypeChecked).
/// </summary>
internal sealed class CheckStrongTypeDeclarationsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckStrongTypeDeclarations";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.RedefinesClassified;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.StrongTypeChecked;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>The STRONG type-declaration use restrictions that need the RESOLVED REDEFINES/RENAMES graph
    /// (ISO §13.18.57.3): <b>SR4</b> — a strongly-typed item shall not be redefined in whole or in part; <b>SR3</b> —
    /// nor renamed in whole or in part. Both → COBOL1532. (SR6, the level-1/strong-parent placement rule, is
    /// checked at clone time in <see cref="DataBinder.ExpandType"/>.) An INTERNAL redefine — a REDEFINES that is part of the same
    /// strong subtree, cloned in from the type template — is NOT flagged HERE (its subject and target share a strong
    /// root), because it is §13.18.44.3 SR14's and <c>Sr12Sr14Violation</c> reports it once, as COBOL1697
    /// (kb/Work PB1282); only an EXTERNAL redefinition of a strong item is §13.18.57.3 SR4's.
    /// <para><b>Why SR4 does not reach the internal case — kb/Work PB183's companion derivation, ANSWERED.</b>
    /// SR4 reads "If type-name-1 is described with the STRONG phrase, the subject of the entry shall not be implicitly
    /// or explicitly redefined in whole or in part", and the question was whether that overturns the internal carve-out.
    /// It does not, because SR4's SUBJECT is the entry CARRYING the TYPE clause, and §13.18.57.3 SR2 — "A data
    /// description entry in which a TYPE clause is specified shall not be followed immediately by a subordinate data
    /// description entry or a level 88 entry" — makes a subordinate REDEFINES under a TYPE entry syntactically
    /// unwritable. The only REDEFINES that can reach a strong subtree from outside is <c>01 X REDEFINES S.</c>, which
    /// the loop below already rejects. A REDEFINES written INSIDE the typedef TEMPLATE has no TYPE-clause subject at
    /// all, so SR4 never reaches it — SR14 does, and a negative
    /// (<c>pb1282-redefines-in-strong-typedef</c>) guards that.</para></summary>
    public void Run(DataBinder binder)
    {
        // ⛔ SR4's IMPLICIT arm (kb/Work PB836): "the subject of the entry shall not be implicitly or explicitly
        // redefined in whole or in part". An FD/SD's level-1 records ARE implicit redefinitions of one area
        // (§13.18.33.4 GR3), and so are the records of a record-area SAME clause (§12.4.6.4.4 GR2), so a
        // strongly-typed item anywhere in such a record is implicitly redefined by every OTHER record sharing the
        // area. The sharing is SYMMETRIC — it does not matter which record the binder anchors the area on — so
        // both sides of each implicit pair are tested, and each strong item is reported once.
        var implicitlyRedefined = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var item in binder.AllItems())
            if (item.RedefinesImplicitly && item.RedefinesTarget is { } area)
                foreach (var (record, other) in new[] { (area, item), (item, area) })
                    if (FirstStrongItemIn(record) is { } strong && implicitlyRedefined.Add(strong))
                    {
                        using var _ = binder.Edition.At(strong);
                        string by = item.RedefinesKind is RedefinitionKind.SameRecordArea
                            ? "a record-area SAME clause (ISO §12.4.6.4.4 GR2)"
                            : "the FD/SD's multiple level 1 entries (ISO §13.18.33.4 GR3)";
                        binder.Edition.Error(DiagnosticCatalog.StrongTypeRedefined, $"'{strong.CobolName ?? strong.ClrName}' is a strongly-typed item "
                            + $"and shares its storage area with '{other.CobolName ?? other.ClrName}' — an implicit redefinition by {by}: "
                            + "a strongly-typed item shall not be implicitly or explicitly redefined in whole or in "
                            + "part (ISO §13.18.57.3 SR4)");
                    }

        // A REDEFINES written in a TYPEDEF template is asked too (kb/Work PB2071): `05 G TYPE T2. 05 H REDEFINES G.`
        // in an unused template redefines the strongly-typed G from outside its strong subtree exactly as it would in
        // a record.
        foreach (var item in binder.ClauseResolutionForest())
            if (item.RedefinesKind is RedefinitionKind.Clause && item.RedefinesTarget is { } tgt
                && StrongTypeModel.IsStronglyTyped(tgt)
                && !ReferenceEquals(StrongTypeModel.StrongRoot(item), StrongTypeModel.StrongRoot(tgt)))
            {
                using var _ = binder.Edition.At(item);
                binder.Edition.Error(DiagnosticCatalog.StrongTypeRedefined, $"'{item.CobolName ?? item.ClrName}' REDEFINES strongly-typed item "
                    + $"'{tgt.CobolName ?? tgt.ClrName}': a strongly-typed item shall not be redefined in whole or in "
                    + "part (ISO §13.18.57.3 SR4)");
            }

        foreach (var owner in binder.RenamesOwners())
            foreach (var ren66 in owner.Renames66)
                if (ren66.Renames is { } ri
                    && ((ri.From is { } f && StrongTypeModel.IsStronglyTyped(f))
                        || (ri.Thru is { } t && StrongTypeModel.IsStronglyTyped(t))
                        || ri.SpanLeaves.Any(StrongTypeModel.IsStronglyTyped)))
                {
                    using var _ = binder.Edition.At(ren66);
                    binder.Edition.Error(DiagnosticCatalog.StrongTypeRenamed, $"RENAMES '{ren66.CobolName ?? ren66.ClrName}' renames a "
                        + "strongly-typed item in whole or in part — prohibited (ISO §13.18.57.3 SR3)");
                }
    }

    /// <summary>The outermost strongly-typed item in <paramref name="record"/>'s subtree (the record itself when it
    /// is a TYPE subject naming a STRONG type), or null — the "in whole or in part" population of §13.18.57.3 SR4
    /// for a record that is implicitly redefined as a whole (kb/Work PB836).</summary>
    private static DataItem? FirstStrongItemIn(DataItem record)
    {
        if (record.StrongType) return record;
        foreach (var c in record.Children)
            if (FirstStrongItemIn(c) is { } s) return s;
        return null;
    }
}
