## Entry 2462 — 2026-10-24 10:26 PDT — PB3611: the fix lane's group cap is eight notes, restated nowhere else

The planner (`scripts/orchestrator/plan_wave.py`) hands `fix_clusters.py` the cap `model_rules.json` `wave.max_notes_per_group`,
which was 5 and made more branches than the landing rate (1.5 per hour, `landing_measure.py summary`) carries: wave 1157
made 11 branches for 15 notes.

Re-probe. `plan_wave.py --budget-points 3 --dry-run` at cap 5 planned the DataBinder.cs group with 5 notes; at cap 8 it
takes 7, the two extra notes (PB1319, PB3162) naming DataBinder.cs as a site, which would otherwise each have been a
group and a same-file successor. Everything else in the plan stayed, because most open files carry fewer than 5 notes.

Why 8, not the 10 the note allowed. 314 implementer reports state their turns; turns against the notes the report
names run about 66 + 13 per note (1 note 79, 3 notes 108, 5 notes 130, 6 notes 150). Eight notes is about 170 turns of
the 220-turn cap, ten about 196, where reports begin to SPLIT (190). `cost.per_group_tokens` and `per_note_tokens` are
unchanged because `group_cost` is linear in the notes; the larger cap changes what one group costs, never what a note
costs. The measurement is recorded in `max_notes_per_group_source`.

Structure. The docs that spelled the number ("cap of five", "caps at 5") now name the key (`plan_wave.py` module
docstring, `DESIGN-orchestrator-loop.md`, the workstream `SKILL.md`, `DESIGN-architecture-review.md`), so the next change
is one edit. `test_plan_wave.py` 338/338 and `test_rolling_wave.py` 41/41 pass; `work.py check` passes.

Not measured: whether a larger group costs review depth (findings per note before and after). The reports of the first
wave planned at cap 8 carry that figure.

