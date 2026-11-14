## 2026-11-14: the `Gate` class split as built (kb/Work PB5411)

**Built.** `Gate` (969 lines, 43 methods) keeps the run (`run`, `_gate`, `_tested`, `_finish`, `_verdict`, the audits and lanes, the work beside,
`new_run`, `previous_run`, `_prune`) and holds three collaborators built in `Gate.__init__`: `passes` (`gate_reuse.PassCarry`: `reusable`, `facets`,
`carry`, `record_passes`, `rederive`, `write_rederived`, `Rederived`, `Carry`), `generations` (`gate_generated.Generations`: `generate`, `handed`,
`check_generated`, `all_up_to_date`, `detail`, `outputs`, `GenerationFailed`) and `legs` (`gate_legs.Legs`: `plan`, `pool_listings`, `listings`,
`first`, `run_hosts`, `check_populations`, `assembly_walls`, `concurrency`). The mover is an AST walk over `self.<attr>` of every moved method, the
design's seven edges decided in section 16.5 (and the four things the table did not say, now in that section): the rule that a collaborator
returns what it found and never calls back into `Gate` holds by the layering line `gate_audits` < (`gate_verdict`, `gate_host`) < `gate_runs` <
(`gate_reuse`, `gate_generated`) < `gate_legs` < `gate`, which `script_layering.py --check` GREEN refuses to let any module break.

**Oracle.** The case files under `scripts/selftests/gate/` GREEN (the one world that plants the class reaches `PassCarry` and `Legs` directly);
`gate.py --help`, `--await` and `--audit-shape` unchanged; a gate run's record has the same fields (`Gate._record` is untouched but for reading
`Legs.assembly_walls` and `Legs.concurrency`).

**Sites the diff changed beyond the note's list:** `scripts/gate_verdict.py` (`Outcome`, `HostRun`, `Ending` moved in), `scripts/gate_host.py`
(`HostRun` out), `scripts/script_layering.py`, `scripts/selftests/gate/` (the cases that plant `Gate.rederive`, `Gate._concurrency`),
`scripts/buildgraph.py` and `scripts/test_population.py` and `tests/_shared/GateLegs.cs` (comments naming the moved methods).
