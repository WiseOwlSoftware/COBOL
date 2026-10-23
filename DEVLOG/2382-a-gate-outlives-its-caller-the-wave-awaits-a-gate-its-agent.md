## Entry 2382 — 2026-10-22 19:08 PDT — A gate outlives its caller, the wave awaits a gate its agent hands over, and a measured tree is gated once (PB3536)

Wave 1150's group A review finisher reached its turn cap while its gate ran, returned SPLIT, and the wave's only path
was a second finisher whose whole job was to re-run the same gate at the same head. The owner, ~16:50 PDT: "Reruning
these simply wastes tokens: but the workflow returned before its gate finished, so it needs the gate re-run at its
head and a round-2 review. - we should have the failure in the first place."

**What the re-probe showed.** The note said the wave neither awaited nor reused the gate's verdict. The evidence said
more: the finisher's run directory (`20261022T225440Z-2797b8`) is empty and its log stops in the audits, so the
agent's return KILLED the gate. The agent's shell is in no job object, so the harness kills by walking the process
tree, and a calibration confirmed that a child its caller starts directly dies with that walk whatever its creation
flags. A gate the wave can await has to survive the return first, so the fix grew by one mechanism (rule 5).

**Mechanisms.**
- `scripts/detached.py`: THE one launch that outlives its caller. A launcher starts the target and exits at once, so
  no tree walk from the caller reaches it. The landing lane's `spawn` moved there, because its runners were exposed to
  the same kill.
- `scripts/run_gate_legs.py`: `build-local` starts the gate DETACHED in its run directory (`gate.log`, `gate.pid`,
  `verdict.json`) and follows it to the verdict, so every caller blocking on the verdict line sees what it saw before.
  `--await <run>` is the one wait (the verdict file's line and exit code, `NOT RUN [verdict]` for a process that died
  with none, `PENDING`). Every record now carries
  the head, the working tree measured at the start and the end (through a private index), the base and the exit code,
  and a re-gate of an unchanged tree answers from the recorded GREEN, saying `REUSED`. A RED is never reused, and the
  landing's merged head is another tree.
- `wf_rolling_wave.js`: an implementer or finisher at its turn cap returns `GATE-PENDING` with the run directory. The
  wave awaits the verdict file inside the same ceiling, one `cobol-locator` window at a time: GREEN continues as DONE
  (the finished test, the review), RED goes to one gate finisher, and no verdict is STALLED.
- `plan_wave.Report`: a `GATE-PENDING` report is finished exactly when its run is GREEN for its HEAD and that commit's
  tree, so the survey needs no awaiter. The planner waits on a running handed gate and plans a finisher for one that
  ended otherwise.

**Docs.** MANDATORY-PRACTICES P2 (its one exception) and I1, the dispatch spec's HOW TO WAIT, the finisher and fix-lane
briefs, the report template, the gate skill, DESIGN-test-build-ci.md §3.14.12 (new) and DESIGN-orchestrator-loop.md 4.9.

**Tests.** `run_gate_legs.py --self-test` gains 10 arms (declared in `GateLegDriftTests` Arm3), among them a real
detached gate that survives a tree kill of its follower and is read back through `--await`. `detached.py` has 3 arms,
`test_rolling_wave.py` gains 4 (GREEN → review, RED → gate finisher, no verdict → STALLED, a review finisher's handoff)
and `test_plan_wave.py` gains 3. The wave and planner arms read real verdict files through the real reader.

**Measured.** By the same pattern, today's workflow journals, and every journal on disk, hold one return of this
shape. That is one finisher dispatch saved, and the wave would have gone straight to round 2.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … Conformance 10,491/18,125 · Unit
33,958/35,379 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) … (run 20261023T002325Z-aa7b26) ===`,
the first gate the new driver ran DETACHED. Re-gated at once on the same tree, it answered in about three seconds:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — REUSED (kb/Work PB3536): tree 42951658c89c at head
222e9d07403c was gated in run 20261023T002325Z-aa7b26 and has not changed since, so nothing ran …`.

**A red that passed alone.** The re-gate after the docs commit went RED in its audits: `test_dispatch_guard.py`'s
"unchecked rendered spec" case expected a BLOCK and got a PASS, and it was green when run alone. The host was
loaded (one audit took 348 s), so the hook's 60 s wait for `check_practices.py` expired, and the hook then PASSED
the dispatch it exists to refuse. `readonly_repo.py` failed open the same way when `git rev-parse` outran its 15 s.
Both hooks now REFUSE on an expired wait, which wedges nothing because the call is re-issued. Each wait is a hang
guard their self-tests lift, so the check's own verdict decides every case, and each self-test plants a 1 us wait
to see the refusal.

