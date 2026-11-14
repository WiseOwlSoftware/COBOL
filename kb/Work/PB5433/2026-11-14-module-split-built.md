## 2026-11-14: gate.py's module split as built (kb/Work PB5433)

**Built.** `scripts/gate.py` (2,399 lines) became the driver (1,248: `Outcome`, `Rederived`, `Carry`, `Gate`, `main`, `rederive_main`) and four
modules cut by an AST mover (every top-level definition to the module the section 16.5 table names, each module's imports derived from
the names it uses): `gate_audits.py` (235), `gate_verdict.py` (176), `gate_host.py` (473), `gate_runs.py` (427). The oracle: `gate.py --help`
and `--await`, the audit-shape cases and every `scripts/selftests/gate/` case GREEN with the labels of PB5432 unchanged (123); `python
scripts/self_tests.py` GREEN over 196 self-tests; `script_layering.py --check` and `--self-test` GREEN.

**What the split found that the design table did not decide (section 16.5 now says so).** The four modules are layered `gate_audits` <
(`gate_verdict`, `gate_host`) < `gate_runs` < `gate`. `LOCK_NAME` and `Spawn` are `gate_host`'s (as the run store they would have made
`gate_host` and `gate_runs` import each other through `lock_of_run`); `RUNNING`, `STARTING`, `DIED` are `gate_runs`'; `ENVIRONMENT`, `BRANCH`,
the three Readings are `gate_verdict`'s; `CI_AUDITS_FLOOR` and `CI_AUDITS_SUBMODULE` are `gate_audits`'. `gate_host` imports
`lane_classification` and `coord.NEVER_WORK`, which `script_layering.LINES` now declares. Each module carries its own `REPO` line.

**Re-pointed in the same change (rule 4: no old spelling).** `enqueue.py`, `land_lane.py`, `progress.py`, `queue_facts.py`, `reports.py`,
`test_rolling_wave.py`, `arch/perf_baseline.py`, `gate_fake_host.py`, `abi_pin.py`, `git_merge.py`, the case files of `scripts/selftests/gate/`,
`orchestrator/land_lane/` and `orchestrator/plan_wave/`, `self_tests.py` (`entry_point_arms` reads `AUDITS`, `BUILT_SELF_TESTS`,
`TOOLING_ONLY`, `SELF_TESTS_REUSE` out of `gate_audits.py`), `test_population.py` (`TRX_WRITERS` and the `dotnet test` caller audit name
`gate_host.py`), `line_endings.py`, `selftests/inert_lane/test_wiring.py` (which reads where each wiring string now lives),
`build-local.{ps1,sh}`, `linux-gate.sh`, the CI workflow's comments, the gate skill, the workstream templates and the two design documents.

**Sites the diff changed beyond the note's list:** the files above that the note's paragraph does not name.
