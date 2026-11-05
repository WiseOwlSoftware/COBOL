## kb/Work PB4749: the orchestrator loop no longer has a meter unit

The loop still chose a `meter` unit whenever `readings.json` held no reading from the last three hours, and that unit
tried to read the claude.ai usage page through Claude in Chrome. The owner had ruled the page out, and `budget.py` has
anchored on the status line's cached `rate_limits` since PB4505. Because only a meter unit or the owner writes
`readings.json`, the rule fired every cycle. Seven attempts on 2026-11-04 and 2026-11-05 each spent a Sonnet session and
failed on a disconnected extension. On 2026-11-05 the owner said: "You have been told multiple times not to use the
chrome extension for meter readings."

The meter unit is gone. That covers its rule in `next_unit.py` (rules 4 to 7 are now 3 to 6), its prompt and its
`--chrome` launch, and the read-only unit handling only it used in `orchestrate.ps1`. The schema, `steer.ps1` and
`model_rules.json` drop it too. The design doc and MANDATORY-PRACTICES O3 now say the meter is `budget.py` and never the
usage page. A new core self-test proves that no unit is launched with `--chrome`.