**Review round 1 (wave 1152), two blocking findings, both of which would have sent a GREEN handed gate to a finisher.**
R1-1: an agent returns its run as `TestResults/build-local/<run>`, relative to its worktree, but the wave's await and
the planner read it from the main checkout, so `--await` said `NOT RUN — no gate run directory` for a GREEN run.
`run_gate_legs.resolve_run` is now the one resolution: the wave passes `--worktree <the agent's worktree>`, and
`Report.gate_state` resolves against the report's Worktree field (that reader now takes either path separator and a
POSIX path, so its test runs on both hosts). R1-2: every harness worktree carries an edited, TRACKED
`.claude/settings.local.json`, and the measured tree included it, so a GREEN at a clean commit never equalled the
commit's tree and `gate_state` read ENDED. `Host.measured_tree` now leaves out `coord.NEVER_WORK` (the one list of
paths that are never work), at HEAD's content whether edited on disk or staged. A first try fed `git update-index
--index-info` through a text pipe, and on Windows the CRLF made git answer `Ignoring path …\r`: the call is binary
and NUL-separated now. New arms: the NEVER_WORK exclusion and the run-name resolution in `run_gate_legs.py`, a relative
run and an end-to-end harness worktree (a real `measured_tree` record, then `gate_state` GREEN) in `test_plan_wave.py`,
and in `test_rolling_wave.py` a relative run awaited from another directory plus a check that the wave's PENDING exit
equals the driver's. The first exclusion form passed `:!STATUS.md` to `git add`, and git refuses any add pathspec that
names an ignored path, so on a real worktree the measured tree was None (nothing reusable, every handed gate ENDED):
the probe on this worktree caught it, and the arm now ignores STATUS.md as this repository does. Run three times, the
driver's self-test went red twice in the scope-reuse arm: run names carried the start to the second, so two gates of
one second sorted by their random nonce and "the newest earlier run" was sometimes the older one. The first fix carried
the stamp to the microsecond, and the merge queue bounced it (PR #415, merge-group run 39888287569): on the Windows
runner three runs started in the same microsecond, and the new arm sorted them by nonce again. The local gate never saw
it because this host's clock advanced between starts. A clock orders nothing finer than its resolution, so a run is now
named `<n>-<UTC stamp>`, where n is one past every run in the run root, taken under the run root's lock. The reuse rule,
the plan's newest earlier run and the pruning all read that number. The arm stops the clock for twenty runs, then turns
it back for three more, so a clock-ordered name is red on every host; a second arm races eight threads for numbers. A
run named before the numbers is older than every numbered one. The sweep found one sibling. The landing lane named an
attempt by its UTC microsecond and the branch, so a launch and its retry in one clock tick took ONE file name, and the
retry's record overwrote the first. An attempt now takes a number the same way, under the tick lock, and an arm with a
stopped clock proves the two records. plan_wave's reports sort by wave, then mtime, then name (kb/Work PB3528), a total
order; landing_measure sums its runs and orders none.

**Merged with main's PB3527.** Main had made a held gate lock `BUSY` with exit 3 and every NOT RUN carries a classified
cause. `--await`'s PENDING moved to exit 4, the lock refusal is BUSY written to the detached run's verdict file, a
dead gate is `NOT RUN [verdict]`, and two ENVIRONMENT causes are new: `launch` (the host would not start the detached
gate) and `run` (no run directory). A record from before schema 3 takes its exit code through `read_verdict`.

**The merged head's gate was RED on the measured-tree arm, and the cause is racy git.** The arm passed alone and failed
inside the loaded gate: an edit measured as the unedited tree. `measured_tree` copied the index with `shutil.copyfile`,
which gives the copy a fresh mtime. The index file's own mtime is git's racy-git reference: git re-reads by content an
entry whose cached mtime is not older than the index file's. Git for Windows compares whole seconds here, so an edit
in the cached entry's second keeps the entry's size and mtime. With the reference moved forward, `add -A` kept the old
blob, and 13 of 200 such edits under eight parallel loops went unseen. A gate that reuses a GREEN on that tree would
have skipped a changed tree. The copy is now `shutil.copy2`, which keeps the mtime (0 of 200). A new arm pins one past
second on the entry, the index and the edit, so it fails every time `copyfile` comes back, whatever the load.

Rows: none (process-only, `closes_rows: []` with its reason).

