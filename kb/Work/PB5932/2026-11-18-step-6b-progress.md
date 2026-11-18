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

**Owed before the step lands**: the section 9 measurement of the no-listing path against the parent (6b touches every
compilation's name resolution), and the full gate.

### Lead (for the controller): land step 6b by name class behind a ratchet
The design says step 6b lands only with `NameTableOwnershipDriftTests` green. The measured remainder is the binder's whole name
resolution (the data-name unit alone is NameResolution, ReferenceResolver's Identify and about 1,000 lines of DataBinder), which
one branch would carry for many sessions against a moving main. The codebase's own precedent for such a restructuring is a
ratchet (architecture-edges.json's tolerated rows, which only fall): commit the measured findings as a baseline the test may only
shrink, land each name class as it moves (each with its own `IDENTICAL` differential), and flip PB5932 when the baseline is
empty. That changes the design's landing condition, which is a decision for the codebase owner's session, not this implementer:
it is recorded here, and this branch keeps the test red as the design asks.
