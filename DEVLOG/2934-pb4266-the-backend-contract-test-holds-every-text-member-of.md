## Entry 2934 — 2026-11-01 01:49 PST — PB4266: the backend contract test holds every text member of the backend input

**The mechanism.** DESIGN-backend-abstraction §6 specified a test that fails the build when a bound node carries C#.
It did not exist, and `ICodeGenBackend` has one implementation, so nothing checked the claim that the bound tree is
the backend-neutral model. The re-probe on this tree confirmed it: `tests/` held no `BackendContract*` test, and
`PlaceNeutralityTests` held only the `Place` hierarchy (no string-returning method), the codegen-backend §6 R5 net.

**The fix.** `tests/WiseOwl.Cobol.Tests.Unit/Architecture/BackendContractDriftTests.cs` measures its population rather
than naming it. `BackendInput.Measure` walks the two layers `architecture-edges.json` places as `compiler.model` and
`compiler.bound`, starting from every type in them and following each field's type and each member's return type,
through arrays and generic arguments and to every subtype of a reached type. A new node, function result or static
table is checked the day it is declared. Every member that carries or returns text must be one of three things. It can be neutral by construction: a `*ClrName`
field, 7 of them (§1.1, §2.4). It can be a neutral row of `BackendContract.Table`: 221 rows, each a COBOL word, CLR
name, literal value, run-time datum, diagnostic text or identity key. Or it can be a leak row that names its §1.3 row
and the notes that remove it: 55 rows. L4 holds 13 (one PB3251), L6 holds 8 (PB2347), L7 holds 7 (PB4320), L8 holds 6
(PB4321) and L9 holds 21 (14 PB3252, 7 with no note yet). An unclassified member fails. A row whose member is gone fails, so the leak
rows can only shrink. A name with §6's render or C#-spelling shape (`Cs` as a word, `Path`, `Field`) can never be
neutral.

The name patterns alone would have missed real C# on the model: `PicInfo.ProfileInitializer` (a `new NumProfile {…}`
initializer), `BoundRelational.Op` ("the mapped C# operator"), `BoundLogical.Op` (`&&`/`||`/`^`) and
`BoundCodedSetClassCondition.Kind` (a runtime enum member's name). The walk found them. §1.3 gained row L9 for them,
and L4 widened from the three index fields to every storage member named by text: index cells, data-address pointers,
ALTER fields, the file instance key and a factory class. L7 and L8 now name their open notes (PB4320, PB4321) in place
of the landed PB4265, and they list the members the walk found.

The parse-context half is the edges file's rule, which keeps one rule in one place. The test asserts that
`architecture-edges.json` allows no edge from the neutral layers to `frontend.parse` or `compiler.binding`, so the
edges walk pins each such field at its tolerated row (PB2398, PB2293's waves). It also refuses the one parse-tree
reference that walk cannot see, a field of an ANTLR runtime type. A pin holds assertion 3 open: `RoslynBackend` is the
only `ICodeGenBackend`, so the seam-proof backend cannot land without the round-trip assertion that replaces the pin.

`BoundUnit.Path`, the run-unit registry key, is renamed `BoundUnit.RegistryKey` (BinderDriver, EmitterState,
ProgramEmitter), so a neutral run-time datum no longer has assertion 2's `Path` shape. `PlaceNeutralityTests` is
deleted. `TheBackendInput_HoldsEveryTypeOfTheNeutralLayers` shows that assertion 1 covers its whole population.

**Review round 1: the walk missed what a backend gets by calling the model.** The first walk started at
`BoundCompilation` and followed instance fields only. The branch review (R1-1) found 83 types of the two layers it never
reached, among them `HostType`: `HostSurface.TypeOf` returns it, and `HostEntryEmitter` writes its `CsType` (`"long"`,
`"Int128"`, `pic.ClrType`) verbatim into the host entry. A backend reaches a model type by holding a value, by calling
a member, and by naming a type outright: a static function or table, a constant, or the generated visitor interface it
implements. So the walk now starts from every type of the layers, follows static fields as well as instance fields, and
follows every member's return type. The wider input held 68 more text members. Most are neutral (the CLR-name
allocator's tables, COBOL words, diagnostic text, identity keys). Three are leaks with a remover or a §1.3 row:
`HostType.CsType` joins PB3252's L9 row beside `PicInfo.ClrType`; the runtime method names `IntrinsicCarrierRules`
keys on (`"ModScaled"`, `"Numval"`) join L9 with no note yet; and `StatementBinder`, a binder still declared in the
bound layer, is an L6 row naming PB2347, which also tolerates its two parse-tree returns. Three findings were fixed at
the root instead. `UsagePhraseRule.Admits` was a delegate field on a model record, so it now stores the admitted usages
as a set. `BitFiller.Field` held a synthesized CLR field name (`__bf0`) under a C#-shaped name, so it is now
`BitFiller.ClrName` and `BitLayout.FillerClrNameStem`, neutral by construction. The bound-visitor generator's types had
no source file a reader could find, so the generator now stamps each with `[GeneratedCode]`. The object-or-delegate
refusal stays on fields: the generated `BoundStatementTree.OwnValueParts` returns `object`, but it walks parts that are
already on the input. A planted type reached only through a method's return, with a static text constant, now witnesses
the walk's new reach, and a planted ANTLR exit fails unless an L6 row names it. Mutating the real table by dropping the
`HostType.CsType` row turned assertion 2 red with exactly that line.

**Witnesses.** Four planted self-tests fire each arm on planted types. They check that the walk reaches subtypes and
records the ANTLR exit, that an unclassified member fails while a `ClrName` does not, that a stale row fails, and that
a C#-shaped, render-shaped or `ClrName` neutral row or an unknown §1.3 row is a table defect. I also mutated the real
table: removing the `DataItem.CobolName` and `BoundRelational.Op` rows and adding a bogus row turned the field
assertion red with exactly those three lines. Restoring the table turned it green again.

**Self-review.** Silent failure: the first walk skipped `object`-typed fields, so a node held behind one would have
gone unchecked with no failure. The walk now records them as exits, and `TheBackendInput_HasNoUntypedOrDelegateField`
refuses an `object` or delegate field (none exist today, and a planted pair shows the check firing). The type-location
check also read every source file once per type; it now collects the declared names in a single pass, which took the
fixture from 4 s to 0.15 s. Architecture: one walk and one table, with the layers read from the edges file and never copied.
Duplication: the parse-context half asserts the edges rule and does not restate it. Performance: one reflection walk,
cached for the fixture. No goldens: the change is behavior-neutral apart from the internal renames, so it
adds no inventory row (closes_rows: []).

**Gates.** The first branch head, `70f7bcada`, gated in run `000003-20261101T081251Z`: `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN`. The review round's code head, `a4470fd91`, gated in run `000002-20261101T091904Z`
on the whole population, because a source-generator change cannot be attributed (kb/Work PB4269): `=== BUILD-LOCAL
GATE: GREEN — Conformance 20,404/20,404 · Unit 39,135/39,135 · Characterization 36/36 cases ran (skipped 0)`, all 13
BackendContractDriftTests passing. Its first run, `000001-20261101T090110Z`, was RED on one Unit case:
`HostWitnessTraitDriftTests` found the fixture had become a tree-wide sweep without the `Sweep` trait, because the
generated-type check read the source-generator directory. The check now reads the `[GeneratedCode]` tool's namespace
and names no path.

