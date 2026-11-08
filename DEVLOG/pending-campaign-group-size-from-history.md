## PB4944 planner half, PB4888 measured: a campaign whose groups always run over is planned smaller

**PB4944, the open half.** The landed half (w20261923c) made a STALL mean "stopped" and counted OVER-SIZE in the
PROGRESS line, per wave, never mailed. What it left open was the planner reading those counts, because a count on one
tick is forgotten by the next unit and the dispatch ledger drops a group when it lands. Re-probe: `progress.py --json`
on this tree returned the over-size list and nothing kept it, and `plan_wave.py` read no history of any kind.

The fix keeps the history per GROUP, not per wave, so the planner can read a campaign's pace and a tick's repeat is
idempotent. The new `scripts/orchestrator/group_sizes.py` owns `group-sizes.json` in the coordination directory. It
holds one entry per ledger group seen running: its notes, its campaigns (`work.note_campaigns`, now the one membership
rule, which `campaign_running_limit` reads through as well), its limit, the longest run seen, and `over`, which stays set
once a tick counts the group over-size. A stalled group is not over-size. An entry is kept for 30 days after it was last
seen. `progress.py --record` writes it on every supervisor tick, and `orchestrate.ps1` passes the flag.

`plan_wave.py --cluster` asks `group_sizes.campaign_cap`. When the campaign's latest 5 recorded groups (at least 3) all
ran over, the group size becomes `limit // median(minutes per note)`, at least one note and never above the turn cap.
The plan prints `group size <n> notes: <why>`. The cap reaches `campaign_clusters` and the partition
(`PlanState.group_size`), and an overflow's reason names the measurement, not turns. If one recent group fitted, the
campaign keeps the turn cap, because the owner's rule speaks of groups that ALWAYS run over. The fix lane always plans
at the turn cap.

Tests: the `group_sizes.py` self-test (7 arms: the empty history, the sticky maximum, the cap at least one note, too few
samples, one fitting group, the campaign's own 150-minute limit under the turn cap, the 30-day drop). The `progress.py`
self-test gains the running list's campaigns, limits and over flags and checks that a tick is recorded. The work.py
self-test covers `note_campaigns`, and `test_plan_wave.py` check 9d plans seven notes on one member at a measured size
of 2. All were GREEN locally (538/538 planner checks). `self_test_arms.py` declares the new arms.

**PB4888, measured after both levers.** The owner's two levers (targeted tests before a fix gate, I12; the `fix` gate
class) were on main by 2026-11-07 23:35Z. `measure_review_cycle.py` over waves 20261921 to 20261927 (17 branches):
FINDINGS-to-next-review median 21.5 min (n = 16), against 31 and 43 before; the round-1 FINDINGS rate is 12 of 17,
against 10 of 13. The surviving gate records from 2026-11-08 on show 8 fix-class gates, 1 RED (13 %, against 50 %
before), with a median slot wait of 0. The note is landed with the measurement. The round-1 rate barely moved, and the
note names the next lever to measure (the implementer's own self-review) as separate work.

Self-review: architecture (one writer and one reader of the history, one membership rule), code (the cap applies the
owner's "always"), performance (one JSON file per tick, at most a few dozen entries), duplication (the campaign
membership rule now lives once, in `note_campaigns`). No inventory rows; process only. Gate: see the report.
