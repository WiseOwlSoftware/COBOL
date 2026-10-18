## PB3359, PB3360 and PB3361 filed: three fleet-tooling defects found by the wave-1115 unit

Registered three fleet-tooling defects found by the wave-1115 unit (campaign PB3353). PB3359: the owner's 2026-10-17
~22:25 requirement that SORT and file I/O tests cover the abnormal and exception paths reaches an implementer only when a
wave unit edits groups.json by hand (wave 1115 did, specs re-rendered GREEN), and never reaches the branch reviewer, whose
prompt (`wf_rolling_wave.js` `reviewBranch`) reads no dispatch spec. PB3360: `plan_wave.py` reserves a wave's codes and
writes its files before it validates `--scratch` and `--stop-file`, and `make_dispatch_specs.py` records the groups in the
dispatch ledger before `check_practices.py` runs and keeps them on RED; a mistyped path burned COBOLNET4277-COBOLNET4300
and left eight phantom in-flight entries until the unit released them. PB3361: the rolling wave's finished test reads the
report path from the implementer's free-text `report` field, so group E (PB2849), gated GREEN and DONE with its report at
the canonical path, was logged SPLIT and got no reviewer because its `report` was a sentence. Notes only; no code changed.
