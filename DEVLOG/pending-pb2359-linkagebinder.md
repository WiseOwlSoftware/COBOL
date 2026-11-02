## PB2359 — DataBinder extract step 9: LinkageBinder, the Linkage partial as a type

kb/Work PB2359 is step 9 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3
(1), census R0-0001), on the critical path of the backend seam (PB2293). It ran in wave 20261834 beside five other
DataBinder groups, each on its own partial, all starting from PB2296 step 1's branch, so its `blocked_by` was rewritten
from the census plan's ORDER (PB2358) to the real result dependency (PB2296: `DataBindState`, `InheritedGlobals`). It is
behavior-neutral by contract, so the proof is the §4 oracle: `python scripts/arch/compare_oracle.py` against the merge
base 2e1a4eed8377 answered `=== ARCH-ORACLE: IDENTICAL — 16143 cases ===` on the finished tree. There is no probe to
re-run: nothing the note measured is a behavior.

**What moved.** `DataBinder.Linkage.cs` is now `Binding/LinkageBinder.cs`, renamed in place so the records it also holds
(`LinkageFormal`, `CallExternalBacking`, `CalleeSignature`, `UserFunctionSignature`, `ProgramPrototype`) keep their history
and a concurrent edit to them merges. `LinkageBinder` (internal, `DataBinder.Linkage`, created on first use because it binds
over its binder's forest, as `SymbolTable` does) owns the linkage roots, the USING formals, the RETURNING item, the
EXTERNAL backings, the GLOBAL roots, the root fields the emitter must not declare, and the §13.7.3 SR4/SR5 sets, with
every writer: `CallBindLinkage`, the ANY LENGTH sweep, `SealLinkageRules` (the method arm of `OoBindMethodData` calls it
too), `CallBindExternalAndGlobal`, the externalized-name screen, and the bridge lists. Its views are
`IReadOnlyList`/`IReadOnlySet`; `SuppressRootField` is the one write the OO binder makes. Every reader changed in the same
change and the old names are deleted (`LinkageFormals` is `Linkage.Formals`, `CallGlobalRoots` is `Linkage.GlobalRoots`,
and so on): emitter, repository codec, resolver, call and condition binders, `InheritStep`, `BinderDriver`, the tests.

**What did not belong.** The partial held three things that were never the linkage binder's, and each went where its
neighbours are rather than into the new type: the inherited-depth tier of the name tables (`DeclaringDepth`,
`InheritsGlobalData`, `DeclaresOwnDataName`, `InheritGlobalSubtree`, `ReserveInheritedMemberNames`) to `DataBinder.cs`
beside `ByName`, `Conditions` and the capacity registers it orders (39 callers, none changed); the cell-backing forcer
(`ForceStringCanonical`, `CellCanCarry`, `AssignCellComponents`) to `DataBinder.Ptr.cs` beside its BASED / ADDRESS OF
callers; the unit-kind facts and `CallSeedUids` to `DataBinder.cs`. That is the stated scope corrected (CLAUDE.md rule 5):
the note said "the Linkage partial", and moving all of it would have put name-resolution state behind `Linkage.`.

**Pinned.** `LinkageBinderBoundaryDriftTests` (3 facts): every view `LinkageBinder` exposes is read-only by its declared
type, `DataBinder` declares no member of a linkage fact type (a forwarder or a second home would be one), and no
`DataBinder.Linkage.cs` partial exists. Seen red on a planted `List<DataItem>` view and a planted `DataBinder` member
returning `IReadOnlyList<LinkageFormal>`. The architecture-edges file tolerates four new `CodeGen -> LinkageBinder`
pairs (`ProgramEmitter`, `OoEmitter`, `HostEntryEmitter`, `RecordStructEmitter`), removed by PB2361 with the `DataBinder`
pairs they sit beside. Four drift tests that read the partial by path now read its successors
(`GlobalNameClassDriftTests`, `MethodNameRegistrationDriftTests`, `ByValueFormalScreenDriftTests`,
`StorageClaimHeadDriftTests`).

**Rows.** 35 inventory rows re-keyed from `DataBinder.Linkage.cs#X` to the member's new file by a `record_verdicts` batch
(35 records, 35 rows changed, GAP 19 to 19); none closed. Design §8.3 (1), `DESIGN.md`, `DESIGN-module-topology.md` and
three open notes' file paths follow the move. PB2359 is landed.

**Gate.** Run 000002-20261102T215833Z: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,396/39,396 ·
Characterization 37/37 cases ran (skipped 0) …`. Run 000001 was red on the rendered-views audit: the new boundary test
carried the `Sweep=tree` trait though it scans no tree. `semgrep` counts did not rise (one rule fell 225 to 216).

**Self-review.** Architecture: one writer per fact and a pin for it; the type takes its binder rather than the
binder's tables being copied. Full code: the moved bodies are textually the old ones with `data.` for the binder members;
the two arms of `SealLinkageRules` and `FormalIsCarrierResident` (program and method) agree. Performance: `Linkage` adds one
null test per access, and `DeclaringDepth` did not move. Duplication: none added; `ExternalizedSubjects` stays the one
population both screens read.
