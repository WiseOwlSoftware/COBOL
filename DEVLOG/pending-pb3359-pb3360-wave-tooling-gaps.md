## PB3359 and PB3360 filed: an owner requirement on a campaign's groups has no path into the specs or the review; the planner reserves codes and records dispatches before it validates its paths

Registered two fleet-tooling defects found by the wave-1115 unit (campaign PB3353). PB3359: the owner's 2026-10-17 ~22:25
requirement that SORT and file I/O tests cover the abnormal and exception paths reaches an implementer only when a wave
unit edits groups.json by hand (wave 1115 did, specs re-rendered GREEN), and never reaches the branch reviewer, whose
prompt (`wf_rolling_wave.js` `reviewBranch`) reads no dispatch spec. PB3360: `plan_wave.py` reserves a wave's codes and
writes its files before it validates `--scratch` and `--stop-file`, and `make_dispatch_specs.py` records the groups in the
dispatch ledger before `check_practices.py` runs and keeps them on RED; a mistyped path burned COBOLNET4277-COBOLNET4300
and left eight phantom in-flight entries until the unit released them. Notes only; no code changed.
