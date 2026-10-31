## Entry 2883 — 2026-10-31 14:27 PDT — Leg-1 selection holes filed, and the owner amends batched gating (PB4228, PB4269, PB4270, PB4271)

PR #894 (PB107) bounced out of the merge queue twice on 2026-10-31, and PR #896 failed with it in the same merge group.
The red was `RunUnitStateDriftTests.NoStaticMutableCollection_OutsideTheDocumentedProcessStores`: the branch added a
static `ConditionalWeakTable` to the runtime without registering it. The branch's GREEN local gate lists the test as
NOT RUN. That test reflects over every field of the runtime assembly. The drift-rule index sees only file paths and
declared tree scopes, and the coverage impact map sees only executed code, so neither one puts the test in leg 1 for a
runtime change. The operator registered the field by hand (`d917370e0`), and the rest of the queue then passed. That
is kb/Work PB4228 (MAJOR): a declared assembly scope that `drift_rules.py` reads, a guard drift test, and a pass over
the 20+ existing `GetTypes()` tests.

The same morning, PR #880's 11 failed cases were in its GREEN gate's not-run list too. The gate had planned against an
impact map 111 commits behind its base, because maps are recorded only when the orchestrator loop adopts main, and
the loop had been stopped overnight. Leg 1 then kept the cheapest 2 % of 19,609 tier-1 cases. That is PB4270 (record
maps from the event that changes main) on top of PB2696.

The operator asked the owner whether a gate that cannot attribute a change should run the whole unattributed tier.
The owner chose "Run the whole tier (Recommended)". That is kb/Work PB4269, an amendment to PB2515's batched gating,
recorded in plan §0's GATE POLICY bullet and not yet implemented in `gate_plan.py`. PB4271 classifies every
merge-queue red (selection miss, local-passed, merge, infrastructure), so selection misses are counted and filed. This
landing changes only the register and §0.

