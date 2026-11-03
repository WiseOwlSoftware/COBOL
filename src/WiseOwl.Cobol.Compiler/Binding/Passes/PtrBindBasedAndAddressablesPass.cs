// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

using Core = CobolParserCore;
using WiseOwl.Cobol.Compiler.Oo;

/// <summary>
/// The PtrBindBasedAndAddressables pass: row 27 of the per-unit resolve pipeline (FilesResolved to FilesResolved).
/// </summary>
internal sealed class PtrBindBasedAndAddressablesPass(Core.ProgramUnitContext program) : IBindPass
{
    /// <inheritdoc/>
    public string Name => "PtrBindBasedAndAddressables";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.Appends(FactSetKind.ImageForcedItems);

    /// <summary>The post-build data-pointer pass (runs beside <see cref="DataBinder.CallBindExternalAndGlobal"/> — the
    /// proven post-classification tier-overwrite seam): (1) every BASED root becomes a pointer-routed
    /// StringCanonical template; (2) every plain record named by a data-address-identifier (<c>ADDRESS OF x</c> — a SET sender or a CALL argument) is
    /// forced onto a per-instance cell so its address is takeable. The pre-scan is a parse-tree walk — the
    /// data pass must decide storage BEFORE any statement binds (emission shape is per-class).</summary>
    public void Run(DataBinder binder)
    {
        foreach (var root in binder.Roots.Where(r => r.IsBased))
        {
            // A rejected class keeps its RejectReason — and the rejection is a BIND-TIME diagnostic now
            // (kb/Work PB151): the bare continue left BasedPointerField null and every ALLOCATE/ADDRESS
            // reference crashed at RUN time on a program that compiled clean, while the EXTERNAL twin
            // (CallMakeExternal) always diagnosed the identical failure at bind — the two-arm shape. What the
            // shared byte cell still cannot carry is a national or pointer-class leaf; every numeric byte form
            // rides it (kb/Work PB164) and so does every boolean position, USAGE BIT packing included
            // (kb/Work PB231 — the interpolated RejectReason names the actual leaf).
            if (binder.ForceStringCanonical(root, "BASED item") is not { } cls)
            {
                binder.Edition.Error(DiagnosticCatalog.BasedRecordSubstrate,
                    $"BASED item '{root.CobolName}' has a subordinate the shared byte cell cannot carry "
                    + $"({root.Class?.RejectReason ?? "unclassified"}) — ALLOCATE/ADDRESS bridging for it "
                    + "is recognized but not yet implemented (kb/Work PB164; ISO §13.18.5 / §14.9.3)");
                continue;
            }
            string addr = NamingConvention.AddressCarrierName(root.ClrName);
            cls.BasedPointerField = addr;
            binder._ptrBasedBridges.Add((cls.BackingClrName, cls.BackingCellClrName, addr, cls.Width));
        }

        // ⛔ A FORMAL WHOSE STORAGE IS AN AREA IS LAID OVER ITS ARGUMENT'S CELL (kb/Work PB2087). ISO §14.2.3 GR8: "If the
        // argument is passed by reference, the activated runtime element operates as if the formal parameter occupies the
        // same storage area as the argument." A non-resident formal — a group, a formal another LINKAGE entry REDEFINES,
        // a formal whose ADDRESS OF is taken (CallBindLinkage) — is therefore described exactly as a BASED item is: its
        // class moves onto a cell through the ONE forcer, and the cell is the one its implicit data-address pointer
        // addresses, which the activation sets to the argument's area (CobolArgAdapt.Area). The pointer IS the formal's
        // carrier field, so the omitted-argument condition, the GLOBAL bridge and ADDRESS OF all read the one member.
        // A formal whose area the cell cannot carry keeps the image round trip at the boundary (CellCanCarry).
        // A METHOD's formals take the same rule (the method arm, kb/Work PB2087): the pointer is the formal's per-activation
        // data-address member (OoFormal.AreaPointerClrName), set at each activation to the argument's area.
        var areaFormals = binder._linkageFormals.Where(f => !f.CarrierResident).Select(f => (f.Item, Pointer: f.CarrierField))
            .Concat(binder.OoBoundMethods.SelectMany(m => m.Binding?.Formals ?? []).Where(f => !f.CarrierResident)
                .Select(f => (f.Item, Pointer: f.AreaPointerClrName)));
        foreach (var (item, pointer) in areaFormals)
        {
            if (!DataBinder.CellCanCarry(item)) continue;
            if (binder.ForceStringCanonical(item, "LINKAGE formal parameter") is not { } cls) continue;
            cls.BasedPointerField = pointer;
            binder._ptrBasedBridges.Add((cls.BackingClrName, cls.BackingCellClrName, pointer, cls.Width));
        }

        // The addressed names: this unit's own procedure division(s), plus — kb/Work PB1009 — every CONTAINED
        // program's, restricted to this unit's GLOBAL names (ResolveStorageClaimHead resolves a contained head among
        // them alone, kb/Work PB3286). §13.18.27.4 GR2 lets a contained program reference
        // a global name "without describing it again", and the storage it references is THIS unit's, so the
        // decision that the storage must be addressable belongs here, where the storage is declared. (A name the
        // contained program also declares locally shadows the global one; forcing the global's cell anyway is
        // harmless — it changes where the value lives, never what it is.)
        // ⛔ A UNIT WITH NO GLOBAL NAME HAS NO CONTAINED PROGRAM TO ASK ABOUT (kb/Work PB4302): both claim loops below keep a
        // contained head only when it resolves among this unit's GLOBAL roots (ResolveStorageClaimHead's contained arm, then
        // `CallGlobalRoots.Contains(root)`), so a unit that declares none can claim nothing from any program it contains — and
        // the question is not asked of them, instead of resolving every contained operand to throw it away.
        Core.NestedProgramContext[] containedPrograms = binder.CallGlobalRoots.Count == 0 ? [] : program.nestedProgram();
        var targets = PtrScanAddressOfTargets(binder, program).Select(t => (t.Name, t.Qualifiers, t.Method, Contained: false))
            .Concat(containedPrograms.SelectMany(binder.ClaimScans.AddressTargets)
                .Select(t => (t.Name, t.Qualifiers, Method: (OoMethodSymbol?)null, Contained: true)));
        foreach (var (name, quals, method, contained) in targets)
        {
            // An unqualified head keeps the historical first-candidate pick (a duplicate-name mis-force is
            // loud-caught at the SET bind's cell check); a QUALIFIED head resolves through the ONE §8.4.2.2
            // qualification machinery so the RIGHT record is forced. Inside a METHOD both lookups run in the
            // method's own scope (§11.7.4 GR5 — a method-local name shadows object data and was never in the
            // unit-wide maps, kb/Work PB3192), exactly as the SET bind will resolve it.
            DataItem? hit = ResolveStorageClaimHead(binder, name, quals, method, contained, firstOfSeveral: true);
            if (hit is null)
                continue;   // unresolved or ambiguous (the SET bind reports 0869 with the source text), or a container's GLOBAL item (declined, the container claims it; no 0869 follows)
            DataItem root = hit;
            while (root.Parent is { } p) root = p;
            if (contained && !binder.CallGlobalRoots.Contains(root)) continue;   // not a global name — not this unit's to force
            if (binder.AlreadyInACell(root)) continue;
            if (binder.ForceStringCanonical(root, "ADDRESS OF target record") is not { } cls) continue;   // rejected → loud
            binder.ClaimAddressableCell(cls);
        }

        // ⛔ AN ITEM PASSED BY REFERENCE LIVES IN A CELL (kb/Work PB2087, PB2089) — the activating half of §14.2.3 GR8.
        // An argument whose storage is a cell crosses with its AREA (CobolArg.Area: the cell and the offset the argument
        // begins at), and an area formal is laid over exactly those positions, so the two elements share ONE storage
        // area for the whole activation. EVERY item named as a BY REFERENCE operand is claimed by the same forcer ADDRESS
        // OF uses — a group, a character item AND a native numeric or pointer item alike, because the activating element
        // cannot know the formal's description: the activated element may lay a group, a REDEFINED or an addressed
        // formal over the argument, and then only a shared cell is "the same storage area" (a native field cannot hold
        // the characters a redefinition stores — kb/Work PB2089: with N native, `CALL "S" USING N N` against a REDEFINED
        // LK-1 and an elementary LK-2 gave LK-1 a private copy whose copy-back undid LK-2's store). Nothing else changes
        // storage, so a program that passes nothing by reference pays nothing. An operand whose area the cell cannot
        // carry, or that is ambiguous here (the CALL bind reports it), keeps its ordinary storage and crosses through its
        // carrier alone — an area formal then holds a copy, stored back at return.
        // A CLASS unit's statements live in its METHODS' procedure divisions, each resolved in its own method's scope
        // (§11.7.4 GR5) — the ADDRESS OF scan's rule (PtrScanAddressOfTargets); the INVOKE operands are scanned with the
        // CALL ones (the INVOKE arm of the same rule, kb/Work PB2087).
        // §8.4.6.6: a REPOSITORY function-prototype-name or the containing FUNCTION definition's own name — a program's
        // own name is no user function (kb/Work PB3121: this test used to admit it whatever the unit's kind).
        bool IsUserFunction(string fn) => binder.UserFunctionNames.Contains(fn) || binder.UnitIsFunction && binder.ContainingDefinitionNames.Contains(fn);
        var own = binder.OoIsClassUnit
            ? binder.OoBoundMethods.Where(m => m.Ctx?.procedureDivision() is not null)
                .SelectMany(m => binder.ClaimScans.ByReferenceOperands(m.Ctx!.procedureDivision(), IsUserFunction).Select(t => (t.Name, t.Qualifiers, t.Invoke, t.SlotsOnly, Method: (OoMethodSymbol?)m)))
            : program.procedureDivision() is { } opd
                ? binder.ClaimScans.ByReferenceOperands(opd, IsUserFunction).Select(t => (t.Name, t.Qualifiers, t.Invoke, t.SlotsOnly, Method: (OoMethodSymbol?)null))
                : [];
        var operands = own.Select(t => (t.Name, t.Qualifiers, t.Invoke, t.SlotsOnly, t.Method, Contained: false))
            .Concat(containedPrograms.SelectMany(n => binder.ClaimScans.ByReferenceOperands(n, IsUserFunction))
                .Select(t => (t.Name, t.Qualifiers, t.Invoke, t.SlotsOnly, Method: (OoMethodSymbol?)null, Contained: true)));
        foreach (var (name, quals, invoke, slotsOnly, method, contained) in operands)
        {
            if (ResolveStorageClaimHead(binder, name, quals, method, contained, firstOfSeveral: false) is not { } hit) continue;
            // ⛔ A GROUP WHOSE VALUES RIDE MANAGED SLOTS CROSSES ONLY AS AN AREA (kb/Work PB1940): a strongly-typed group
            // with an object-reference or pointer leaf has no character image, so its BY CONTENT record (§14.2.3 GR9) is
            // a detached copy of its area and a CALL's RETURNING delivery (§14.6.5) is a store into its receiver's area.
            // Either needs the operand in a cell; every other BY CONTENT operand or RETURNING receiver crosses as a value.
            if (slotsOnly && !WiseOwl.Cobol.Compiler.Oo.OoClassTable.LeafCarried(hit)) continue;
            DataItem root = hit.Root;
            if (contained && !binder.CallGlobalRoots.Contains(root)) continue;
            if (binder.AlreadyInACell(root) || root.Section is EntrySection.Linkage || !DataBinder.CellCanCarry(root)) continue;
            // Object data never crosses an INVOKE BY REFERENCE (§14.9.23.3 SR10: "Identifier-3 shall not reference a data
            // item defined in the file or working-storage section of a factory or instance object"), so a bare one is
            // passed BY CONTENT (GR6 a) 2.) and needs no cell.
            if (invoke && binder.OoIsObjectData(root)) continue;
            if (binder.ForceStringCanonical(root, "BY REFERENCE operand") is { } cls) binder.ClaimAddressableCell(cls);
        }

        // ⛔ THE ACTIVATED HALF FOR A RETURNING ITEM WHOSE VALUES RIDE MANAGED SLOTS (kb/Work PB1940): a strongly-typed
        // group with an object-reference or pointer leaf is delivered as its AREA (§14.6.5 — "the content of the data item
        // referenced by that RETURNING phrase"; CobolArgAdapt.StoreReturnArea), its characters and its references into the
        // receiver's area, so the item itself lives in a cell.
        if (!binder.OoIsClassUnit && binder.LinkageReturning is { } ret)
            binder.ClaimSlotCarriedCell(ret, "RETURNING item");
    }

