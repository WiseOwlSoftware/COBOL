## A gate outlives its caller, the wave awaits a gate its agent hands over, and a measured tree is gated once (PB3536)

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
  `--await <run>` is the one wait (the verdict file's line and exit code, `DIED`, `PENDING`). Every record now carries
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

Rows: none (process-only, `closes_rows: []` with its reason).
