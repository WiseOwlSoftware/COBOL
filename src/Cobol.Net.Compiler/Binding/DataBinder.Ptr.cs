// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The data-pointer half of the data binder (Phase-4b increment 2 — ISO §13.18.5 BASED / §8.4.3.11 ADDRESS OF;
/// the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). Both jobs are instances of the ONE
/// cell-backing mechanism (<see cref="ForceStringCanonical"/>, the factored EXTERNAL re-basing): a BASED 01/77
/// becomes a storage TEMPLATE whose backing is a pointer-deref bridge (<c>ref CobolPtr.Deref(__addr_X, w).Ref</c>
/// — every reference windows the ADDRESSED cell at the pointer's runtime offset); an ADDRESS-OF-taken item's
/// record moves onto a per-instance <see cref="CobolNet.Runtime.StorageCell"/> so a
/// <see cref="CobolNet.Runtime.CellPointer"/> can alias its storage with structural (§8.8.4.2) equality.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The per-instance cell backings the emitter renders for ADDRESS-OF-taken records:
    /// <c>private readonly StorageCell {CellField} = new StorageCell {{ Ref = «ImageInitOf(Canonical)» }};</c> +
    /// <c>private ref string {Backing} =&gt; ref {CellField}.Ref;</c>. The seed honors the record's VALUE
    /// clauses exactly like the Tier-B stored-backing initializer (the emitter computes it via the ONE
    /// <c>FieldEmitter.ImageInitOf</c> — the based_pointer first-run lesson: a default image loses VALUE).</summary>
    internal List<(string Backing, string CellField, DataItem Canonical, int Width)> PtrAddressableBackings { get; } = [];

    /// <summary>The BASED bridges the emitter renders: the implicit data-address pointer field
    /// (<c>private ManagedPointer {AddrField} = ManagedPointer.Null;</c> — initially NULL, §13.18.5 GR2) +
    /// the deref bridge (<c>private ref string {Backing} =&gt; ref CobolPtr.Deref({AddrField}, {Width}).Ref;</c>
    /// — GR3/GR4 loud at every reference).</summary>
    internal List<(string Backing, string AddrField, int Width)> PtrBasedBridges { get; } = [];

    /// <summary>ADDRESS-OF-forced classes → their cell FIELD name (the emitter's
    /// <c>ManagedPointer.At({cell}, {offset})</c> source; bind-time validation checks membership).</summary>
    internal Dictionary<RedefinesClass, string> PtrAddressableCellOf { get; } = [];

    /// <summary>The post-build data-pointer pass (runs beside <see cref="CallBindExternalAndGlobal"/> — the
    /// proven post-classification tier-overwrite seam): (1) every BASED root becomes a pointer-routed
    /// StringCanonical template; (2) every plain record named by a <c>SET p TO ADDRESS OF x</c> operand is
    /// forced onto a per-instance cell so its address is takeable. The pre-scan is a parse-tree walk — the
    /// data pass must decide storage BEFORE any statement binds (emission shape is per-class).</summary>
    internal void PtrBindBasedAndAddressables(Core.ProgramUnitContext program)
    {
        // Class units stage LOUD (the DataBinder.Oo 0899 gate pattern — method-WS EXTERNAL/GLOBAL took the
        // same posture): the OO emitter renders FieldEmitter output only, never the program-class cell/bridge
        // loops, so a BASED item or ADDRESS-OF target in OBJECT/method data would reference undeclared
        // members (a CS0103 compiler-crash channel — the review finding).
        if (OoIsClassUnit)
        {
            if (Roots.Any(r => r.IsBased) || PtrScanAddressOfTargets(program).Any())
                Edition.Error(DiagnosticCatalog.OoBasedInClass, "BASED data or ADDRESS OF in a class definition's data "
                    + "divisions is recognized but not yet implemented (the OO cell/bridge emission — a "
                    + "named Phase-4b residue; ISO §13.18.5 / §8.4.3.11)");
            return;
        }

        foreach (var root in Roots.Where(r => r.IsBased))
        {
            // A rejected class (COMP leaf) keeps its RejectReason — every reference then fails loud.
            if (ForceStringCanonical(root, "BASED item") is not { } cls) continue;
            string addr = "__addr_" + DataItem.Sanitize(root.CobolName ?? root.CsName).ToUpperInvariant();
            cls.BasedPointerField = addr;
            PtrBasedBridges.Add((cls.BackingCsName, addr, cls.Width));
        }

        foreach (string name in PtrScanAddressOfTargets(program))
        {
            if (!ByName.TryGetValue(name, out var candidates) || candidates.Count == 0)
                continue;   // unresolved / ambiguous — the SET bind reports 0869 with the source text
            DataItem root = candidates[0];
            while (root.Parent is { } p) root = p;
            if (root.IsBased) continue;                 // ADDRESS OF a based item reads its implicit pointer (§8.6.5)
            if (root.Class is { } existing && PtrAddressableCellOf.ContainsKey(existing)) continue;
            if (root.Class is { Tier: RedefinesTier.StringCanonical } ext
                && CallExternalBackings.Any(b => b.BackingCsName == ext.BackingCsName))
                continue;                               // EXTERNAL — already cell-backed (ExternalStore); At() takes it directly
            if (ForceStringCanonical(root, "ADDRESS OF target record") is not { } cls) continue;   // rejected → loud
            string cell = "_cell_" + DataItem.Sanitize(root.CobolName ?? root.CsName).ToUpperInvariant();
            PtrAddressableCellOf[cls] = cell;
            PtrAddressableBackings.Add((cls.BackingCsName, cell, cls.Canonical, cls.Width));
        }
    }

    /// <summary>Collect the plain data-names taken by <c>SET pointer TO ADDRESS OF x</c> (alternative 2 of
    /// setAddressStatement — the ONLY ADDRESS OF surface in the grammar; alternative 1's operand is the BASED
    /// receiver, never storage-forced). Qualified / subscripted operands are skipped here — the SET bind
    /// stages them loud (0869, the named increment residue).</summary>
    private static IEnumerable<string> PtrScanAddressOfTargets(Core.ProgramUnitContext program)
    {
        if (program.procedureDivision() is not { } pd) yield break;
        foreach (var ctx in PtrDescendants(pd))
            if (ctx is Core.SetAddressStatementContext sa
                && sa.GetChild(1) is not ITerminalNode { Symbol.Type: Core.ADDRESS })   // alt 2: SET dr TO ADDRESS OF dr
            {
                var target = sa.dataReference(1);
                if (target.ChildCount == 1)   // a plain unqualified, unsubscripted name
                    yield return target.GetText();
            }
    }

    private static IEnumerable<IParseTree> PtrDescendants(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            yield return child;
            foreach (var sub in PtrDescendants(child)) yield return sub;
        }
    }
}
