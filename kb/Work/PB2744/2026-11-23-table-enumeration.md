# PB2744, 2026-11-23: the table-enumeration exclusion is built; the note is landed (wave 20262154 group D)

**Re-probe (base 3d0e3be2f5).** `Reachability.Rules` had no `table-enumeration` key, and `MembersAsync` answered every
confirmed test-only member `test-only`, so the finding `tests-scripts-ci-31--architecture--f2#1` still held. The
constructor arm stays discharged by PB2756 (the 2026-11-22 addendum).

**Fix.** `tools/ArchCensus/TableEnumeration.cs` (`TableEnumeration.Is`) states DESIGN-architecture-review section 8.7
item (a) as a declaration-judged rule: a STATIC read-only collection field or get-only property (never a string) is a
table's rows; a get-only integral `*Count` whose whole getter is the `Count`, `Length` or `*Count` of its own type's
field or property is its size. `Reachability.MembersAsync` applies it only once `SymbolFinder` confirms a test is the
member's one user (an unread table stays `unreferenced`), and `Reachability.Rules` carries the rule's text into the
record. `ArchCensus --self-test` (`tools/ArchCensus/RuleFixtures.cs`) drives five arms on planted source (a mutation
dropping the static test turned one arm FAIL, exit 1); `census.py measure` runs it after building the host, and
`ArchCensusDriftTests.CensusHost_DeclaresEveryRuleFixtureArm` holds the arms by name.

**Found on the way, fixed here.** Every census run since PB4950 round 10 (2026-11-18) died in `SymbolFinder` with
`Unexpected value UnresolvedAnalyzerReference`: `src/Directory.Build.props` pins the analyzer reference to Release,
the Debug solution build builds it at Debug only, and the workspace resolves it at Release. `census.py` now builds the
analyzer at the configuration the props file pins (`analyzer_reference`, with a self-test arm), and
`LoadedSolution.LoadAsync` refuses an unresolved analyzer reference by name.

**Measured.** `python scripts/arch/census.py --out-dir <scratch>` at 0d544ed95c: `=== ARCH CENSUS: RECORDED … ===`,
reach counts `table-enumeration 36, test-only 147, unreferenced 97` (without the rule those 36 were test-only: 183).
The 36 are the section 8.7 (a) tables (`FileControlKeyRules.Catalog*`, `ExceptionCatalog.Level3Rows`,
`Table19.StandardModes`, `IntegerOperandRules.LiteralOnlySlots`, `ArithmeticOperandRole.MixedRules`, `ZonedSign.TableCount`,
`GraphemeBreaker.RangeCount`, `CollationTable.MappingCount`, ...). The record stays in scratch (not committed: a new
record re-keys `architecture-edges.json`, the next on-demand census's change).
