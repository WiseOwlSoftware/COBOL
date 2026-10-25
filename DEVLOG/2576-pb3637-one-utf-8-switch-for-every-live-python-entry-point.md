## Entry 2576 — 2026-10-25 14:50 PDT — PB3637: one UTF-8 switch for every live Python entry point, and a drift test that runs every --help under cp1252

**Re-probe.** `python scripts/spec/work.py --help` through a pipe on the Windows host died with `UnicodeEncodeError:
'charmap' codec can't encode character '\u26d4'`: the module docstring is the argparse description, and `main()` switched
stdout to UTF-8 only after `parse_args()`, which prints the help and exits first. 126 Python files carried their
own `reconfigure` copy (122 of them live tools), written about ten ways. Run rather than read, the sweep found 31 entry points whose `--help` or
usage error crashed with stdout and stderr encoded cp1252, not the 71 the note's text scan counted.

**Determination.** The PB3726 plan (DESIGN-orchestrator-loop.md §16.10) named `PYTHONUTF8=1` set in the launchers.
The owner's direction of 2026-10-10, recorded in PB1546, puts the switch in the program ("this would work if the
compiler startup (plus likely test cases) set the console output encoding to UTF-8"), and a measurement agrees:
`PYTHONUTF8=1 PYTHONIOENCODING=cp1252` gives `utf8_mode 1` and a cp1252 stdout, and `work.py --help` still crashes, so
the plan's own oracle cannot pass with the environment alone, and a hand run carries no variable. §16.10 now says so.

**Fix.** `scripts/standard_streams.py#ensure_utf8` puts stdin, stdout and stderr in UTF-8 with UTF-8 mode's error
handlers (PEP 540), safe with no console or a replaced stream, with `line_buffering=True` for the two scripts whose log
another process tails (`run_gate_legs.py`, `record_impact_map.py`). Every live Python entry point calls it as the first
statement of its `__main__` block, or at module level after its imports when it has none (the hooks, the stand-alone
self-tests); scripts outside `scripts/` append it to `sys.path`. The 122 live copies are deleted (the four frozen records keep theirs), `land_lane.py`'s fake
enqueue child included, and the call was added to the 51 entry points that had none. The launchers keep setting UTF-8
for a child, as the owner asked; `ExternalCorpusPopulationDriftTests` lost its private copy of PythonInstrument's
interpreter probe and launches through it. Frozen records (`docs/**/evidence/**`, `adjudication/**`) are untouched.

**Drift test.** `standard_streams.py --check` holds every tracked `.py` file under `live_tools.ROOTS` to three rules:
no copy (COPY), the call first (ENTRY), and every argparse entry point's `--help` and a usage error exit 0 and 2 under
`PYTHONIOENCODING=cp1252 PYTHONUTF8=0` with no traceback and no backslash-escaped character (HELP; stderr's handler is
backslashreplace, so a usage error never crashes, it prints `\u26d4`). `ScriptStandardStreamsDriftTests` runs it as a
tree-wide sweep: 183 files scanned, 127 entry points probed, 0 findings, about 7 s. Its `--self-test` plants a parser
whose prog and help carry ⛔ and requires two findings without the call and none with it.

**Register.** PB3637 landed; PB1546, PB1651 and PB1726 are the same mechanism (work.py and filter_population.py
`--help`, where.py, drift_rules.py, record_impact_map.py `--help`) and land with it; each repro exits 0 after the fix.
No inventory row (tooling).

**The merge with main (PR #609 left the queue DIRTY).** One file conflicted:
`scripts/spec/audit_grammar_optional_words.py`, where PB715 (PR #593) had added a `--self-test` branch beside a
per-script `reconfigure` loop. The resolution keeps PB715's self-test and tree-parsing audit and drops the loop: the
file's `__main__` already calls `ensure_utf8()` first. Main also gained `scripts/orchestrator/cancel_superseded.py`
with its own `reconfigure` (the drift audit's COPY and ENTRY findings); it now calls `ensure_utf8()` like every other
entry point. That script runs in CI's supersede job from a SPARSE checkout of `scripts/orchestrator` only, where
`import standard_streams` (its own, and through `ruleset.py`) would raise `ModuleNotFoundError` behind the job's
`continue-on-error`, so the job cancelled nothing and stayed green; the sparse list now names
`scripts/standard_streams.py` (reproduced in a scratch copy of the sparse tree: the self-test fails without the
file and passes with it).

**Self-review.** Architecture: one function, one audit beside it, the readers of `live_tools.ROOTS` now five.
Code: the ENTRY rule's "runs code at import" counts a try that does more than import (a hook's `json.load(sys.stdin)`),
so the call precedes the stdin read. Performance: the probes are the sweep's cost, two interpreter starts per entry
point in a pool. Duplication: three copies of the `git ls-files -- live_tools.ROOTS` listing remain (self_tests.py,
scratch_repo.py, temp_names.py) and this audit adds a fourth; reported as a lead, not widened here.

