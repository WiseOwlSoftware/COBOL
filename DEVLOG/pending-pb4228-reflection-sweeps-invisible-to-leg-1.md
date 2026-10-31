## PB4228 filed: leg 1 cannot see a drift test that reflects over a whole assembly

PR #894 (PB107) bounced out of the merge queue twice on 2026-10-31, and PR #896 failed with it in the same merge group.
The red was `RunUnitStateDriftTests.NoStaticMutableCollection_OutsideTheDocumentedProcessStores`: the branch added a
static `ConditionalWeakTable` to the runtime without registering it. The branch's GREEN local gate lists the test as
NOT RUN. That test reflects over every field of the runtime assembly. The drift-rule index sees only file paths and
declared tree scopes, and the coverage impact map sees only executed code, so neither one puts the test in leg 1 for a
runtime change. The operator registered the field by hand (`d917370e0`), and the rest of the queue then passed.

kb/Work PB4228 (cluster PB1957, MAJOR) records the case and the fix's shape. A declared assembly scope that
`drift_rules.py` reads, so tier 0d plans the sweep for every file of its project. A guard drift test that holds every
reflection sweep to that scope. A pass over the 20+ existing `GetTypes()` tests. This landing changes only the register.
