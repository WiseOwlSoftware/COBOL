## Entry 3071 — 2026-11-03 21:24 PST — The backend seam, wave 20261847 group A: the repository records are model data written at bind time (PB4319), and no parse context rides a unit or the compilation (PB2398); The backend seam, wave 20261848 group A: the OO symbols carry no parse context, and a method's declaratives are bound-tree data (PB2401); The backend seam, wave 20261851 group A: the OO symbols and their closure move to compiler.model (PB2402); PB2403 step 1: AdapterPair, OoStandardClasses and the crossing-form predicates are model data; PB2403 step 2: the backend reads the OO answers as bound data, and no emitter names WiseOwl.Cobol.Compiler.Oo; PB4517: a function in a report clause runs at each evaluation, and PRESENT WHEN FUNCTION compiles; PB2361: the backend reads the frozen data binding, never the binder; PB2361 and PB2293 close: every binder member has its listed readers, and the backend seam's L6 leak is gone

### The backend seam, wave 20261847 group A: the repository records are model data written at bind time (PB4319), and no parse context rides a unit or the compilation (PB2398)

**PB4319 — the external repository's records cross the seam as data.** The re-probe on this tree matched the note: the
architecture walk measured the tolerated row `WiseOwl.Cobol.CodeGen → WiseOwl.Cobol.Binding.Repository` at 26 type pairs
(`HostEntryEmitter`, `OoEmitter` and `RepositoryAttributeRenderer` named the record family and called `RepositoryCodec.Of`
and `RepositoryCodec.KindOf` at emit time). The fix moves the encode, not only the types:

- `Binding/Repository/RepositoryRecord.cs` is `Binding/Model/RepositoryRecord.cs` (namespace `WiseOwl.Cobol.Binding.Model`),
  every type unchanged; the codec's two constants are the records' own vocabulary and moved with them
  (`RepositoryRecord.CobolEntryConvention`, `OccursRecord.OutsideTheRecord`).
- `RepositoryPublication` (new, `Binding/Repository`) holds the two publication decisions: WHICH units publish
  (`EntryUnitsOf`, moved from `HostEntryEmitter.EntryUnits`, the Annex C pairing of kb/Work PB3872) and, under
  `--repository-update on`, each published unit's record, written by the same `RepositoryCodec.Of` over the finished bound
  data at the end of `BinderDriver.Bind`. The bound compilation carries the results: `BoundUnit.Record`,
  `OoClassUnit.Record`, `BoundCompilation.InterfaceRecords`, `EntryUnits` and `PublishesSignatures`, the one fact the
  module's `HasSignatures` and each skeleton's `[CobolSkeleton]` read. `RepositoryCodec.KindOf` is `BoundUnit.RepositoryKind`;
  `RepositoryAttributeRenderer.EmittedHere` is `ExpansionPlacements.EmittedHere`, so the binder publishes a record for exactly
  the classes and interfaces the backend emits.
- The option rides `EditionContext.RepositoryUpdate`, as `--sign-encoding` and `--numeric-display` do;
  `BackendOptions.RepositoryUpdate` and the `repositoryUpdate` parameters of `EmitBound`, `ProgramEmitter.Emit`, `OoEmitter`
  and `HostEntryEmitter` are deleted (no forwarder). A check-only compile writes no module, so the driver binds it with the
  option off and it encodes nothing; a group the binder refused is never emitted, so it publishes nothing and the encoder
  never reads a refused tree.
