## The fix lane's group cap is 4 notes, and the turns measurement behind it now counts every report (PB3704, PB3711)

Two notes, one root: `scripts/orchestrator/model_rules.json` and `scripts/orchestrator/turns_by_group_size.py`.

**PB3711 first, because PB3704 quotes its number.** The re-probe on the live reports directory found 771 `w*-report.md`
files and a tally that accounted for 702 of them. `turns_by_group_size.py` kept its own `REPORT_NAME`
(`^w(\d+)([a-z]+)-.*-report\.md$`), a second copy of the fix-lane report name that `plan_wave.REPORT_NAME` already owns,
and the copy required the group letter to be followed by `-`, so a same-file successor's report (`w1021b2-PB1042-...`)
failed the match and a bare `continue` dropped it without a count. The script now imports `plan_wave.REPORT_NAME` (one rule
in one place) and joins `w<wave><letter><n>-...` to `msg-w<wave>-<letter><n>.txt`, the spec name `make_dispatch_specs.py`
writes for a successor. A `w*-report.md` that is no group report (a train, refute or filing report) is counted as "not a
group report" instead of dropped, so every file is either joined or counted under one cause: 322 joined + 32 + 375 + 37 +
5 = 771. The sweep for sibling parsers found none: `plan_wave.py` and `prune_worktrees.py` already read the successor form
through `plan_wave.REPORT_NAME`. A sibling silent undercount in the same function went with it: `TURNS` read only
`**turns:** ~N` and `Turns used: about N`, so `**turns:** about N` (15 reports) and the unbolded `turns ~N` fell into "no
turns figure"; both forms are read now. The self-test gained a successor report and spec, a non-group report, and each
header form.

**PB3704, the owner's decision (2026-10-25).** `wave.max_notes_per_group` drops from 8 to 4, so a group lands within the
hour (the fix-cycle bar, PB3638). The re-measure with the successors and the extra header forms: 322 reports joined, a
least-squares line of about 65 turns per group plus 20.5 per note, so 4 notes is about 147 turns (the owner's "about 153"
is the earlier fit, 61 + 23 x 4). `max_notes_per_group_source` quotes the decision, the owner's words and this fit;
`cost.source` quotes the same line. The cap is one key read by `plan_wave.py`, `ledger_plan.py` and `gen_ledger.py`; their
self-tests pass at 4 (`test_plan_wave.py` 425 checks). The 400-turn cap (PB3701) and the sizing by predicted turns (PB3718)
stay their own notes; PB3718 was blocked on this one and is unblocked.

No diagnostic code was used. Gate: see the checkpoint that follows.
