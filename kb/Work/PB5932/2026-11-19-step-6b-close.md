## Step 6b, the closing slice's first session (w20262104z, branch `listing-step-6b-close`): what moved, what remains, measured

Dispatched as THE closing slice (controller 2026-11-19: "Move whatever the baseline still holds ... then do THE LAST SLICE's duties").
It started from origin/main 6fda8efbac (every lane landed: slices 1 to 7, the OO lane, the data-name lane's first slice) with the
baseline at 122, and an interrupted earlier run had left uncommitted edits in the worktree (the USE and WHEN registers and ten
classifications), which this session checked, simplified and committed first.

**It did not empty the baseline.** What it holds is the binder's data-name and condition-name RESOLUTION: 22 lookups that take the
name as text, about 100 call sites, half of them the data division's clause operands that the binder keeps as `(name, qualifiers)`
strings (the data-name lane's first slice measured the same). Converting them is the next slices' work; this one moved the
tables they read, so each of those lookups is now a reader of the table's own private tables, and landed what it moved behind the
lowered baseline, as the land-by-class decision (`2026-11-18-land-by-class`) provides. The LAST slice's duties (empty the baseline,
delete the file and the code that reads it, measure the whole step against the parent of slice 1, flip PB5932) stay with the slice
that empties it.

**Moved** (each change behaviour-neutral by `python scripts/arch/compare_oracle.py --no-build` against 6fda8efbac:
`=== ARCH-ORACLE: IDENTICAL — 22647 cases ===` after every source change; each commit message carries its line):

| Commit | What | Baseline |
|---|---|---|
| c16d6130dc | USE's SR8 and SR14 registers and the WHEN census of SR14/SR15 key the `FileModel` a file-name identified (the exception-name the catalog's spelling); `ProcedureReach.AlteredTargetsOf` keys the C# alter field; ten sites classified (KEYWORD: the ASSIGN device classes, IS/ARE, the EC-PROGRAM family, the RAISING exception-names twice; EXTERNAL: the EXTERNAL file connector and record groupings, a CALL literal against externalized names, the >>DEFINE variable names) | 122 -> 104 |
| 42ae7ffdb1 | The element's and each method's data-name and condition-name multimaps and the screen-names into `SymbolTable.DataNames.cs` and `SymbolTable.MethodNames`; `MethodDeclaresWord` is THE §2023:11.7.4 GR5 question (`DataBinder.MethodDeclares` and `OoMethodDataScope.IndexesDeclare` deleted); `SelectWhenConditionsOf(DataItem)` resolves a SELECT WHEN condition-name from its record; `ForestSlice` and `OoMethodDataScope` hold no name table | 104 -> 93 |
| e9e8c64591 | `SymbolTable.EndMarkerAgrees` / `EndMarkerNamesAny`: one question for every end marker (it was `ClassNames.EndMarkerAgrees` for the OO markers only); `FlagConformancePass.FileByName` identifies the written file-name | 93 -> 89 |
| 74b5a8cb08 | `SymbolTable.SpellsIndexName` (the subscript pre-filter of kb/Work PB919); `SymbolTable.WordOf` internal, so `VersionConformancePass` keys its level-01 facts by `CobolName`; review lead R2-1 of the OO lane (rule (2) sees a with-expression) | 89 -> 86 |

The classification `name-comparisons.json` 81 -> 91 (the ten sites above, which resolve no written name); `string-parameter-members.json`
unchanged (125).

**What remains: 86 entries (rule (1) 1 table, rule (3) 22 lookups, rule (5) 63 sites), by class.** Each moves WITH its resolution.

