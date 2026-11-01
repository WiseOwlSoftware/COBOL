## PB4266: the backend contract test holds every text member of the backend input

**The mechanism.** DESIGN-backend-abstraction §6 specified a test that fails the build when a bound node carries C#.
It did not exist, and `ICodeGenBackend` has one implementation, so nothing checked the claim that the bound tree is
the backend-neutral model. The re-probe on this tree confirmed it: `tests/` held no `BackendContract*` test, and
`PlaceNeutralityTests` held only the `Place` hierarchy (no string-returning method), the codegen-backend §6 R5 net.

**The fix.** `tests/WiseOwl.Cobol.Tests.Unit/Architecture/BackendContractDriftTests.cs` measures its population rather
than naming it. `BackendInput.Measure` walks every type reachable by instance field from `BoundCompilation`, through
arrays and generic arguments and to every subtype of a reached type, inside the two layers `architecture-edges.json`
places as `compiler.model` and `compiler.bound`. A new node is checked the day a field reaches it. Every member that
carries or returns text (214 today) must be one of three things. It can be neutral by construction: a `*ClrName`
field, 7 of them (§1.1, §2.4). It can be a neutral row of `BackendContract.Table`: 164 rows, each a COBOL word, CLR
name, literal value, run-time datum, diagnostic text or identity key. Or it can be a leak row that names its §1.3 row
and the notes that remove it: 43 rows. L4 holds 13 (one PB3251), L7 holds 7 (PB4320), L8 holds 6 (PB4321) and L9
holds 17 (13 PB3252, 4 with no note yet). An unclassified member fails. A row whose member is gone fails, so the leak
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
deleted. `TheBackendInput_ReachesEveryPlaceAndAccessPathType` shows that assertion 1 covers its whole population.

**Witnesses.** Four planted self-tests fire each arm on planted types. They check that the walk reaches subtypes and
records the ANTLR exit, that an unclassified member fails while a `ClrName` does not, that a stale row fails, and that
a C#-shaped, render-shaped or `ClrName` neutral row or an unknown §1.3 row is a table defect. I also mutated the real
table: removing the `DataItem.CobolName` and `BoundRelational.Op` rows and adding a bogus row turned the field
assertion red with exactly those three lines. Restoring the table turned it green again.

**Self-review.** Architecture: one walk and one table, with the layers read from the edges file and never copied.
Duplication: the parse-context half asserts the edges rule and does not restate it. Performance: one reflection walk,
cached for the fixture (about 0.1 s). No goldens: the change is behavior-neutral apart from the internal rename, so it
adds no inventory row (closes_rows: []).
