## PB2360 — DataBinder extract step 10: OoDataBinder, CompilerTemps and the dissolved Oo partial

kb/Work PB2360 is step 10 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3
(1), census R0-0001). It is behavior-neutral by contract, so the proof is the §4 oracle:
`python scripts/arch/compare_oracle.py --base 7b0ec6eb1` (the merge base with PB2296 step 1) answered
`=== ARCH-ORACLE: IDENTICAL — 16143 cases ===`. It ran beside five other DataBinder groups (owner, 2026-11-02, kb/Work
PB4452), each on its own partials, so `blocked_by` is rewritten from the census order (PB2359) to the real result dependency,
PB2296 step 1.

**What the re-probe showed.** Nothing to reproduce: an Extract wave has no defect. The `DataBinder.Oo.cs` partial held seven
unrelated mechanisms behind one name, and the work was to give each its own home rather than to rename the file.

**The fix shape.**
- `Binding/OoDataBinder.cs` (`DataBinder.Oo`): a class unit's OO half, one per binder: the method roster, the method-scoped
  roots, the root-to-method owner map, the method declaration context, `BindMethodData`, `BindPropertyClauses`,
  `BindObjectRefDescriptor` and the class-member-name reservation. `OoIsClassUnit` and `OoOwnerClassName` (two init
  properties that could disagree) became one init-only `DataBinder.OoRole` (`OoUnitRole`: null for a program, an owner
  class name or null for an interface), which `OoDriver.ElementBinder` sets.
- `Binding/OoMethodDataScope.cs`: the per-method name scope, moved unchanged.
- `Binding/CompilerTemps.cs` (`DataBinder.Temps`): THE ONE constructor of a compiler temporary (`Create`, `CreateTable`,
  `CloneNode`) and the object-reference families that go through it (`ForProperty`, `ForInvocation`, `ForObjectView`,
  `ForSelf`, `ForIdentifiedObject`), with the clone pairs `StorageFormPass` re-syncs. `DataBinder` gives it a uid and a root
  list through `NextUid`, `TakeUid`, `AddRoot`, and `CopyDescriptionToTemp` (the one copy in its `CompilerTemp` scope, so
  `CopyEntryDescription` and its private `PictureAsWritten` stay private).
- `Binding/OoPendingPropertyOp.cs`: the entry type of `ProcedureBindState.PropertyOps`, no longer nested in the binder;
  `ProcedureBindState.ClaimInterleavedReceiver`. `ReceiverOpensAtRunTime` moved to `MoveBinder`, its only asker, which
  already referenced `PlaceIdentification`; left on the state type it would have added a Binding to CodeGen edge.
- The scope-aware resolver's accessors (`Symbols`, `ActiveScope`, `ScopeOf`, `NameScopeOf`), the four static-storage channels
  and `IndexDeclarationsUnder` moved, text unchanged, into `DataBinder.cs`. The static channels are written by
  `RouteStaticUnitStorage` (PB2354), which takes them with it; changing those writer lines here would only conflict with that
  step.
- Five `DataBinder` members went from `private` to `internal` because the OO binder asks for the entry binding every element
  kind shares: `BindEntries`, `DeclareDataEntries`, `BindRemainingConstants`, `CompleteDescriptionAhead`, `SealLinkageRules`.

**Goldens and tests.** None: behavior-neutral (oracle IDENTICAL). New `OoDataBinderBoundaryDriftTests` (3 facts): no
`DataBinder.Oo*` partial and no `Oo…` member on `DataBinder` beyond its door; the OO binder is made only by `DataBinder`; a
compiler temporary (`IsCompilerTemp = true`) is built only by `CompilerTemps`. Seen red with a planted `DataBinder.OoTmp.cs`.
Six existing drift tests read the old file by name and were re-pointed. One of them,
`DescriptionCopyCompletenessDriftTests.NoCodeOutsideTheOneCopy`, matched `DataBinder.cs` by `EndsWith`, which `OoDataBinder.cs`
also satisfies; it now matches the file name.

**Rows.** 48 inventory rows cited `DataBinder.Oo.cs`; one `record_verdicts` batch re-pointed them (`+48 code-location added;
48 retired`, `CONFORMS 48`, `GAP 19 → 19`) and restated the notes that named the renamed members.

**Edges.** `architecture-edges.json`: the pair `DataBinder -> PlaceIdentification` fell (ratcheted); the tolerated row
`Bound -> Binding` grew two pairs, `StatementBinder -> OoDataBinder` and `-> CompilerTemps` (the method declaration scope and
the subscript temporaries), both removed by PB2347 with the rest of that row's `StatementBinder` pairs. The census-measured
`uses` stays 39; `walkUses` is 41.

**Self-review.** Architecture: one mechanism per type, the door pinned by a drift test. Full code: the moved bodies are
byte-identical apart from `data.` qualification and renamed members; `RootOwner` is still written only in
`DataBinder.AttachToHierarchy`. Performance: `Oo` and `Temps` are lazy fields, no new hot-path allocation. Duplication: none
added; `IsClassUnit`/`OwnerClassName` no longer come from two properties.

**Gate.** At head 2c5a0843f the gate answered `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,399/39,399 ·
Characterization 37/37 cases ran (skipped 0) … (run 000001-20261102T221940Z) ===`, first time, no red on the way. The three
`OoDataBinderBoundaryDriftTests` facts are in that run's `Unit.trx`. `python scripts/semgrep/verify.py`: PASS, no rule's count rose.
