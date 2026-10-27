## Entry 2637 — 2026-10-26 17:28 PDT — PB3379 recurs inside one lane: the planner plans a finisher for a group its running wave is still answering

During the pre-fuzz campaign (PB3852) the operator re-planned `--cluster PB3852` while wave 20261712 was still running,
to dispatch a same-file successor the moment its predecessor settled. The wave's group A (PB3554) had just drawn one
round-1 review finding, which the wave's own review finisher answers in its slot; the planner planned a finisher for it
anyway and ranked it first, so a one-group plan held only the duplicate. It was released unrun and the successor
(PB2923) dispatched from a second plan. The recurrence is recorded in PB3379, whose fix bar (a group whose wave has not
settled is held, its finisher included) covers it.

