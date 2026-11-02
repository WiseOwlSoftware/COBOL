---
name: gate
description: Use before every commit and before every merge to choose and run the correct test gate - the ONE gate (build-local - the whole Unit, Conformance and Characterization populations inside a gate slot, previous reds first, the same for an implementer's commit and a re-gate after a fix, nothing run on Linux; a landing reads its record and gates nothing; CI's merge group is the first run of the merged tree, Linux and Release), and the comprehensive battery per accumulated batch - and to read the verdict without producing a false green.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:test-gate` (Skill tool) before reading on. If the plugin is not loaded
> (a cloud session receives no project marketplace), Read `tools/claude-skills/skills/test-gate/SKILL.md` instead
> (`git submodule update --init tools/claude-skills` if the path is missing). THEN apply this overlay: it carries only
> what is specific to WiseOwl COBOL — commands, paths, CI and the merge queue, the owner's dated decisions — and wins on
> conflict. Pinned: **brent-tools 1.22.0** (`tools/claude-skills`, kb/Work/PB1699).

# Gate

**Self-check first: is this a change's gate, or the batch's pre-merge?** A change's gate has ONE shape (owner
2026-10-11, kb/Work PB3015 Q1a, PB3022): an implementer's commit and a re-gate after a fix run the same command
below, and the landing runs none (kb/Work PB4331). No gate FILTERS: every gate runs the whole population, ORDERED so a likely red comes first (owner,
2026-09-28, kb/Work PB1708: "order, don't skip"; owner decision 1 of PB4323, 2026-10-31, kb/Work PB4328: the population
is the selection); CI's merge group is the first run of the merged tree, Linux, Release and GitHub's runners (THE RULE,
below). What the owner corrected
repeatedly was over-gating per fix with the whole-corpus suites (the battery, the NIST guard); the gate is not that —
its populations take about four minutes on a quiet box, and a gate slot caps how many run at once.

## Always first

```
dotnet build WiseOwl.Cobol.sln -c Debug
```

Build the **solution**, not one project. Building only the compiler project and then running `dotnet test
--no-build` tests whatever compiler was copied into the test bin at its LAST full build — a stale compiler. Local
goes green while CI fails.

## Per change — the one gate (every implementer, every re-gate)

```
pwsh scripts/build-local.ps1 -Priority BelowNormal *> <log>
```

The driver, `scripts/gate.py` (DESIGN-test-build-ci.md §3.13, §3.14; kb/Work PB4328), holds the worktree's GATE LOCK (a
second gate in the same worktree is refused), decides the change's LANE (`python scripts/inert_lane.py classify --base
<rev>`: a change no test reads is the INERT LANE, which runs the audits alone, kb/Work PB2878; a change whose every other path
is tooling in `scripts/`, `.github/`, `tools/` or `.claude/` that no compiler test or the build reads — not under
`inert_lane.py`'s `CODE_READS` — is the TOOLING LANE, which runs the audits with the script self-tests and no slot, build or
test host, its verdict `TOOLING LANE (kb/Work PB4395)`), starts the audits BESIDE
everything after them (outside the slot; FAIL-FAST: a red audit known before the slot or the hosts ends the gate in
seconds with `NO TEST RAN`, and one found while the hosts run STOPS them, kb/Work PB2523, PB4326 — run `python
scripts/rendered_views.py --check` and the citation audits yourself before gating to avoid the round trip; one audit,
`SELF-TESTS`, is every script self-test, discovered and run in parallel by `scripts/self_tests.py`, kb/Work PB2563, and
runs ONLY WHEN YOUR CHANGE TOUCHES A SCRIPT (a path under `scripts/`, `.github/`, `tools/`, `.claude/`, or a
`.py`/`.ps1`/`.sh` file: the `tooling=` line of `inert_lane.py classify`; a compiler data file a self-test reads, its
`TOOLING_READS` list, starts none, owner 2026-11-01, kb/Work PB4409; CI's merge group runs every Linux self-test
whatever the change, and the Windows ones whenever the `reads_tooling=` line is true: a script or that data) — every gate
passes `--reuse`, so a self-test whose inputs are unchanged since a recorded PASS is REUSED, not run; one still running an
hour after taking its budget place is `SELF-TESTS HANG`, never RED, kb/Work PB2914, DESIGN-test-build-ci.md §3.14.10) and
fetches the per-worktree GnuCOBOL corpus when absent, then takes a GATE SLOT (`scripts/gate_slot.py`: at most N gates
build or test at once, repository-wide — `gate-slot: waiting, k ahead` is the cap working), builds the solution and runs
the WHOLE Unit, Conformance and Characterization populations, three `dotnet test --no-build` hosts at once (Server GC,
kb/Work PB2526), each population then checked by name against its `--list-tests` (`scripts/test_population.py check`).

- ⭐ **THE POPULATION IS THE SELECTION** (owner decision 1 of PB4323, 2026-10-31; kb/Work PB4328). There is no plan, no
  tier, no impact map and no leg 2: the gate runs every case and is done on `=== BUILD-LOCAL GATE: GREEN — Conformance
  n/n · Unit n/n · Characterization n/n cases ran … ===` (or `RED`). Ordering changes WHEN a case runs, never WHETHER:
  the classes red at the worktree's previous gate, its newest hand-run Linux gate or the merge-group run that bounced its
  pull request (fetched into `TestResults/ci-bounce/`, kb/Work PB3644) go first (`WISEOWL_COBOL_GATE_FIRST`, the test
  framework's `GateOrderer`), then every collection longest first by the committed weights (`tests/_shared/test-weights.json`,
  `scripts/shard_weights.py generate`). Measured for this step (2026-11-01, quiet box, compile cache off): one gate's
  three populations 247 s; three gates started together 503 s each; xunit's default thread count (the processor
  count) beat 16 and 8 in both, so no `xunit.runner.json` caps it. A re-gate of an unchanged tree answers from its
  GREEN record (`REUSED`, kb/Work PB3536). The landing (`python scripts/orchestrator/enqueue.py land <branch>`, kb/Work
  PB3023, MANDATORY-PRACTICES L1, L2) reads this record; CI's merge group is the first run of the merged tree, of Linux
  and of Release (PB4323 §5.1).
- **The cap is ONE shared setting, 3 by default** (owner 2026-10-10 10:20 PDT, kb/Work PB2514: "Cap 3 permanently"):
  `python scripts/gate_slot.py set-cap N [--until ISO] [--why TEXT]` departs from it in the slot directory every
  worktree shares; every gate re-reads it while it waits, so a change reaches the queue at once, and an expired setting
  is the coded default again with no one acting. `gate_slot.py clear` returns it to its coded default at once, and
  `gate_slot.py status` prints the cap, every held slot and the queue. No environment variable sets it. **An inert-lane
  gate takes no slot at all** (kb/Work PB4327).
- **The gate runs nothing on Linux** (owner 2026-10-31, kb/Work PB4327; MANDATORY-PRACTICES I8): CI's merge group runs
  the whole Linux population and every Linux self-test, so a Linux-only red is found in the queue. `bash
  scripts/linux-gate.sh --nice` (CI's whole Linux legs, ~6 min; `--legs unit,conformance,characterization,guard` picks)
  is the tool that reproduces a Linux CI red on this box, run by hand and not a step of any change's gate; its newest
  run's red classes go first at the worktree's next gate.

## Per landing — the landing reads the record; it gates nothing (owner 2026-10-23, kb/Work PB3612, PB4331)

A branch is gated locally ONCE, at its head, by its implementer or finisher: that run is the whole definition of "tested
locally". `enqueue.py land` does not gate the merge with main and does not gate again when main moves: it READS the
worktree's newest `TestResults/build-local/*/verdict.json` runs and refuses (exit 14, UNTESTED) a tip none of them covers
GREEN (a finisher gates the tip). The merge queue's `ci-gate` runs the whole population on the exact
merge, on both operating systems, before `main` moves: it is the FIRST run of the merged tree, a true textual merge in
a shared file included (kb/Work PB4331), and the landing lane classifies every red it bounces against this record
(INFRA and RUNNER re-queued once, MERGE and GENUINE to a finisher, kb/Work PB4271). A gate of the worktree that
runs when the landing would write it is exit 12, GATE BUSY, whatever record exists (kb/Work PB4204).

⛔ **THE RULE (owner 2026-10-31, CLAUDE.md "Testing", replacing the CI invariant of 2026-10-04): every case the merge
group runs on Windows already ran green at the pushed head, on this host, under the same `toolchain.env`.** The merge
group is the FIRST run of (a) the merged tree, (b) Linux, (c) Release with a cold compile cache, (d) the Windows tooling
self-tests on a tooling change and (e) GitHub's runners; a red there is one of those five, a flake or infrastructure,
never an untested case. A red of class (b), (c) or (e) is also a finding against the pipeline's record of that class,
filed with its rate (`kb/Work/PB1957`); a flake is a kb/Work defect fixed at its root, never retried inside the run.

**Do NOT run per commit:** the battery or the NIST guard (`scripts/guard-fast.sh`).

## Per accumulated batch / pre-merge — comprehensive

- Full greenfield Conformance + full characterization
- The GnuCOBOL external differential, before AND after, diffing PER-CASE verdicts
- `scripts/guard-fast.sh` (the NIST guard: the CLI-level NIST leg through `cobol`, every program in its own directory)

- **CI's own Linux leg — comprehensive is not comprehensive without it.** Everything above runs on ONE host
  (this Windows machine, Debug). The workflow's `ubuntu-26.04` jobs — `guard-fast.sh`'s NIST loop, the Linux
  shards — and the Release build are verified by CI and by nothing else (`guard-fast.sh` is the nightly's, kb/Work
  PB4332), so a test keyed on host ACL or file-lock semantics can be green here and red there. Read the
  verdict for the commit you pushed:
  `gh run list --branch main --commit <sha> --json databaseId,status,conclusion,displayTitle`, then
  `gh run view <id> --log-failed` on a red. `scripts/session-probe.ps1` prints the latest run's conclusion at
  session start, so a red main is never inferred from silence (`kb/Work/PB796`).
- ⛔ **AND IT IS NO LONGER OPTIONAL TO READ IT: `ci-gate` IS A REQUIRED STATUS CHECK ON `main`.** `ci-gate` is the
  workflow's terminal job — `if: always()`, `needs:` every other job, PASS only when each is `success` or
  `skipped` — and it is the ONE stable check name the protection can require (every matrix job's check name
  carries its shard AND its filter text, and every one of them is skipped on a docs-only push). It is required ON THE
  MERGE GROUP by the ruleset on main (`scripts/orchestrator/ruleset-main.json`, applied on the owner's go, kb/Work
  PB3024): main takes only pull requests GitHub's merge queue has tested, administrators included, so a bare
  `git push origin HEAD:main` is refused by the server (owner decision 2026-09-06, question 23; `kb/Work/PB796`). The
  landing is therefore a SCRIPT, not a habit: **`python scripts/orchestrator/enqueue.py land <branch>`** (kb/Work
  PB3023) merges main in, stamps the DEVLOG entry and ends in one pull request into the queue, gating nothing: no CI wait; the
  merge group runs the matrix and is the CI verdict, and a pull request's own run is the cheap jobs only. It refuses
  (exit 8) before merging anything while `ruleset.py check` does not report the queue ACTIVE. Never run its
  inner `queue` verb by hand: `land` is what merges main in and stamps the DEVLOG entry. There is no other
  landing path: main refuses a direct push (kb/Work PB3024).
- ⚠ **A docs-only push runs `changes` + `ci-gate` and nothing else** — seconds, not a matrix. `paths-ignore` was
  removed because a workflow that declines to START leaves a required per-commit check unsatisfiable; the same
  path list now lives in the `changes` job, one layer down, where it decides whether the MATRIX runs.

`guard-fast.sh` is **not** CI-complete: CI builds Release, local builds Debug. Any change whose semantics differ by
build configuration gets a local `-c Release` leg before push. Better: do not write configuration-divergent
compiler behavior.

SERIAL does not mean comprehensive-per-change. The grammar batch is serial (one shared parser, so implement
constructs one after another) but still gets ONE comprehensive gate for the whole batch.

## Reading the verdict — where false greens come from

The base's "Read the verdict line", "Flakes and attribution" and "A filter that matches nothing" sections apply as
written (full output to a file, no source edits while a gate runs, no flake without the test's name and an isolated
re-run). Here, additionally:

- **Never chain ANYTHING after a verdict command** (build, test, `build-local`, `enqueue.py land`, battery) with `&&`,
  `||` or `;` — MANDATORY-PRACTICES P14, and the guard hook (`scripts/hooks/forbidden_commands.py` rule 5) BLOCKS
  it. To capture the status in the same call, append `; echo "EXIT=$?"`; read-only commands may follow that.
- `scripts/build-local.{ps1,sh}` prints ONE `=== BUILD-LOCAL GATE: ` verdict line — `GREEN`, `RED`, `BUILD
  FAILED`, `BUSY` (exit 3: another gate holds
  this worktree's gate lock; nothing ran and nothing is red: wait for it, then re-run) or `NOT RUN [<cause>]` (exit
  2: it measured nothing; the cause's class is `gate.NOT_RUN_CAUSES`' — ENVIRONMENT `[settings]`
  `[interpreter]` is no defect of yours, BRANCH `[driver]` `[population]` is code your tree carries: fix it) —
  block on that line, never on the exit code. The run directory it names holds
  every host's full log and trx (`Conformance.trx`, `Unit.trx`, `Characterization.trx`), each listing, the first-classes
  file and `verdict.json` (timings, first red, slot, per-assembly wall and effective concurrency).
- **The gate outlives its caller and is gated once per measured tree** (kb/Work PB3536; DESIGN-test-build-ci.md
  §3.14.12). `build-local` starts the gate DETACHED in its run directory (`gate.log`, `gate.pid`, `verdict.json`) and
  follows it, so killing the caller leaves the gate running to its verdict; `python scripts/gate.py --await
  <run> [--worktree W] [--wait-s N]` reads that verdict (its line and exit code; a process that died with none is
  `NOT RUN [verdict]` exit 2, a gate still running `PENDING` exit 4; a relative `<run>` is W's run). A re-gate of a
  tree an earlier gate in the worktree measured GREEN (the harness's `.claude/settings.local.json` edit, STATUS.md and
  TestResults/ are no part of a measured tree: coord.NEVER_WORK), against the same base and under a scope it covered, answers at
  once `… GREEN — REUSED (kb/Work PB3536): … that gate: …`, naming the run that measured it; a RED is never reused.
  An agent at its TURN CAP with its gate running hands it to the wave (status `GATE-PENDING`, MANDATORY-PRACTICES P2).

## Read the failure before diagnosing it

Stack trace = emitter or runtime bug. `error CS####` = generated-code problem. A COBOL#### diagnostic = front-end
reject. Timeout = infinite loop. These are different problems; check which one you have first.
