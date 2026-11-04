// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// The immutable result of the Binder phase (rearch PHASE-06 Step 2 — replaces the emitter-owned
/// <c>BoundRunUnit</c>): everything the <c>VersionConformancePass</c> gates and the emit half renders, produced by
/// <see cref="BinderDriver.Bind"/>. The emitter consumes it READ-ONLY — no CodeGen write into the Binding data
/// model remains (exit criterion #2; the storage-form decision and the file-connector registry-key qualification
/// both run inside Bind).
/// <para>⛔ It carries NO parse context (kb/Work PB2398; DESIGN-backend-abstraction §1.1): the conformance pass's parse-tree
/// arm walks the group's parse root on the binder's own <c>GroupBindContext</c>, and the definition each unit was bound from
/// is on the bind session's <c>SourceMap</c>, so a backend that consumes this record cannot reach the parser.</para>
/// </summary>
/// <param name="Units">Program units in emission order (containers precede containees).</param>
/// <param name="ClassUnits">CLASS-ID units (each carrying its OBJECT + FACTORY halves).</param>
/// <param name="Interfaces">The interfaces the group defines (the pass-1 class table's, OO deep-dive D1), in definition order;
/// each is emitted where its expansion lives. The table itself stays with the binder (kb/Work PB2403): every answer the backend
/// reads of it is written into this record or the nodes it carries.</param>
/// <param name="InterfaceData">Per-interface DATA forests (prototype LINKAGE formals) for interface emission.</param>
/// <param name="OoAdapters">The §9.3.8.2.3 5a/5c2 covariant-return adapter pairs (returned by
/// <c>OoConformance.ValidateImplements</c>; the interface emitter renders one explicit implementation per pair).</param>
/// <param name="Imported">The classes and interfaces the group imported from the external repository (DESIGN-external-repository
/// §10.1): the assemblies the backend references and the types it names in another assembly.</param>
/// <param name="Turn">The group's compile-time TurnState (ISO §7.3.25; EC deep-dive D10).</param>
/// <param name="EcActive">ANY EC-model feature in use (gates every machinery emission; SSOT §18.16).</param>
/// <param name="Edition">The ISO edition (<c>EditionContext.DialectLevel</c>: 85, 2002, 2014 or 2023) the group was bound under;
/// the module's repository record names it (DESIGN-external-repository §5.4 item 2).</param>
/// <param name="AnyFiles">Any unit/class declares files or declaratives (drives the IO using + the run-unit-start
/// FileInit; the §14.6.11 termination CloseAll is runtime-side in ProgramTable.RunMain, run-unit-scoped).</param>
/// <param name="SignEncoding">The compilation's over-punch convention (<c>EditionContext.SignEncoding</c>, kb/Work PB803):
/// one per compilation, so a group with no program class (prototypes alone) states it too.</param>
/// <param name="Expansions">What the group publishes for its parameterized definitions and where each expansion lives
/// (<c>ExpansionPlacementPass</c>, DESIGN-external-repository §10.5).</param>
/// <param name="EntryUnits">The units that publish a host entry and so a repository record (DESIGN-external-repository §7.2),
/// in <paramref name="Units"/> order: every outermost unit except a prototype whose definition the same group compiles, paired by
/// the Annex C fold (<c>RepositoryPublication.EntryUnitsOf</c>; kb/Work PB3872).</param>
/// <param name="PublishesSignatures">Whether the compilation updates the external repository (<c>--repository-update on</c>,
/// DESIGN-external-repository §9.1) and bound without an error (a refused group is never emitted): the module states <c>HasSignatures</c>, every skeleton carries its <c>[CobolSkeleton]</c>, and
/// every published unit carries its record (<see cref="BoundUnit.Record"/>, <see cref="OoClassUnit.Record"/>,
/// <paramref name="InterfaceRecords"/>), written at bind time (kb/Work PB4319).</param>
/// <param name="InterfaceRecords">The record of each interface this module emits (ISO §8.13), under
/// <paramref name="PublishesSignatures"/>; empty otherwise.</param>
internal sealed record BoundCompilation(
    IReadOnlyList<BoundUnit> Units,
    IReadOnlyList<OoClassUnit> ClassUnits,
    IReadOnlyList<OoInterfaceSymbol> Interfaces,
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> InterfaceData,
    IReadOnlyList<AdapterPair> OoAdapters,
    ImportedTypes Imported,
    TurnState Turn,
    bool EcActive,
    bool AnyFiles,
    int Edition,
    WiseOwl.Cobol.Runtime.SignEncoding SignEncoding,
    ExpansionPlacements Expansions,
    IReadOnlyList<BoundUnit> EntryUnits,
    bool PublishesSignatures,
    IReadOnlyDictionary<OoInterfaceSymbol, RepositoryRecord> InterfaceRecords);