- The record members are now on the backend input, so `BackendContract.cs` classifies them (`PicRecord.SignKind` joins
  `PicInfo.SignKind` on PB3252's L9 row).

Neutrality: `python scripts/arch/compare_oracle.py` = `=== ARCH-ORACLE: IDENTICAL — 16435 cases (baseline 0fa5f7201acd) ===`.
The ratchet emptied the row's 26 walked pairs; the landing's census deletes the census-only row (DESIGN-architecture-review
§8.1 "The landing of a remover"). New witness: `RepositoryCodecTests.TheBoundCompilation_CarriesEachPublishedUnitsRecord_OnlyWhenItUpdatesTheRepository`
(every entry unit, class and interface carries exactly the codec's record under the option, none without it). Docs:
DESIGN-codegen-backend §2.2, DESIGN-backend-abstraction §1.3 L6 and §2, DESIGN-external-repository §6.1 and its slice rows,
DESIGN-architecture-review §8.1.

**PB2398 — no parse context crosses the seam on a unit or the compilation.** The re-probe found less than the note named: the
walk measured three pairs, `BoundCompilation → CobolParserCore` (`Tree`, which nothing outside the binder read),
`BoundUnit → CobolParserCore` (`Ctx`, read by `BinderDriver` eight times and `FlagConformancePass` once) and
`CSharpEmitter → CobolParserCore` (the `CSharpEmitter.Bind` facade, "a thin shim over BinderDriver.Bind"); BoundOo.cs's half
was already gone (an unused `using` and alias remained, deleted) and `ProgramEmitter`/`StatementEmitter` read no parse
context. `BoundCompilation.Tree` is deleted; `BoundUnit.Ctx` is the binder-side `SourceMap` on `BindSession` (`Of`, and
`NameOf`, which replaced `BinderDriver.NameCtx`), built by `CollectUnits` as each unit is made; the facade is deleted and the
23 unit-test fixtures that bound through it bind with `BinderDriver.Bind` (rule 4: no shim). The facade's deletion also cut
`CSharpEmitter → BinderDriver`, `→ EditionContext` (PB2293's `CodeGen → Binding` row) and `→ DirectiveResults` (all of
PB2408's row). Oracle: IDENTICAL, 16435 cases. The census-only `Binding.Bound → Frontend.Parsing` pair left PB2398's
Removes line (its source record, the R1 targets file), as PB2417's finisher asked.

**The landing census (DESIGN-architecture-review §8.1 "The landing of a remover").** `census.py` on `762a06a33`, then the
ratchet, deleted four census-only rows at zero (`CodeGen → Binding.Repository`, `Binding.Model → Frontend.Generated`,
`CodeGen → Frontend.Generated`, `CodeGen → Frontend.Preprocessor`) and the dead allowance `frontend.pipeline → runtime`. The
census also showed five tolerated rows ABOVE the edges file's `uses` (`Binding → CodeGen` 4 → 5, `Binding → Validation`
15 → 20, `Binding.Bound → Binding` 39 → 50, `CodeGen → Binding` 105 → 140, `CodeGen.Emit → Binding` 38 → 48), which
`ArchCensusDriftTests` refused at the first gate. A census of the base `0fa5f7201` measures the same five (`CodeGen → Binding`
at 142, which this branch lowered to 140), so the growth predates the branch: landings between census `b8e4a3b58` and main
added uses to already-tolerated pairs, which the per-gate walk (pairs, not uses) cannot see and no census measured. The
five rows' `uses` are set to the measured counts, their removers unchanged, and the gap is a lead in the wave report.

### The backend seam, wave 20261848 group A: the OO symbols carry no parse context, and a method's declaratives are bound-tree data (PB2401)

**PB2401 — what the OO symbols name that the model may not.** The re-probe on this tree matched the note:
`OoClassSymbol.Ctx`, `OoInterfaceSymbol.Ctx` and `OoMethodSymbol.Ctx` held the CLASS-ID, INTERFACE-ID and METHOD-ID parse
contexts (49 reads, every one binder-side), `OoMethodBinding.Declaratives` held bound nodes on the method's signature record,
and `OptionsModel.cs` carried the two float-phrase factories over parse contexts. It found one thing the note did not list: the
class and interface symbols also carried `Expansion`, an `ExpansionOrigin` that names the REPOSITORY entry and the skeleton's
parse context, which PB2402's move would have carried into the model.

The fix extends the ONE map PB2398 made rather than adding a second: the bind session's `SourceMap` now also holds each Source
class's, interface's and method's definition and what made a class or an interface an expansion (`Add`, `Of`, `ExpansionOf`),
recorded by `OoClassTable.Build` as it makes each symbol. `Add` refuses a symbol whose origin is not Source and `Of` throws for
one, naming its origin, so PB4459's loud fault survives the move; each symbol now has one constructor, taking its origin.
Readers take the map from where they already stand: the class table keeps the session's map for its own later questions
(`MethodAt`, `ResolveOverrides`); `OoConformance`'s four validators, `CheckingFold.Method` and `ExpansionPlacementPass.Run` take
it as a parameter; `OoDriver` and `BinderDriver` read `session.Sources`. `OoDataBinder.BindMethodData` is handed the method's
definition and keeps its body (`BodyOf`, loud for a method this binder never bound), which the class-body binder and the
data-pointer pass read, so `StatementBinder` (still in the bound namespace, PB2347) names no `SourceMap`: the first shape
passed the map to `BindMethodRoster`, and the architecture walk refused the new pair on the tolerated `Binding.Bound → Binding`
row. A method's declaratives are `BoundMethod.Declaratives`, on the class's bound program beside its statements, and
`OoEmitter.EmitMethod` reads `bound.Methods[i]` with `roster[i]`. `FloatFormatPhrase` is its own binder file. Every caller
changed in the same change; no forwarder is left.

Behavior-neutral (no ISO rule changes; no golden). Neutrality: `=== ARCH-ORACLE: IDENTICAL — 16440 cases (baseline 5f50cd7dbb04, new 5f50cd7dbb04-dirty) ===` (the baseline is the branch head before this change, so the comparison isolates it from the predecessors it carries). The architecture walk and the edges file are
unchanged: every row the change touched is allowed, and PB2402's move is what removes the tolerated rows. Witness:
`OoMethodSymbolOriginTests` now pins the map (a Source symbol's definition is in it, a non-Source one is refused and answers a
fault naming its origin, a Source symbol the map never saw is a fault). Docs: DESIGN-backend-abstraction §1.3 L6, OO_DESIGN and
CONDITIONS_EXCEPTIONS_DESIGN describe the map and `BoundMethod.Declaratives`. Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 21,333/21,333 · Unit 39,633/39,633 · Characterization 37/37 cases ran (skipped 0) … run 000002-20261103T214022Z ===` on c32077fbc.

**PB2402's closure is not yet closed** (found while planning it, recorded in kb/Work PB2402): `OoMethodDataScope` names
`ConstantEntryBinder.ConstantScope` (whose `DeclaredConstant` holds four parse contexts) and `UserWordCensus`, both binder
working state in `WiseOwl.Cobol.Binding`; moved as listed, it would add new pairs to the tolerated `Binding.Model → Binding`
row, which the ratchet refuses. The method's §8.3.2.2 census and its constant-declaration state leave `OoMethodDataScope` for a
binder-side home first.

### The backend seam, wave 20261851 group A: the OO symbols and their closure move to compiler.model (PB2402)

**Carried in.** This branch continues the PB2293 campaign: the predecessor branches' PB4319, PB2398 and PB2401 commits were
cherry-picked onto main `a9f2c2897`, and the PB4509 branch (`worktree-wf_3feef266-b02-2`, PR 1086, its DEVLOG already
stamped 3056) was merged with the merge drivers. The first gate on the combined head was RED on one Unit test: the edges
file's `Binding.Bound → Binding` row said 49 walked pairs where the walk measures 48 (main removed one after the
predecessor's base). The ratchet lowered it; the re-gate was GREEN.

**The prerequisite Extract.** The re-probe confirmed the predecessor's what-if: `OoMethodDataScope`, the method's model
name scope, held two members of binder working state, its §8.3.2.2 census (`Words`) and its constant tables (`Constants`,
whose declared entries hold four parse contexts each). Moved with the symbols they would have added `Binding.Model →
Binding` pairs. They now live binder-side. The census is the class table's, one per method symbol for the whole
compilation (`OoClassTable.CensusOf(OoMethodSymbol)`, beside `_elementWords`), so the binder declaring a method's words and
every reference made in it still record into one object; `OoDataBinder.EnterMethodDeclarations` takes the symbol and
selects it. The constants are the class half's `ConstantEntryBinder._methodConstants`, keyed by the method's name scope.
The one "does this method declare the word" predicate (ISO §11.7.4 GR5) split at the same line: `OoMethodDataScope.IndexesDeclare`
asks the scope's five name indexes, `ConstantEntryBinder.MethodDeclares` the constants and the words the data division
defines before any entry binds, and `DataBinder.MethodDeclares`, THE answer, asks both; every `SymbolTable` method tier,
`TryFindTypeDecl` and `ActiveMethodDeclares` ask it. `GlobalNameClassDriftTests` holds all three (reflection over the
scope's indexes, text over both halves). Oracle: `=== ARCH-ORACLE: IDENTICAL — 16464 cases (baseline 6e13573d5abe) ===`.

**The move.** `OoClassSymbol`, `OoInterfaceSymbol`, `OoMethodSymbol`, `NamingConvention`, `OoMethodBinding`, `OoFormal`,
`OoMethodDataScope` and `OptionsModel.cs`'s types are in `Binding/Model` (namespace `WiseOwl.Cobol.Binding.Model`), and
`StandardMethod` left `OoStandardClasses.cs` for its own file; every caller's `using` and qualified name changed, and the
`using WiseOwl.Cobol.Compiler.Oo` lines that named nothing left were removed. The walk showed only falls, as the design
predicted: `Binding.Model → Compiler.Oo` 5 → 2 (`AdapterPair` and `OoClassTable`, PB2403), `Binding.Bound → Compiler.Oo`
6 → 1 (`StatementBinder → OoClassTable`, PB2347), `CodeGen → Compiler.Oo` 28 → 13, and the OPTIONS-model pairs off four
`→ Binding` rows. The ratchet lowered them and PB2402 left every `removedBy`. The symbols are now in the backend contract's
scanned namespace, so their text members were classified (DESIGN-backend-abstraction §6). Five whose names read as C#
were renamed for what they carry: `FactoryOrInstanceClrNames`, `ImplementedClrNames` (CLR type names), `RunUnitKey`,
`HalfKey` (run-unit keys) and `ImportedFrom.AssemblyFile`. `NamingConvention.FactoryInstanceField`, a storage member named
by text, joined the L4 row. Inventory: 33 rows re-sited to the moved files and two to the renamed members, by
record_verdicts batches (GAP 1 → 1). Oracle: `=== ARCH-ORACLE: IDENTICAL — 16464 cases (baseline 5471f0e35f08) ===`.
Docs: OO_DESIGN, INTRINSICS_DESIGN, DESIGN-architecture-review §8.1, DESIGN-backend-abstraction L6.

### PB2403 step 1: AdapterPair, OoStandardClasses and the crossing-form predicates are model data

PB2403 removes the backend's last reads of the binder's OO answers (`WiseOwl.Cobol.Compiler.Oo`). This branch lands its
behavior-neutral first step:
- `AdapterPair` moved to `Binding/Model`. It names only the OO symbols.
- `OoStandardClasses` moved whole to `Binding/Model`, because its closure is model types only. `BackendContract` now
  classifies its three COBOL words.
- The two crossing-form predicates moved out of `OoClassTable` into the model's `ActivationCrossing`, and every caller in
  the binder and the backend changed. `OoEmitter.OoStringCarried`, a forwarder to one of them, is deleted.

The architecture walk fell from `CodeGen → Compiler.Oo` 13 to 8 and from `Binding.Model → Compiler.Oo` 2 to 1, and the
edges file is ratcheted. The two inventory rows that cited the moved `BuildBase` are re-sited. The oracle compared the tree
with the PB4517 head and found it identical.

Step 2 is still open, and kb/Work PB2403's Progress section records its plan. The activation descriptions cannot move below
the binder, because their item half reads `MoveTable16` and `PrototypeSignatures`. They therefore become bind-time data, as
do the class lists, the imported types and the RAISING class names.

### PB2403 step 2: the backend reads the OO answers as bound data, and no emitter names WiseOwl.Cobol.Compiler.Oo

Wave 20261854 A carried the predecessor branches forward: the 13 unlanded commits of w20261852a (PB4319, PB2398, PB2401,
PB2402, PB2182, PB4517 and PB2403 step 1) were cherry-picked onto main c1f649345, and w20261852d's PB4512 branch was merged
with the merge drivers. The carried head gated GREEN (run 000001) before any new work.

Step 2 of PB2403 ends the backend's reads of the class table and of the binder's description rules. The emitters asked
`OoClassTable` (through `BoundCompilation.OoClasses` and `EcState.OoClasses`), `ActivationDescriptions` and
`OoConformance.SignatureDifference` at emit time, so a second backend would have needed the binder to describe a formal.
Now:
- `BoundCompilation` carries the defined interfaces (`Interfaces`) and the imported types (`Imported`, a new model record
  `ImportedTypes` whose `Assemblies` replaced `OoClassTable.ImportedAssemblies`) instead of the table.
- `BackendAnswers.Write` runs at the end of `BinderDriver.Bind`, beside the repository records, and writes each answer onto
  the node the backend already reads: a unit's formal and RETURNING descriptions (for a unit that checks
  EC-PROGRAM-ARG-MISMATCH or publishes a host entry), the host entry's pairs of formals with one description, a method's
  descriptions (for a method with a universal dispatch case), the RAISING phrase's classes and interfaces resolved to
  symbols (`RaisingObject`), and an EXTERNAL file's §12.4.5.3 GR1 identity.
- `OoEmitter.SelectFingerprint` moved to the binder as `FileEntryIdentity.Of`, and its result is `FileModel.SelectFingerprint`.
- A missing answer is a loud internal error in the emitter, never a silent default.

The behavior-neutrality oracle compared the new tree with the carried head: `ARCH-ORACLE: IDENTICAL — 16527 cases`. The
architecture walk now reads zero for `CodeGen → Compiler.Oo` and `Binding.Model → Compiler.Oo`, and the edges file is
ratcheted with a census recorded in this landing, so both tolerated rows are deleted and any new use is a layering red. The
`OoNameResolutionDriftTests` adjudication of the two RAISING re-lookups moved with them to `BackendAnswers`.
`ObjectReferenceIdentityDescriptionTests` gained a check that the bound descriptions are the builder's answer.
DESIGN-backend-abstraction §1.3 L6 and DESIGN-architecture-review §8 record the step as done.

PB2403 is landed with `closes_rows: []` (a behavior-neutral extract). PB2293, PB2361 and PB4273 remain open: the backend
input still carries `DataBinder` (PB2361's `DataBindResult`), and the assembly split (PB4273) waits for that.

Self-review (architecture, full code, performance, duplication): one writer of the answers and one reader per answer; every
answer the emitter needs and lacks throws an internal error; each method's descriptions are now built once at bind rather
than three times per formal at emit; `ImportedAssemblies` moved, not copied. A semgrep verify run under the gate's CPU load
reported `DiagnosticCatalog.cs` unreadable; rerun alone it passed (no rule count grew). Gate on a09065bca:
`=== BUILD-LOCAL GATE: GREEN — Conformance 21,463/21,463 · Unit 39,670/39,670 · Characterization 37/37 cases ran (skipped 0) ... (run 000002-20261104T021646Z) ===`.

### PB4517: a function in a report clause runs at each evaluation, and PRESENT WHEN FUNCTION compiles

**The defect.** `PRESENT WHEN FUNCTION MOD (WS-N, 2) = 1` was refused COBOL0899 "not yet implemented", though
§13.18.41.2 Format 1's condition-1 is any condition. The re-probe on the branch base reproduced it.

**The root was wider than the refusal.** The report clauses bind before the first statement and outside every statement
(`ReportWriterBinder.BindReportGroupClauses`). A function-identifier, a function-bearing subscript or an inline invocation
in them still registers an operand-evaluation step on the statement-scoped pending list, and the FIRST procedure statement
hoisted it. The step then never ran when a group was presented. The refusal hid this for PRESENT WHEN only. Two siblings
were silent wrong answers on the same base: `SOURCE WS-T (FUNCTION MOD (WS-N, 2) + 1)` printed spaces on every line, and
`SOURCE FUNCTION user-fn (WS-N)` printed 0 on every line.

**The fix.** Each report clause operand is now its own per-evaluation window, drained by the one
`UdfBinder.DrainPerEvaluation`:
- A PRESENT WHEN condition becomes a `BoundUdfEvaluated`, whose IIFE runs in the presence probe.
- A SUM addend and a VARYING FROM or BY become a `BoundUdfEvaluatedExpr`.
- A SOURCE operand and the CODE identifier keep their steps beside the value.

`ReportWriterEmitter.EmitSteps` runs the steps at each evaluation: before the placement in the compose, inside the sum and
rolled-total closures, before each VARYING setting, and inside the CODE read. A BY with steps is stepped rather than taken
in closed form, because §13.18.64.4 GR3 b) adds arithmetic-expression-2 at each occurrence. A GENERATE or TERMINATE that can
reach such a report takes a `BoundActivationSite` (§14.9.33.4 GR2 a) 2.). `BindReportGroupClauses` throws if it leaves
anything on the lists, so a new clause operand that skips the window fails at compile time. The COBOL0899 arm, its catalog
entry and `ReportSectionBinder.HasToken` are deleted. `docs/REPORT_WRITER_DESIGN.md` and `docs/CONFORMANCE.md` A.4.11 now
describe the window. The A.4.11 sentence also listed the GLOBAL clause and GROUP INDICATE on a relative COLUMN as staged;
both are live, and the sentence no longer says otherwise.

**Goldens.** `conformance-suite/cases/13.18.41-PRESENT-WHEN/pb4517-report-clause-functions` (2002) covers every arm.
Its values are derived in the header: ODD/EVN/ODD, 02 04 06, "BIG" on the third line only, both SUMs 012, V = 08 10 12, and
CODE B/A/B. `pb4517-present-when-function-at-85` is refused COBOL0900 by the clause's own 2002 gate.

**Left open.** A report-clause activation takes the empty §14.9.18.4 GR1 b) activator profile, not the presenting
statement's. This is a lead in the wave report.

### PB2361: the backend reads the frozen data binding, never the binder

The backend input carried the data binder. `BoundUnit.Data`, `OoClassUnit.Data` and `FactoryData`,
`BoundCompilation.InterfaceData` and `EmitContext.Data` held a `DataBinder`, and the bound units also held the
`ReferenceResolver`. An emitter could reach every table, writer and fact set of the bind through them. A second
backend would have needed the binder to emit anything (kb/Work PB2293, DESIGN-backend-abstraction §1.3 L6).

**Re-probe.** This is a behavior-neutral extract, so there was nothing to reproduce. On the carried head `ca1eed46c`, the
edges walk measured `CodeGen → Binding` at 143 pairs: 19 onto `DataBinder`, 15 onto `DataBindState`, 11 onto
`ConfigurationSlice` and 4 onto `LinkageBinder`. It measured `CodeGen.Emit → Binding` at 37 pairs. In the
`Binding.Model → Binding` row, `BoundUnit`, `OoClassUnit` and `BoundCompilation` named `DataBinder`, and the two units
named `ReferenceResolver`.

**Fix shape** (DESIGN-architecture-review §8.3 (1), step 11; the "as built" paragraph there):
- `DataBindResult` (`Binding/Model/DataBindResult.cs`) is what the backend reads. `DataBinder.Result` makes one per
  element. `ElementBinders.Freeze` calls it after the group tail and the validation passes, so the procedure binders'
  two late doors are already shut. The freeze refuses any binder whose watermark is not the tail's terminal phase.
- The result holds the frozen `ForestSlice`, `FileSlice` and `StorageTierSlice`, which moved to `Binding/Model` with it.
  It also holds `ReportScope`, `UnitChecking`, `ConfigurationFacts` (from `ConfigurationSlice.Facts`), `LinkageFacts`,
  the range collations, APPLY COMMIT, the three edition facts, and the answers the emitters used to ask the binder:
  `EmitsStaticReset`, `DeclaredCapacityRegisterTables` and the cell lifetimes.
- The binders and resolvers stay on the binder's side, in `ElementBinders` on `BindSession.Binders` (program units,
  class halves and interfaces). `BoundUnit.Refs`, `OoClassUnit.Refs` and `FactoryRefs` are deleted. Every binder-side
  reader now asks the table: `InheritStep`, the group passes, the repository provider and codec, the validation passes,
  `OoDriver`, and `BinderDriver`'s signature tables through `ElementBinders.SignatureOf`. `BinderDriver.Session` gives
  the session to a binder-side reader (a test of a binder rule) after the bind.
- `BoundUnit.AnchorOf` moved to `StorageTierSlice.AnchorOf`, and `DataBinder.IndexDeclarationsUnder` to
  `IndexDeclaration.Under`. Every caller changed in the same commit, and neither old member is left as a forwarder.

**Measured.** The behavior oracle against the carried head reports
`=== ARCH-ORACLE: IDENTICAL — 16536 cases (baseline ca1eed46c253, new 2a581b75c606-dirty) ===`. The edges walk
changed as follows:

| Row | Before (pairs) | After (pairs) |
|---|---|---|
| `CodeGen → Binding` | 143 | 72 |
| `CodeGen.Emit → Binding` | 37 | 22 |
| `Binding.Bound → Binding` | 48 | 46 |
| `Binding.Model → Binding` | 21 | 21 |

The `Binding.Model → Binding` row lost eight binder pairs and gained eight pairs onto value types that the result's
parts name. Those eight are tolerated for PB2399 (the alphabet family, `LinkageFormal`) and PB2420 (`ReportScope`,
`RangeCollationCarrier`, `NumericDisplayForm`, `CellLifetime`, `CallExternalBacking`). The census was recorded at
`2a581b75c` and the ratchet keyed the edges file to it.

**What the gate's tests changed.** The backend contract now measures the slices. Their name tables are classified as
COBOL words. The storage tier's member names, the cell lifetimes and the suppressed linkage root fields are recorded as
§1.3 L4 leaks: they always crossed the seam, but the binder hid them from the walk. Three inventory code locations
followed the moves (record_verdicts batch `w20261855a-pb2361-member-moves`, three code locations re-sited, GAP 1 → 1).

**Not closed.** PB2361 stays open on its second half. The members the earlier extracts widened from `private` to
`internal` still need their final owners and a reader-list drift test. `DataItem` immutability after binding belongs
to PB2323. PB2293 and PB4273 wait on PB2361.

### PB2361 and PB2293 close: every binder member has its listed readers, and the backend seam's L6 leak is gone

PB2361's carrier half (the entry above) put `DataBindResult` on the backend input. Its other half was the members the
ten `DataBinder` extract steps widened from `private` to `internal` so the carved binders and passes could reach them.
The note asked that none stay wider than its readers need, and that a drift test make a new reader a conscious change.

**Measured, not grepped.** Against main before the first extract landing (`1f60f2503`, before #1042) about 100 members
private then are non-private now, spread over `DataBinder` and the six binders carved from it. A text census over-counts:
names such as `Of`, `All` and `LiteralPosition` collide across types. So the edges test's metadata walk
(`TypeReferenceWalk`) learned one more answer. Given owner types, `MemberReaders` records each owner's non-private members
and the outermost types outside it whose IL tokens, signatures or attributes name each. A nested type counts as one member.
Constructors, `const`s, virtual members and compiler-synthesized members are left out, and a test is not a reader. On the
branch it measured 261 members, 15 of them read by no other type.

**The fix.** Those 15 are now `private` (16 declarations): the constant binder's `ConstantScope`, `DeclaredConstant` and
`ConstantBindState`; `DataBinder`'s `InheritGlobalSubtree`, `SymbolicRaw`, `ImpliedReportPicture` and
`NonConditionDeclarationNearerThan`; the file binder's `SelectedFiles`, `SuppressWhenOperands`, `ResolveApplyCommit` and
`ResolveFileCollating`; `OoDataBinder.OwnerClassName`; the report binder's `BareReferenceOf` and `IsParenthesized`; and
`SpecialNamesBinder.RegisterRangeCollation`. One member moved to its final owner. `DependingInsideTableRange`, the
§13.18.38.3 SR20 byte-range test, had one reader, `DependingAdmission` (the one admission of an OCCURS DEPENDING ON
object), and is now its private member. The other single-reader statics stay where they are, since their owner calls each
of them too. `CopyDescriptionToTemp` also stays: it is the door that keeps `CompilerTemps` inside the one copy scope it may
use. Two text pins in `GlobalNameClassDriftTests` followed the narrowing. That test now reads the private `ConstantScope`
by reflection.

**The drift test.** `BinderMemberReadersDriftTests` holds the walk to
`tests/WiseOwl.Cobol.Tests.Unit/Architecture/binder-member-readers.json`, which lists 245 members with their readers. It
refuses three things: a member no other type reads, a reader the file does not list, and a stale entry. On a mismatch it
writes the measured picture for copying. A planted assembly makes each refusal fire, and its keys pin the property,
nested-type and override rules.

**PB2293 closes.** It was the record of the L6 leak (the backend input carried the binder and the parse tree), and it
closes with PB2375, PB2361, PB2398 and PB2401. Re-measured, no bound unit, `BoundCompilation` or `EmitContext` field holds
a binder, a resolver or a parse context. Code generation names `DataBinder`, `ReferenceResolver` and `ResolveItem` only in
comments. DESIGN-backend-abstraction §1.3 now records L6 as DONE. It names two residues, each a tolerated edge with an open
remover: `OoEmitter`'s `BinderDriver.ExternalItemIdentity` call (PB2418) and `StatementBinder` in the bound layer (PB2347).
In the edges file, the pairs that named PB2361 next to other removers no longer name it. The two pairs it named alone are
binder-to-binder state. `StatementBinder → DataBindState` ends with PB2347, and `SymbolTable → DataBindState` with PB2399.

**PB4273 stays open, now blocked by what is left.** The assembly split cannot compile while code generation, the model and
the bound tree name compiler.binding types in tolerated rows (`CodeGen → Binding` 72 pairs, `CodeGen.Emit → Binding` 22,
`Binding.Model → Binding` 21, `Binding.Bound → Binding` 46, and three smaller rows). Its `blocked_by` now names those
rows' removers: PB2320, PB2347, PB2399, PB2405, PB2406, PB2407, PB2418, PB2419 and PB2420.

**Witnesses.** `=== ARCH-ORACLE: IDENTICAL — 16536 cases (baseline 5ee45df108fc, new 5ee45df108fc-dirty) ===`. No
inventory row changed (`closes_rows: []`, behavior-neutral). The gate's verdict line is in the wave report.

