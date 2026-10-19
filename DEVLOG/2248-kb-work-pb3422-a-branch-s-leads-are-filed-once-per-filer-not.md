## Entry 2248 — 2026-10-19 01:31 PDT — kb/Work PB3422: a branch's leads are filed once per filer, not once

The leads of PR #268 (PB3336, which carried PB3334 and PB3335) were filed three times. The previous enqueue unit's
clerk filed the A6 report's leads as PB3409-PB3411 (PR #275) before the branch landed. The Mythos lane filed the A5
report's round-5 leads as PB3414 and PB3415 (on PR #276). Then this enqueue unit filed the same leads again as PB3417
and PB3418 (PR #278), and the operator retired those as duplicates (`notes-retire-pb3417-pb3418-20261019`).

PB3422 records the mechanism. Step 5 of the enqueue unit reads "a land-lane attempt not yet collected" as "leads not
yet filed", but collection marks an attempt. A branch landed again after a bounce gets a new, uncollected attempt.
Filers outside the unit collect nothing. The report itself never records that a lead was filed. The R1 grep looks at
the worktree's base, which predated PB3410's merge, and does not look at notes still on a queued branch. This unit's
own check also cut its grep output with `head`.

The fix the note names is one record at the source: `filed as PBnnnn` on each lead line of the report, written by one
tool. Step 5 then selects leads that are not yet marked. R1 also greps the open notes branches, and a drift test pins
both.