    /// <summary>⛔ THE ONE RESOLUTION OF A SCANNED STORAGE-CLAIM HEAD (an <c>ADDRESS OF</c> head or a BY REFERENCE operand;
    /// kb/Work PB3286) in the scope its statement will bind in: the owning METHOD's (§11.7.4 GR5) when
    /// <paramref name="method"/> is set, else the unit's. Null when it names nothing, or is ambiguous and
    /// <paramref name="firstOfSeveral"/> is false: the statement's bind reports both (§8.4.2.2.3 SR1), and a storage
    /// claim never guesses between candidates — except where the SET bind's own permissive reading takes the first
    /// declared survivor (the ADDRESS OF arm, whose mis-force is loud-caught at that bind's cell check).
    /// <para>⛔ A CONTAINED program's head is resolved among this unit's GLOBAL names alone. §13.18.27.4 GR2 lets it
    /// "reference that name without describing it again", and §8.4.6.2.1 admits a local name only in the source element
    /// that declares it, so a container's LOCAL record that shares a name with a GLOBAL one is not a candidate: asking
    /// the whole container scope took the local one (candidate 0, or one of two) and dropped it as not global, so the
    /// global record was never put on a cell and the contained <c>SET P TO ADDRESS OF X</c> was refused with a reason
    /// that was false. A qualified head resolves through the one §8.4.2.2 qualification machinery, which the written
    /// qualifiers already narrow to the record they name.</para>
    /// <para>⛔ A HEAD THAT NAMES A CONTAINER'S GLOBAL ITEM IS NOT THIS UNIT'S TO CLAIM. The containers' GLOBAL names are
    /// visible here before this pass runs (<see cref="DataBinder.InheritContainerGlobalData"/>, at the end of the declaration half,
    /// kb/Work PB3766), but the storage they name is the container's (§13.18.27.4 GR2), and the container claims it from
    /// its own scan of its contained programs (the <c>contained</c> arm above). Claiming it here as well declared the
    /// record's cell and backing a second time in this unit's C# type beside the GLOBAL bridge to the container's
    /// (CS0102 on <c>_scell_R2</c> / <c>_redef_R2</c>, merge group 41183181188), so the answer is the head's own
    /// element only: <see cref="DataBinder.DeclaringDepth"/> 0.</para></summary>
    private static DataItem? ResolveStorageClaimHead(DataBinder binder, string name, List<string> quals, OoMethodSymbol? method, bool contained,
        bool firstOfSeveral)
    {
        var scope = method is null ? Model.Scope.Program : new Model.Scope(method.DataScope);
        DataItem? hit;
        if (quals.Count == 0)
        {
            if (!binder.Symbols.TryResolveUnqualified(name, scope, out var candidates)) return null;
            if (contained) candidates = candidates.FindAll(c => binder.CallGlobalRoots.Contains(c.Root));
            hit = candidates.Count == 1 || (firstOfSeveral && candidates.Count > 0) ? candidates[0] : null;
        }
        else
        {
            using (binder.Procedure.ActiveMethodScope.Enter(method?.DataScope))
                hit = new ReferenceResolver(binder).Names.FindItem(name, quals);
        }
        return hit is not null && binder.DeclaringDepth(hit) == 0 ? hit : null;
    }