| Class | Rule (1) / (3) | Rule (5) sites | The work |
|---|---|---|---|
| Data-names and condition-names: the lookups | (3) `SymbolTable` `TryResolve`, `TryResolveUnqualified`, `TryResolveCondition`, `NearestDeclaringData`, `NearestDeclaringConditions`, `SumCounterOperand85`; `DataBinder` `QualifiedCandidates`, `QualifiedConditionCandidates`, `ConditionTier`, `EntryClauseCandidates`, `RegionClauseCandidates`, `SubtreeCandidates`, `ResolveClauseOperand`; `NameResolution` `FindItem`, `FindReportedItem`, `ResolveQualified`, `ResolveUnqualified`, `DeclarationOf`; `ReferenceResolver.ResolveByName`; `ObjectPropertyResolver.ResolveObjectProperty`; `ReportSectionBinder.ReportItemOperandRefused`; `FileModel.RecordKeyNameCandidates` | `DataBinder` `QualifierWalk`, `SubtreeCandidates`; `FileModel` (5); `ConditionBinder.BindComparison` (2); `ResolveRedefinesPass.Run`; `BitLayout.IndexOfNamed` (model layer: reads `RedefinesTargetName`, so it resolves from the resolved target or moves with the pass) | The section 6.7.1 typed method `IdentifyDataName(WrittenQualifiedName, ...)` beside `IdentifyIndexName`, private walks, and every caller handing the written words: the procedure division holds the `dataReference`; the clause operands keep a `WrittenQualifiedName` instead of `(name, qualifiers)` (FILE STATUS, RELATIVE and RECORD KEY, PADDING, LINAGE, ASSIGN USING, OCCURS DEPENDING ON, REDEFINES, RENAMES, SAME AS, LENGTH OF, a BASED claim), or are resolved from the carrier (slice 6's pattern, as `SelectWhenConditionsOf` is). |
| Data-names: the declarations | (1) `DataBinder._dataEntryLocations` | `DataBinder` `_dataEntryLocations`, `IsBeingDescribed`, `BindDeclarations` (2), `_letterlessReported`, `_repositoryNameReported`, `ScreenIndexNameUniqueness`; `LinkageBinder` (6); `OoDataBinder` (4: a method's USING / RETURNING operands, a property's subject); `CorrespondingBinder` (2); `FileControlBinder` (5: `FirstDescriptionPositions`, `ScreenSameOperands`, `ScreenDataRecords`, `ResolveFiles`, `ScreenEnclosingFileOperand`); `CommunicationSectionBinder.ReadClauses` | Declaration questions into `Declarations`; the linkage operand matches through the identified item; CORRESPONDING's pairing by the declared names of two resolved groups through the table's key. |
| The §2023:8.3.2.2 census | | `UserWordCensus` (4) | Keyed by `CobolName` through the table, or nested in it. |
| The compilation group's units | | `GroupRepositoryProvider.FunctionsByWord`; `BinderDriver.CheckFunctionUnits` (2), `CheckDefinitionNameUniqueness` | The group tier (`SymbolTable.ClassNames` is the OO half) gains the group's program and function units by word. |
| The parameterized definitions' expansion | | `OoExpansion.Expand` (12 CMP, 4 SAME), `SubstitutedTokens` (2) | The OO lane's open design question: the tier made before the expansion, or a tier of its own. |
| A TURN directive's file-name | | `TurnState.Fold` | The directive's file-name against the statement's file: by the identified file, or the table's key. |

**Section 9** (the `None` row of the front end and bind, branch 74b5a8cb08 against its parent 6fda8efbac, twice each, alternating
parent, branch, branch, parent, Release): IX113A 68.32 and 68.29 MB against 68.33 and 68.33; NC218A 30.61 and 30.66 against 30.68 and
30.64; SM201A 15.27 and 15.28 against 15.30 and 15.25; NC105A 34.60 and 35.26 against 34.61 and 34.71 (the second 0.55 MB above,
inside NC105A's run-to-run spread on one tree, which slice 3 measured at 0.7 MB). No measurable allocation moved; the `None` budget
holds (the benchmark README's step-6b table, the closing slice; DESIGN-listing.md section 9).

**Records the moves reached:** `GlobalNameClassDriftTests` (the moved tables; the model scope holds none; the method tier asks
`MethodDeclaresWord`), `MethodNameRegistrationDriftTests` (the table's two writers), `binder-member-readers.json`
(`DataBinder::Symbols` gains `ResolveRecordFormatPass`, `AcceptDisplayBinder`, `CallBinder`; `::MethodDeclares` deleted),
`BackendContract` (the deleted model tables), `SymbolTableTests`, seven Unit test files that read an item by its spelling
(`ForestItems.ItemNamed`), and the current-state docs that named the deleted members (DATA_MODEL_DESIGN, DESIGN.md, INTRINSICS_DESIGN,
OO_DESIGN, DESIGN-architecture-review, nine code comments, one conformance case's comment). No inventory row named a moved member.

### Leads carried

Every LEAD of `python scripts/orchestrator/review_findings.py show` over the seventeen `review-listing-step-*.jsonl` files, and the two
of `2026-11-19-review-leads-slices-2-and-3`, checked on this branch (origin/main 6fda8efbac plus this slice):

| Lead | State | Evidence |
|---|---|---|
| step 2 R2-1, step 3 R1-1 to R1-3, step 4a R2-1, step 4b R1-1 and R1-2, step 6b-4 R2-1, step 7 R1-1 | already fixed (slice 7, commit 5e86899090 on main) | the OO lane's dated file `2026-11-19-step-6b-oo` "Leads carried"; spot-checked here: `ListingRequest` sets `OutputBase` and `Formats` together (`WithDestination`), `TextListingRenderer.SeverityWord` reads the diagnostic's one word |
| step 6b-2 R1-1, step 6b-3 R1-1, review-leads-slices-2-and-3 items 1 and 2 | already fixed (slice 5) | the same table |
| step 6b-6 R1-1 (`ContainingDefinitionNames` outside the table) | already fixed (slice 7) | `git grep ContainingDefinitionNames -- src` finds nothing |
| step 6b-6 R1-2 (inventory prose naming `host.ProgramPrototypes`, `SelfPrototype`) | already fixed (slice 7) | neither word in `SR-8.4.3.13.3-3.json` or `SR-12.3.8.3-15.json` |
| step 6b-7 R1-1 (the design's status banner stops at slice 6) | fixed here | DESIGN-listing.md's banner names slice 7, the OO lane, the data-name lane and this slice |
| step 6b-7 R1-2 (PB2418 plans to extract members slices 6 and 7 moved) | fixed here | `kb/Work/PB2418/2026-11-19-signature-tables-moved-into-symboltable.md` |
| step 6b-oo R2-1 (rule (2)'s companion misses a with-expression; its doc claims a default) | fixed here (74b5a8cb08) | `CobolNameCreations` refuses a with-expression of every table-made type and a default of the name key; the planted fixture's `Copy` and `Empty` are reported (`TheRules_ReportEachPlantedViolation_AndNothingInsideTheTable` passes with them) |
| step 6b-data (none open; R1-2 and R1-3 fixed in that lane) | not applicable | `2026-11-19-listing-step-6b-data-review-r1` |
