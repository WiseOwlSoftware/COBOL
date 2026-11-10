## Entry 3444 — 2026-11-10 12:22 PST — PB5155: the orchestrate self-tests run their supervisors in a fixture HOME, so none reads the developer's live quota meter

Five gates in two nights went RED with 13 or 14 `test_orchestrate_*.ps1` self-tests hung, the last two for 13,869 s and 8,932 s,
while the same files passed alone. The cause was not contention. Each case's supervisor asks `budget.py` whether to proceed,
and `budget.py` reads the account's live meter from `~/.claude/rate-limits.json`, which the status line of the developer's own
sessions writes; the test's temp telemetry directory does not change that. With the developer's account over its daily
allowance and the reading fresh, the answer was hold-day and every supervisor waited for tomorrow; with the reading stale, or
after the owner's reset put the account under the allowance, the tests passed in seconds. PB5113 had isolated one case of the
account part. `Run-Orch` now runs every supervisor in a fixture HOME with `CLAUDE_CONFIG_DIR` unset. Proof: main's library
printed the real figure (`weekly 20 % of 42.9 %`); on the branch the core part asserts that no reading is in reach and that a
reading planted in the fixture HOME (3 %) is the one reported. All 14 files pass, 257 checks.

