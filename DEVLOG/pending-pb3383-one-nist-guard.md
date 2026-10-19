## PB3383 + PB925: one NIST guard, one program runner, a derived population

**The defect (kb/Work PB3383).** `scripts/guard.sh`, the serial NIST guard, ran every program in ONE shared directory in
the order of a hand-written `NIST_TESTS` block, and never cleaned between programs. A program therefore met every file
the programs before it had left. PB3334 had fixed exactly this in the group runner (`guard-run-group.sh`: every program
in its own clean directory after its declared predecessors), and the serial arm was the sibling it left. Re-probed on
this branch's build (Windows, Git Bash): `GUARD_TESTS="IX109A IX114A IX115A" bash scripts/guard.sh` printed
`IX115A: DIFF — REGRESSION! (… tf024.txt differs)` and the audit `unexpected=1`, because IX115A read the `tf024.txt`
IX114A's writable OPEN had fenced. The battery's equivalence proof (`guard-verify.sh` full mode) diffed that serial
guard against `guard-fast.sh`, so it would go red for the serial guard's directory, not for any difference in what a
verdict means.

**The fix: the serial arm is retired** (the note's second option; CLAUDE.md rule 5, the shape that makes the next chain
automatic). `scripts/guard.sh` and `guard-verify.sh`'s equivalence proof are deleted. `scripts/guard-fast.sh` is THE NIST
guard, and `guard-run-group.sh` its one runner of NIST programs.
- **The population is derived (kb/Work PB925, landed with this).** `guard_nist_population` (scripts/guard-population.sh)
  returns the rows the golden suite asserts (`green`, `divergent`) plus their declared chain predecessors at any depth:
  exactly the 376 names `NIST_TESTS` listed. `guard_with_predecessors` adds a `GUARD_TESTS` subset's predecessors to the
  compile list, so a consumer run alone has its input (IX114A IX115A alone: both MATCH, audit CLEAN). Both walk the
  chain-preds column through one awk (`guard_closure`), and a predecessor with no row is loud.
- **Every program is its own job.** With a clean directory per program, the chain graph's connected components (332
  groups, each serial) carried nothing but a schedule: `guard-fast.sh` now runs each member as its own job, and the
  serial re-observation re-runs a lost program plus every member that declares it as a predecessor.
- **The guard's self-test is discovered.** `guard-verify.sh --self-test` (was `--witnesses`) now runs in every gate and
  CI's audits through `scripts/self_tests.py`. Three new run-directory witnesses over a fake `corpus.tsv`: a consumer is
  not corrupted by a non-predecessor that wrote its file, and a consumer run alone still gets its declared predecessor's
  file (both RED against the pre-PB3334 shared-directory runner); a discriminator with no declared predecessor is a
  REGRESSION. The older chain-member witness now has a declared predecessor too (it had passed vacuously since PB3334).
  A structural witness fails on any other guard script that runs a NIST program. `battery.sh` phase 2a is folded into
  phase 2, whose `guard-fast.sh` runs these self-tests before it measures.
- **Drift tests.** `CorpusManifestTests`: `GuardNistPopulation_IsDerivedFromTheManifest`, `_IsDrawnFromTheManifest`
  (every member, predecessors included, has a row and a source) and `_SurplusIsOnlyPendingPredecessors` replace the
  three containment facts that parsed `NIST_TESTS`; `GuardScripts_CarryNoHandMaintainedNistNameList` now scans
  multi-line assignments (red on a planted copy of the retired block, 372 names). Shown red on that mutant: two facts.

**Docs.** DESIGN-test-build-ci.md §1.2, §1.3, §3.2, §3.10's guard rows, the plan's §9 commands, DOC_INDEX, the gate
skill, CI's guard-job comment, kb/Compiler/Build System, the glossary and FAQ describe the one guard. Every guard-family
script comment that named the serial guard is current; `guard-nist-audit.sh`'s stale "15 pending programs carry a valid
golden" count is corrected (the manifest has none today).

**Rows.** None: test tooling, no ISO rule; PB3383 and PB925 close with `closes_rows: []` and a reason. PB3385 (a
predecessor's exit status) is its own group in this wave.
