## Pipeline step 5: the population replaces the plan (PB4328), and a CI cache saves only what its producer finished (PB4348)

**PB4328 (step 5 of PB4323): FIRST the acceptance measurement.** Before anything was deleted, on a quiet box (gate slots 0
held at every start and end), one worktree's build, the compile cache off so every run is cold as after a compiler
change, BelowNormal, the three whole populations (Conformance 20,468, Unit 39,237, Characterization 37 cases, every run
GREEN):

| run | xunit threads per assembly | Conformance s | Unit s | wall s |
|---|---|---|---|---|
| one gate | 32 (the default) | 246.1 | 138.1 | 247.1 |
| one gate | 16 | 243.2 | 139.1 | 244.2 |
| one gate | 8 | 279.1 | 147.1 | 280.1 |
| three gates started together | 32 | 501.2-502.2 | 277.1-277.2 | 503.2 |
| three gates started together | 16 | 871.4-879.3 | 247.1-275.1 | 880.5 |

The settings fixed on those numbers: `maxParallelThreads` stays xunit's default, the processor count (16 ties it alone
and costs 75 % under three gates, since Conformance's cases wait on child processes; a checked-in `xunit.runner.json`
would also bind CI's 4-vCPU runners), and the gate cap stays 3 (three gates together finish in 503 s where one after
another take 741 s; the review's bound was 14.6 min, the measured 8.4).

**THEN the change.** `scripts/run_gate_legs.py` became `scripts/gate.py`, every caller re-pointed. A change a test can
read runs the WHOLE Unit, Conformance and Characterization populations, one host each, three at once, the listings
beside them, each population checked by name; the inert lane still runs the audits alone. The order is the one lever
left: the classes red at the previous run, the hand-run Linux gate's newest run or the bouncing merge-group run go first
(`WISEOWL_COBOL_GATE_FIRST`, the new `GateOrderer` in tests/_shared/GateLegs.cs), then every collection longest first by
the committed weights file. Record schema 5; the reuse rule answers only from a schema-5 record, never from an older
gate's `LEG 1 ONLY` one. Deleted: `gate_plan.py`, the impact map (`impacted_tests.py`, `impact_store.py`,
`record_impact_map.py`, `impact_lane.py`, `tools/impact/`, `cs_members.py`, `adopt_main.py`'s feed), the shared timings
store, the leg handshake, the identity records, `GateNameKey`, and the implementer scope and the light pool of
`gate_slot.py` (step 4's lead), with their C# tests (`ImpactedTestsDriftTests`, `ImpactMapLaneDriftTests`, the name-key,
leg and handshake arms of `GateLegDriftTests`); about 8,000 lines.

**Estimates corrected.** `scripts/test_population.py` stays: since step 6 it is CI's `ci-gate` population check, and the
gate's. It keeps the one trx reader and gains `red_classes`, the trx writer table and both trx audits; its XML range is
stated once, as escapes (the step's lead). `gate.py` is about 2,400 lines, not 450: the detached run, `--await`, the reuse
rule and its self-test stay, because enqueue.py and the rolling wave read them. The superseded notes: PB2696 and PB4228
retired, PB2515 and PB4269 marked superseded. CLAUDE.md "Testing" is left for step 11 (owner decision 3).

**PB4348 (the package-cache job saved a partial NuGet set under main's exact key).** Re-probed by restoring the old
workflow under the new audit: `CiBoundsDriftTests.NoCacheSavesWhatAFailedStepLeftBehind` fired on both saves of
`package-cache` (the NuGet packages after `dotnet restore`, the GnuCOBOL tarball after its fetch; both producers carry
`continue-on-error: true` and no `id`). Fix: the producers gained the ids `restore` and `fetch`, and each save runs only
`if: steps.<id>.outcome == 'success'`. The audit names the producer (the nearest earlier step that is not a cache step)
rather than every earlier `continue-on-error` step, which would have tied the tarball's save to `dotnet restore`. Sibling
sweep `git grep -n 'actions/cache/save@' -- .github`: three saves; the third, `setup-toolchain`'s, follows an `install`
step that does not continue on error. DESIGN-test-build-ci.md §1.4 states the rule.
