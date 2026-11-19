## Step 6b, first session (w20262104k, branch `listing-step-6b`): what is built, what remains, measured

**Built and pushed** (each commit behaviour-neutral by `python scripts/arch/compare_oracle.py` against the branch's base
4527cb3dcd: `=== ARCH-ORACLE: IDENTICAL — 22641 cases ===` after every source change):

1. `NameTableOwnershipDriftTests` and its analyzer `NameTableOwnership` (tests/WiseOwl.Cobol.Tests.Unit/Listing/), written first
   and red: the five rules of DESIGN-listing.md section 6.7.1 by symbol through Roslyn over the compiler's own sources (its source
   generator run, the compilation asserted error-free), a planted compilation showing each rule able to fail, a measured file
   (`name-table-ownership.measured.txt`, tab-separated) beside the run's output. Red at 4527cb3dcd: rule (1) 65, rule (3) 188,
   rule (5) 299 (125 SAME, 130 CMP, 44 NAMEEQ), rules (2) and (4) red (no `CobolName`, no `Declarations`).
2. `SymbolTable` in the binder layer (`Binding/SymbolTable/`, partial, one file per name class), `Declarations`
   (`DeclarationFact`, the old `NonDataDeclaration`), `CobolName`, `Identify` / `IdentifiedName`, `NameReach`.
3. The classifications seeded: `string-parameter-members.json` 122 members, `name-comparisons.json` 86 sites (ceilings 122, 86).
4. The cd-names and the file-names moved in (9 tables), every reference through `Identify`.

**Measured now** (head of the branch): rule (1) **56** tables, rule (3) **63** unclassified lookups, rule (5) **206**
unclassified sites; rules (2) and (4) green.

**What remains, by name class** (rule (1)'s tables; each class moves WITH its resolution code, because a private table is read
only by the table's own members):

| Class | Tables left | Their lookups (rule 3) |
|---|---|---|
| Data, condition, index, capacity, type, debug registers (ONE unit: they resolve together) | `ForestSlice` _byName, _conditions, _capacityRegisters, _typeDecls and the views ByName, Conditions, CapacityRegisters, CapacityRegistersSoFar, TypeDecls; `OoMethodDataScope` ByName, Conditions, CapacityRegisters, TypeDecls; `IndexNameRegistry._byName`; `InheritedGlobals` _typeDecls, TypeDecls; `ClauseFacts.LapsedTypeMembers`; `ImplicitRegisters` _debugRegisters, DebugRegisters; `DataBinder._dataEntryLocations` | `NameResolution` (6), `SymbolTable`'s public lookups (7: they become private), `DataBinder`'s candidate walks (13), `ReferenceResolver.ResolveByName`, `ObjectPropertyResolver.ResolveObjectProperty`, `IndexNameRegistry.Candidates` (2) |
| Report, report group, VARYING, sum counter | `ReportScope` _sumCounters, SumCounters; `DataBindState.SumCounters`; `ReferenceResolver.VaryingScope`; `DataBinder._reportEntryLocations` | `ReportScope` Named, SumCountersNamed, EntriesNamed; `CounterResolver` (3); `ReportGroupResolution.Resolve`; `ReportSectionBinder` (2); `DataBinder.ReportEntryCandidates`, `ReportPrintablePicture` |
| Constant | `ConstantEntryBinder.ConstantScope` Bound, Declared; `InheritedGlobals` _constants, Constants | `ConstantEntryBinder.FindConstant`, `DataBinder.ReadConstant`, `LiteralEnvironment` (2), `PreBindConstants.Read` |
| Procedure | `ProcedureNameMap<T>` _first, _duplicated | `ProcedureNameMap` TryResolve, Definitions (the resolution is `ProcedureTableBuilder.ResolveProcedureWord`) |
| CONFIGURATION SECTION | `ConfigurationNames<T>._entries`; `SpecialNamesBinder.RangeCollations`, `DataBindResult.RangeCollations` | `ConfigurationNames` TryUse, TryPeek; `SpecialNamesBinder` CodedCharacterSetOf, ResolveLocaleName; `ConditionBinder.BindPartialClassName` |
| Program, function, prototype, repository | `StatementBinder` ProgramPrototypes, NestedCallables, UserFunctions; `ConfigurationSlice` ProgramSpecifiers, FunctionSpecifiers; `DataBinder._repositorySpecified`; `GroupRepositoryProvider.NoFunctions` | `StatementBinder` ProgramRestrictionOf, ProgramSignatureOf; `IntrinsicBinder.SignatureOf`; `UdfBinder.UdfBindCall` |
| OO class, interface, method (run-unit wide: the table needs a run-unit tier) | `OoClassTable` _byName, _ifaceByName, _imported, _importedInterfaces; `OoClassSymbol` _methods, _factoryMethods; `OoInterfaceSymbol` _protos, _conformingChoice | `OoClassTable` Find, FindInterface; `OoClassSymbol` FindMethod, FindFactoryMethod; `OoNameResolution` Resolve, Lookup; `OoDataBinder.DescribeNamedReference` |
| Keyed by no COBOL user word (re-keyed by a type of their own, section 6.7.2's last row) | `IntrinsicCatalog.Table`, `IntrinsicArgumentRules.Verified`, `ImplementorNameRegistry.ByName`, `FileModel.FdClauses`, `ScreenFacility.ClauseFacts`, `ExpansionPlacements.ByName`, `Polynomial._terms` | (none) |

Rule (5)'s 206: the 130 `Comparer` uses move with their tables or are re-keyed; the 76 SAME/NAMEEQ sites left are the RESOLVE
and DECLARE sites of section 6.7.2 (and the members whose evidence classes were mixed, classified site by site as each class
moves). Then `SymbolTable.Declare` replaces `SymbolTableWriter.Declare` and `SymbolTable.Finish()` the snapshot hand-off.

**Section 9** (6b touches every compilation's name resolution): each slice measures its `None` row against its parent; slice 1
is within the parent's run-to-run spread on all four programs, at most +0.23 MB (tests/WiseOwl.Cobol.Benchmarks/README.md,
the step-6b table). The last slice measures the whole step against the parent of slice 1.

### Decided: land step 6b by name class behind a temporary baseline (slice 1)

The lead this session left (one branch would carry the binder's whole name resolution for many sessions; land by class behind
a ratchet) was decided by the controller: dated file `2026-11-18-land-by-class` beside this note. Slice 1 is this branch: what
is built above, plus `tests/WiseOwl.Cobol.Tests.Unit/Listing/name-table-ownership.baseline.json` holding the measured remainder
(rule (1) 56 tables, rule (3) 63 lookups, rule (5) 206 sites: 325 entries, the test's `BaselineCeiling`): rule (1)'s and rule (3)'s entries are the
classes in the table above, and rule (5)'s move with them or are classified as each class moves. `NameTableOwnershipDriftTests` is green (9 of 9) at slice 1; both of the baseline's reds were shown
firing on the product (an entry removed: rule (1) red naming `ClauseFacts.LapsedTypeMembers`; a stale entry added: rule (1) red
`STALE`, and the ceiling test red at 326), and `TheBaseline_ReportsAFindingItLacks_AndAnEntryNoLongerFound` holds both over
planted findings. The next slice takes any whole class of the table above, lowers the baseline and the ceiling by what it moves,
and writes a dated file here.