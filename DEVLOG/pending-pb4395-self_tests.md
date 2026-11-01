## PB4395 item 1, family SELF_TESTS: the self-test runner's, the fixture's, the layering's, the streams', the review lane's and the filtered-run checks leave the compiler's test assembly

Wave 20261810 group A, one of four implementers splitting PB4395 item 1 by script family (owner 2026-11-01). Six C#
classes in the Unit assembly read the pipeline's scripts, workflows and units as text or ran them, so every change to
one of those files was a compiler change that only a build could check. Each class's arms are now arms of the
`--self-test` of the script they check, and each class is deleted (no forwarder).

**Re-probe.** On the base (origin/main 1d089b67e merged with 38cad1b62) the six classes held 17 facts; `standard_streams.py
--check` was RED on two files the predecessor checkpoint added (`scripts/ci_workflow.py` and `scripts/self_test_arms.py`
called no `ensure_utf8()` first in `__main__`), which `ScriptStandardStreamsDriftTests` would have shown at the gate and
`standard_streams.py --self-test`'s rule-2 arm already did. Both now call it.

**Where each arm went.**
- `SelfTestDiscoveryDriftTests` (7 facts): the one-list arms to `live_tools.py --self-test` (`copies`: every reader lists
  the trees through `tracked`, no live script spells the listing itself; planted and real tree); the broad text net and
  the entry points to `self_tests.py --self-test` (`net_arms`, `entry_point_arms`: gate.py's declarations read from its
  syntax tree with `ast.literal_eval`, never imported; the Linux gate's legs; CI's jobs through `ci_workflow`; no hand-run
  self-test, planted and real); the runner's declared arms to `self_test_arms.py`'s `DECLARED`; CI's audits job against
  the gate's AUDITS to `gate.py --self-test` (`ci_audits_not_in_gate`, `CI_AUDITS_ELSEWHERE`); the arm reader's own
  witness stays C#, as `SelfTestArmSourceTests` (the census and derived-verdict tests still use `SelfTestArmSource`).
- `SelfTestFixtureDriftTests` (4): the declared arms to `DECLARED`; the audit on the tree was already the fixture's own
  `the tree` arm; the hand-made-repository net and the rules-written-once net to `scratch_repo.py --self-test`
  (`rule_copies`, planted and real).
- `ScriptLayeringDriftTests` (1): `script_layering.py --self-test` already ran `--check`; it now holds the six-module floor.
- `ScriptStandardStreamsDriftTests` (1): `standard_streams.py --self-test` now runs all three rules over the tree, the
  cp1252 probes included (it ran rules 1 and 2 only), and holds the floors.
- `FilterPopulationGuardDriftTests` (2): to `test_population.py --self-test` (`audit_filtered_runs`, `shard_union_checks`);
  `filter_population.py`'s own self-test needs a build, so it could not host them. The planted cases spell `--filter`
  and the trx logger mark in parts so that the file is not a site of its own rule.
- `ReviewLaneDriftTests` (2): the natural home, `review_lane.py`, is outside this slice's file set, so the facts went to
  the nearest owned one, `self_test_arms.py`, as `WIRING` (patterns a PowerShell function, a Markdown step or a file must
  and must not hold).

**Kept in C#.** `GateLegDriftTests` (Unit, Conformance, Characterization) and `tests/_shared/GateLegAudit.cs` check the
test assemblies' own code (the orderer, the shards, the weights, the framework links, the discovered names, Server GC):
they read no tooling path, and the note keeps "what is not a script's own arm". `HeldAnalyzerOutputDriftTests` reads only
the `src/` and `tests/` project files: moved into `test_held_analyzer_output.py` (Windows-only, run only for a tooling
change) it would stop gating the code change that adds a Roslyn component.

**Counts.** Before: 17 facts in six deleted classes (plus 1 kept in `HeldAnalyzerOutputDriftTests` and 22 cases in the
three `GateLegDriftTests`). After: 22 new self-test arms (live_tools 2, self_tests 8, gate 3, scratch_repo 3,
test_population 4, self_test_arms 2), two extended (script_layering, standard_streams), 51 declared labels in `DECLARED`,
and 1 C# fact in `SelfTestArmSourceTests`.

**Docs.** DOC_INDEX, REARCHITECTURE_PLAN, DESIGN-orchestrator-loop and DESIGN-test-build-ci name the new homes at every
line that named a deleted class; the C# comments that named one (`SelfTestArmSource`, `LandingMeasureDriftTests`,
`TrackedTree`) too. `scripts/inert_lane.py` is the
integrator's: its CODE_READS readers lose these classes (see the report).

**Gate.** Run 000001-20261101T211212Z at c73dab4e4 ended `NOT RUN [verdict]`: its process died at the fleet stop
(STOP-w20261810, "integrate now; one gate on the merged head"). Before it died, Unit was `Failed: 3` of 39,218 and
Characterization was `Passed 37/37`. The three Unit reds are fixed in a04a5a5e4. Two were SparseCheckoutDriftTests: the
ci-gate job's sparse checkout now lists `scripts/ci_workflow.py`, which test_population.py imports. The third was
LandingLaneDriftTests: it now expects land_lane's classify_red label as f5b9e021e rewrote it. The integrator's gate on the
merged head is this branch's verdict.
