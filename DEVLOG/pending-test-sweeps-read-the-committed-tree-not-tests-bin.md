## PB3814: the test-source sweeps read the committed tree, and a drift test refuses a walk of tests/ from the file system

PB3677's merge group failed on `SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest`: it resolved a
`unit:` test-ref with `Directory.GetFiles(tests/WiseOwl.Cobol.Tests.Unit, "<Class>.cs", AllDirectories)`, which enters
the running test host's own `bin/Release/net10.0`, where `CobolFileLockTests` had left sidecar lock directories; a
delete-pending `.wolk-gone-<guid>` directory answered the walk with access denied. PB3677's finisher removed the one known
writer. This change removes the reader side.

**Re-probe.** The race has no deterministic repro (it needs a delete-pending directory under a walked `bin/`), so the
probe was the population: `git grep -n AllDirectories -- tests` lists 231 recursive listings in the test projects, and
12 classes root one at `tests/` (the note named 11; `FileHarnessOneHomeDriftTests`, `VacuousTestDriftTests` and
`SourceEncodingDriftTests` were missing from its list). Several filtered `bin/` and `obj/` only after the enumeration had
entered them, and `GateLegDriftTests` filtered `obj/` alone.

**Fix.** Each of them reads the COMMITTED tree through a declared `TreeScope` (`TrackedTree.Files`, the one reader that
PB3531 built for the whole-tree sweeps), so a build output is excluded structurally and the drift-rule index lists the
class for the files it reads. `UnresolvedTestRefs` narrows the `tests/` C# scope by the schema's `test-dir` and the
`<Class>.cs` name. `SourceEncodingDriftTests` keeps its three theory rows and takes each row's population from a declared
scope instead of a directory.

**Guard.** `TrackedTreeScopeDriftTests.NoTest_WalksTheTestsTreeFromTheFileSystem` reads every committed test source and
refuses a recursive listing (`SearchOption.AllDirectories`, `RecurseSubdirectories`) of the repository root, of `tests/` or
of a test project's directory (`TestsTreeWalks`, below); `TheWalkCheck_FindsEachShapeOfAWalkOfTheTestsTree` fires it on each
shape and on its clean twins (a listing of `src/` or of a data-only directory, a non-recursive listing, a mention outside a
listing). `docs/rearchitecture/DESIGN-test-build-ci.md` (tier 0d) states the rule.

**Review round 1.** R1-1 (blocking): the first guard matched a root spelled in the call, so a root held in a local, field,
property, lambda parameter or theory row (155 of the ~265 listings pass an identifier) went unseen. The guard is now
`TestsTreeWalks`, a Roslyn syntax walk that follows the root through those shapes to the `TestRepo` expression it was built
from and judges the path: the repository root, `tests/` and a directory under `tests/` that holds a project (named by the
committed `*.csproj` files) are hazards; a data-only directory (`tests/nist`, `tests/differential`) is not. On the tree it
sees 216 recursive listings, 209 followed to a safe directory, 7 unresolved (a root from another file), none a hazard; the
drift test holds the followed share at 90 % so the guard cannot lose its reach unseen, and its twin fires each shape
(local, field, property, assignment, helper return, lambda, `foreach`, method parameter, theory row, parent directory,
options variable, `DirectoryInfo`) and the clean twins. R1-2 (lead, fixed): `SourceEncodingDriftTests` no longer skips
`.g.cs` and `Generated/` (the population is the committed tree; the three committed `.g.cs` files have no BOM). R1-3 (lead,
`audit_annex_a1.py` resolves a test ref over the file system) is left for the clerk.

**Review round 2.** R2-1 (blocking): the resolver indexed only parameterless helpers and did not follow a collection-expression
argument or a namespace-qualified `TestRepo`, and the 90 % reach bound let every unresolved root pass, so the codebase's usual
helper shape (`Repo(string rel) => Path.Combine(TestRepo.Root, rel)`, `CompilerSrc(params string[] parts)`,
`TestRepo.Tests(["X", .. path])`) went unjudged. The resolver now binds a helper's parameters to what each call passes (a
`params` parameter to the arguments it holds; a spread it cannot expand stands for an open tail, judged by the segments before
it), follows collection expressions, arrays, `Path.Combine([...])`, `"a/b".Split('/')` and a qualified `TestRepo`, `Path` and
`Directory`, and indexes local functions. Of 216 listings it now follows 214 to a safe directory; the two it cannot are
`RunDirectoryState`'s run directories, and the drift test admits exactly those (a new unresolved root fails). R2-2 (lead,
fixed): a listing rooted at `tests/<project>/bin` or `obj`, or below, is a hazard. The twin fires each new shape, and its clean
twins (a helper called for `tests/` elsewhere but for `src/` here; `Unit("Store")`).

**Left alone.** The 217 other recursive listings root at `src/` (about 190, each with its own `bin/obj` filter), `docs/`,
`scripts/`, `.github/` and `conformance-suite/`; no failure measured the `src/` ones, so they are a separate population and
the report registers them as a lead.

`closes_rows: []`: process tooling, no inventory row.

**Gate.** `build-local` leg 1 on b76dfe383 (run 000001-20261028T173732Z): `LEG 1 ONLY (batched gating, PB2515): GREEN`, Unit 5,538/38,218 and Conformance 43/19,251 ran, Linux half (WSL clone of HEAD) GREEN; the guard, its twin and `EveryTestRef_ResolvesToARealTest` all Passed in `leg-1-Unit.trx`. Self-review: the guard reads a root spelled in the call, not one assigned first (named above); the `src/` walks are registered as a lead; no new allocation of diagnostic codes.
