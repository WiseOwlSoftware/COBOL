**What the first landing (branch `build-stage6-ci`) did, and what remains of PB5235 (2026-11-10).** This note stays
`open`.

**Landed in the first change** (the transport addendum states the decisions):
- the transport (`buildgraph.publish`, `fetch_published`, `Published`, `is_pass`, `STORE_REF`);
- the merge group's plan (`scripts/ci_plan.py`, the `changes` job's plan step), with the shard jobs, the configurations
  and ci-gate's population check driven by it and held to it (`ci_plan.ci_wiring`);
- the BUILD TARGETS audit in the merge group's audits job;
- the nightly's red as the session probe's first line (`scripts/session-probe.ps1` step 0);
- the landing's publish: `enqueue.py`'s `queue` step 4 publishes after the branch push, and a failure is exit 16, STORE
  NOT PUBLISHED, retried once by the landing lane (review round 1).

**Remaining:**
1. **The dynamic input proof per target** (item 3 of the note). `inert_lane.py watch` gains `--target <name>`, and the
   Linux steps pass their target, so a test that opens a tracked path outside that target's declared inputs fails the
   shard, naming the path. Three facts this lane measured shape it:
   - inotify gives no process or test identity, so "naming the test" means the tests whose trx start-to-end window
     contains the open's time (the shard's xUnit runs classes in parallel, so it is a short list, not one name);
   - `inert_lane.wiring` requires the literal `scripts/inert_lane.py watch -- dotnet test` in every Linux step, so it must
     change in the same change;
   - for Unit the set outside its inputs is exactly what the watch already watches (`@population-inputs` minus inert and
     tooling), so the new judgment bites only on Conformance and Characterization, whose watched set grows to every
     tracked directory outside their inputs.

   The witness: a self-test of the checker over recorded access logs (events plus a trx), and a deliberate undeclared read
   on a scratch branch in a merge group. Not built in this landing. The merge group is the only place it can be proved,
   and a first run that finds a genuine undeclared read in Conformance or Characterization would block this landing on
   `scripts/build_targets.json`, which PB5231 owns now.
2. **The nightly as graph targets.** It already runs the slow work (the NIST guard `scripts/guard-fast.sh`, the GnuCOBOL
   differential, every self-test on both OSes), and its red is now the probe's first line. What is left is declaring each
   job as a target with a key, so a nightly whose inputs did not change since its last green records "up to date" instead
   of re-running. That needs PB5231's graph (targets with outputs and kinds) on main. PB5234 is measuring the guard
   scripts. The first probe run with this change showed the scheduled nightly of 2026-11-10 red in `latest` and in
   `Guard (WiseOwl COBOL NIST, parallel)` (run 42860414795), a finding for PB5234's lane and the toolchain bump.
3. **CI records its own passes.** The Linux leg accepts no record today. Once CI writes its Linux Release passes to the
   ref (a merge-group job with `contents: write` on that ref alone), a re-queued pull request whose Linux targets already
   passed at their keys would skip them. That needs a decision on the token's scope.
4. **The measured after.** The DEVLOG entry of the first landing states the before (13.0 minutes of wall time and 114.2
   job-minutes per matrix merge group, 48% of them Windows shards re-running locally passed tests) and the replayed plan.
   The first ten merge groups after the landing give the measured after.
