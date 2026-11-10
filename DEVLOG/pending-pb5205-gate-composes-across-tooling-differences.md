## PB5205: the populations are a gate step keyed by a digest of their inputs, so a docs, kb or tooling-only difference carries their passes

Worktree records-85's gate run 000001 passed every case (Conformance 23,687, Unit 40,741, Characterization 37) and was RED
only because the orchestrate self-tests hung (PB5155). The branch then merged main: 61 `kb/` files, 2 `DEVLOG/` files and three
orchestrate test scripts that no compiler test or build reads. The re-gate built and ran all 64,465 cases again. The owner:
"We better not be running all those tests again, when only the self tests failed to run", and then: "This is precisely what
the Unix make and Windows MSBuild utilities are for. They rerun only what is required due to changes."

**What was wrong.** Two things. First, PB5099's composition carried passes only when the trees were equal or every differing
path was inert (`Host.code_state_diff`). A tooling-lane script cannot change a Unit, Conformance or Characterization result
either, but it made the predicate refuse. Second, run 000001 could not be carried even at an equal tree. A red audit calls
`stop_hosts`, which sets the stop flag even when every host has already exited, and the gate read that flag as "a host was
killed". So it recorded no populations and no `tree_end`, although all three hosts had finished green.

**The fix.** The populations are now keyed the way a script self-test is (PB2914). `population_digest` hashes the toolchain
and every `git ls-tree -r` entry of the measured tree whose path a compiler test or the build may read.
`inert_lane.Classification.is_population_input`, the one definition `classify` also uses, decides which paths those are. The
key takes about 0.7 s over main's 35,590 entries. Each full-lane record keeps its `population_key` and `audit_reds`.
`Gate.carry` matches on the key alone, and `code_state_diff` and the separate toolchain check are gone. A run RED only for its
audits, with every host green and every population exact, counts as having measured its populations GREEN. A stop now counts
as partial only when it killed a host (`_Live.stopped_any`). Audits are never carried: they run at every gate's own tree, and
the self-tests re-run where their recorded inputs changed. A script change that owes the post-build self-tests still builds,
but starts no host. The COMPOSED line names the run whose population passes it carried and lists what ran now. Passes are
still found only inside the worktree. The cross-worktree store is written into kb/Work PB5205 as remaining work.

**Proof.** `python scripts/gate.py --self-test`: `=== gate SELF-TEST: PASS (97 arms) ===`. Six arms are new: a late audit
red keeps the measured populations; a hang-only RED plus a tooling-only difference carries with no build and no host; a hang
plus a test-read path runs everything; a GREEN run plus a tooling-only difference still runs the self-tests; a run without
post-build self-tests carries into a building gate with no host; and the real key over a scratch repository. The same
self-test run against main's driver (a copy outside the tree) ends `=== gate SELF-TEST: FAIL (97 arms; …) ===`, with ten arms
red. `python scripts/inert_lane.py --self-test`: `=== INERT LANE SELF-TEST: PASS ===`. `python scripts/self_test_arms.py
--self-test`: `=== SELF-TEST ARMS SELF-TEST: PASS (17 scripts) ===`. `python scripts/inert_lane.py check`: `=== INERT LANE
CHECK: GREEN — 9032 tracked inert path(s) under 4 INERT entr(ies), 4 READ ===`. `python scripts/self_tests.py --reuse`,
which runs every self-test whose recorded inputs this change touched: `=== SELF-TESTS: GREEN — 42 ran, 129 reused (inputs
unchanged since a recorded PASS) in 1919s … (1 deferred to --built: scripts/filter_population.py) ===`.
