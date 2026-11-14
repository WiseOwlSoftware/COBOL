## PB3726 steps: the gate, the landing lane and the inert lane leave their self-tests and split by job (PB5396 to PB5398, PB5432 to PB5434, PB5411)

Seven steps of the pipeline restructure land together on one branch, in the order the work ran. The branch also carries the wave
20262028a and 20262027a landings it was planned onto (PB5373, PB5118) and the first implementer's PB5395 step A.

**PB5396, the public API baselines.** `scripts/public_api_baselines.py` owns the baselines' rules (moved from `landing_check.py`) and
its `--check` is the PUBLIC API BASELINES audit in the gate and CI. `PublicAPI.Unshipped.txt` was out of the analyzer's order from
line 138; `--write` ordered it once, the line multiset unchanged, and this branch merged main's 34 new symbols into that order.

**PB5397 and PB5398, the inert lane.** `inert_lane.py`'s 45 expectations became 44 cases in `scripts/selftests/inert_lane/`; the module
split into `lane_classification.py` (the lanes and the one classification, its entries in key order), `source_reads.py` (the static
reads of build files, test sources and scripts), `inert_watch.py` and the 95-line CLI. The classification over the last 20 landings is
identical before and after. Main changed `inert_lane.py` while the split sat unlanded (the C# lexer moved to `csharp_lexer.py`, the
RepoDrift test target joined the READ and CODE_READS entries): both were ported into the split modules, and `source_reads` calls the one lexer.

**PB5432, the gate's cases.** `gate.py` lost 1,430 lines: 123 expectations became 31 cases in `scripts/selftests/gate/`, and the
planted host `FakeHost` an ordinary module `scripts/gate_fake_host.py` that `arch/perf_baseline.py` also imports. The RepoDrift
population main added meanwhile was ported into the cases and the planted host.

**PB5434, the landing lane's cases.** `land_lane.py`'s self-test was one `try` body of 602 statements over one shared scope. A
reaching-definitions walk found the five groups of cases that share no state and made each a file (`test_tick`, `test_detached_runner`,
`test_stranded_branches`, `test_queue_watch`, `test_conflict_brief`), plus `test_source_shape` and `fixtures`; the file went from
3,038 lines to 1,750. The 97 labels are the same set before and after. The conversion found one accident (the conflict-brief case read a
list the real-launch case filled) and one trap (the stopped-clock case rebound the subject's `now_utc` through `globals()`, which in a
case file rebinds the case file's). The note and the design called the detached runner's world Windows-only; the old self-test carried
no platform header and ran on Linux, so its file carries none either (section 16.2 says so). `landing_rules.DECLARED` now names land_lane's
arms as case functions.

**PB5433, the gate's module split.** `gate.py` (2,399 lines) became the driver plus `gate_audits.py`, `gate_verdict.py`, `gate_host.py` and
`gate_runs.py`, cut by an AST mover from the design's table, with every importer re-pointed and no old spelling left. The split decided
what the table did not: `LOCK_NAME` and `Spawn` are `gate_host`'s (as the run store's they made `gate_host` and `gate_runs` import each
other), and the layering is `gate_audits` < (`gate_verdict`, `gate_host`) < `gate_runs`, declared in `script_layering.py`. The mover masked
one import behind a method of the same name (`toolchain` in `Host.toolchain`); the first gate ended NOT RUN [driver] on it, and a new
real-host case (`scripts/selftests/gate/test_host.py`) now fails on any such name, which no planted-host case could.

**PB5411, the `Gate` class split.** `Gate` keeps the run and holds `PassCarry` (`gate_reuse.py`: reuse, carry, record, re-derive),
`Generations` (`gate_generated.py`) and `Legs` (`gate_legs.py`: plan, listings, first classes, hosts, population check), each returning
what it found and never calling back; a verdict a collaborator reaches is a value (`Ending`, `GenerationFailed`) that `Gate._finish`
writes, and `Outcome` and `HostRun` moved to `gate_verdict` beneath all three.

**Proof.** Gate `000003-20261114T135439Z` at `cf4af90432`: `=== BUILD-LOCAL GATE: GREEN — Conformance 24,880/24,880 ran · Unit 40,728/40,728 ran
· RepoDrift 177/177 ran · Characterization 37/37 ran (skipped 0)`; `python scripts/self_tests.py` GREEN over 196 self-tests; `script_layering.py`,
`inert_lane.py check`, `work.py check` and the DOC AUDITS GREEN. PB5429, PB5430 and PB5431 (the CONFORMANCE.md steps B1, B2 and C) are not
started and stay open for the next link of the chain. No compiler behavior changed.
