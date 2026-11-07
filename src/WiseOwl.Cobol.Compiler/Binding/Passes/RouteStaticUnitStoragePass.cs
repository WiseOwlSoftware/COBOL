// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The RouteStaticUnitStorage pass: row 28 of the per-unit resolve pipeline (FilesResolved to FilesResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// A RECURSIVE unit's WS → the static-field channel (§2023:13.5.4 GR1 / §2023:14.6.2.3.3; no-op unless the binder's UnitStaticWs). AFTER the pointer pass — the last tier-overwrite seam — so Tier-B backings, EXTERNAL re-basing, and BASED/ADDRESS-OF forcing are settled facts when the roots route.
/// </para>
/// </remarks>
internal sealed class RouteStaticUnitStoragePass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "RouteStaticUnitStorage";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>Route a RECURSIVE unit's WORKING-STORAGE onto the ONE static-field channel (the mechanism the
    /// method-WS D3 pattern established — <see cref="StorageTierSlice.StaticRootFields"/> / <see cref="StorageTierSlice.StaticIndexCells"/>,
    /// consumed by <c>RecordStructEmitter</c>): a no-op unless <see cref="DataBinder.UnitStaticWs"/>. ISO basis on the
    /// flag's doc — §2023:13.5.4 GR1 (a non-initial program's / a function's WS is STATIC data) + §2023:14.6.2.3.3 (static
    /// data is in LAST-USED state: one copy shared across all activations), while the per-activation instance
    /// keeps carrying the §2023:13.6.4 GR1 automatic data and the formals. Runs AFTER the pointer pass (the last
    /// tier-overwrite seam) so Tier-B backings, EXTERNAL re-basing, and BASED/ADDRESS-OF forcing are settled:
    /// <list type="bullet">
    /// <item>a Tier-B StringCanonical WS root → its ONE string backing goes static (the OO
    ///   <see cref="OoDataBinder.RouteMethodRedefinesBackings"/> twin), plus the root's own name (harmless when the
    ///   physical is the backing — membership is tested per emitted field name);</item>
    /// <item>an EXTERNAL WS record stays OFF the channel — it is run-unit storage on the ExternalStore
    ///   (§2023:8.6.7; §2023:14.6.2.3.3 external data is ALWAYS last-used), reached through a ref-bridge, never a
    ///   per-class static;</item>
    /// <item>a WS table's INDEXED BY cell goes static with its table (the M2-OO-1h step-4 mirror — a
    ///   last-used table with per-activation indexes would silently lose SET positions);</item>
    /// <item>a BASED WS root / an ADDRESS-OF-taken WS record stages LOUD (0899) — their cell/bridge storage
    ///   is per-instance (<see cref="StorageTierSlice.AddressableBackings"/>/<see cref="StorageTierSlice.BasedBridges"/> emit instance
    ///   members), and §2023:14.6.2.3.2 #5 makes a based item's address part of the unit's static state; a silent
    ///   instance-resident cell would re-initialize per activation (§1.4 — never silent-wrong).</item>
    /// </list></summary>
    public void Run(DataBinder binder)
    {
        if (!binder.UnitStaticWs) return;
        foreach (var root in binder.State.Forest.WorkingStorageRoots)
            RouteStaticRoot(binder, root, "WORKING-STORAGE");
        // THE FILE SECTION ROUTES WITH IT (kb/Work PB168 — the review fleet's §2023:8.6.4 finding): "Data items
        // and file connectors defined in the working-storage or file section of a source element that is not
        // an initial program are static items … For static items that are not object data, there is one copy
        // in a run unit." The connector half is the static registration guard (same flag); the RECORD
        // AREA half is this — without it, the shared connector reads into a per-activation area and the
        // resumed outer activation sees stale record content (a silent wrong answer where the standard has
        // ONE area). EXTERNAL files stay off the channel (run-unit ExternalStore, §2023:8.6.7); a REPORT FD has
        // no record description to route (§2023:13.18.46). ⚠ __ResetStatics deliberately does NOT re-seed these:
        // §2023:14.6.2.3.2 action 2 names only the working-storage and local-storage sections.
        foreach (var file in binder.State.Files.Declared)
            if (!file.IsExternal)
                foreach (var record in file.Records)
                    RouteStaticRoot(binder, record, "FILE SECTION");
    }

    /// <summary>Route ONE root of a <see cref="DataBinder.UnitStaticWs"/> unit onto the static channel — shared by the
    /// WORKING-STORAGE walk and the kb/Work PB168 FILE SECTION walk (<paramref name="section"/> names the
    /// section in the staged-loud message).</summary>
    private static void RouteStaticRoot(DataBinder binder, DataItem root, string section)
    {
        if (root.Class is { Tier: RedefinesTier.StringCanonical } cls
            && binder.Linkage.ExternalBackings.Any(b => b.BackingClrName == cls.BackingClrName))
            return;   // an EXTERNAL-backed class (canonical AND views) — run-unit cell storage (§2023:14.6.2.3.3), not a class static
        if (root.IsBased)
        {
            // kb/Work PB154 — §2023:14.6.2.3.2 action 5: a BASED root in a RECURSIVE unit's static WS routes
            // its `__addr_X` bridge as a STATIC field, reset to NULL by __ResetStatics (the old code
            // REJECTED this legal source). The DATA lives in the allocated cell, so no static root field
            // joins the value-reset set. A based root whose class the substrate screen rejected
            // (COBOL1695) was already diagnosed — skip quietly.
            if (root.Class?.BasedPointerField is { } bp && !binder.State.StorageTier.AddressableCellOf.ContainsKey(root.Class))
                binder.State.StorageTier.AddStaticBasedBridgeAddr(bp);
            // An INDEXED BY cell under a BASED table is an ordinary emitted field (allocated in
            // RegisterIndexes regardless of BASED-ness) and §2023:13.5.4 GR1 makes it static like the rest of
            // the unit's WS — the bridge routes the ADDRESS, the index cells still route themselves.
            foreach (var idx in IndexDeclaration.Under(root))
                binder.State.StorageTier.AddStaticIndexCell(idx.Cell);
            return;
        }
        if (root.Class is { } ac && binder.State.StorageTier.AddressableCellOf.TryGetValue(ac, out var addrCell))
        {
            // kb/Work PB234 — an ADDRESS-OF-taken record of a RECURSIVE unit's static WS. §2023:13.5.4 GR1 makes the
            // data STATIC — one copy shared by every activation — and its storage IS the forced StorageCell (the
            // backing is a ref-bridge over it), so the CELL is what goes static; §2023:14.6.2.3.2 action 2 then
            // re-seeds it in place from __ResetStatics. Nothing in §2023:8.4.3.11, §2023:11.10.4 or §2023:14.9.5.4 forbids the
            // source this used to refuse (COBOL0899): a static item's address is simply the one copy's.
            // Every root of the class (the canonical and each REDEFINES view) names the ONE cell.
            binder.State.StorageTier.AddStaticAddressableCell(addrCell);
            foreach (var idx in IndexDeclaration.Under(root))
                binder.State.StorageTier.AddStaticIndexCell(idx.Cell);
            return;
        }
        if (root.Class is { } c && c.BasedPointerField is not null)
        {
            // A non-BASED root of a BASED item's class — a level-01 REDEFINES of it (§2023:13.18.44 does not forbid
            // one, and §2023:13.18.5.3 names only class object and dynamic length). Its storage is the based item's
            // ALLOCATED cell reached through the ONE bridge the BASED root above already routed static, so it
            // has no storage of its own to route; only its index cells do (kb/Work PB234 — this arm used to
            // refuse the source with the same COBOL0899 as the ADDRESS OF arm).
            foreach (var idx in IndexDeclaration.Under(root))
                binder.State.StorageTier.AddStaticIndexCell(idx.Cell);
            return;
        }
        if (root.Class is { Tier: RedefinesTier.StringCanonical } c2 && ReferenceEquals(c2.Canonical, root))
            binder.State.StorageTier.AddStaticRootField(c2.BackingClrName);   // Tier-B: the ONE string backing IS the storage
        binder.State.StorageTier.AddStaticRootField(root.ClrName);
        foreach (var idx in IndexDeclaration.Under(root))
            binder.State.StorageTier.AddStaticIndexCell(idx.Cell);
    }
}