    /// <summary>The data-names taken by every §8.4.3.11 DATA-ADDRESS-IDENTIFIER in the unit's own procedure division(s)
    /// (<see cref="StorageClaimScan.AddressTargets"/> — the head name + its OF/IN qualifiers, for EVERY operand shape: a
    /// subscripted operand forces the same containing record, the occurrence displacement being a bind-time offset over
    /// the ONE cell, never separate storage).
    /// <para>⛔ IT ASKS THE IDENTIFIER'S OWN RULE, NOT A STATEMENT (kb/Work PB239). It used to recognize only a
    /// SET Format-7 sender, so the day a second surface took the identifier — the CALL argument §14.9.4.3
    /// SR3/SR4 name — its record would have been left off cell storage and the bind would have refused it.
    /// Every surface now spells the identifier through the ONE <c>dataAddressIdentifier</c> rule, so every one
    /// of them is forced here by construction. The receiving <c>ADDRESS OF data-name-1</c> of SET Format 7 is a
    /// different rule (<c>setToReceiver</c>) and is never forced: it names a BASED item (kb/Work PB450).
    /// </para></summary>
    private static IEnumerable<(string Name, List<string> Qualifiers, OoMethodSymbol? Method)> PtrScanAddressOfTargets(DataBinder binder, Core.ProgramUnitContext program)
    {
        // A CLASS unit's statements live in its METHODS' procedure divisions — the synthetic unit OoDriver
        // binds carries the data divisions only — so each method body is scanned and its targets carry the
        // method whose scope resolves them (kb/Work PB956).
        if (binder.OoIsClassUnit)
        {
            foreach (var m in binder.OoBoundMethods)
                if (m.Ctx?.procedureDivision() is { } mpd)
                    foreach (var (n, q) in binder.ClaimScans.AddressTargets(mpd))
                        yield return (n, q, m);
            yield break;
        }
        if (program.procedureDivision() is not { } pd) yield break;
        foreach (var (n, q) in binder.ClaimScans.AddressTargets(pd))
            yield return (n, q, null);
    }
}
