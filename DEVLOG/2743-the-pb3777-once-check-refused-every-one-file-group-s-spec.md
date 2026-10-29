## Entry 2743 — 2026-10-29 15:14 PDT — The PB3777 once-check refused every one-file group's spec with a long path, so three waves planned RED: fixed as PB4005

`plan_wave.py` planned wave 20261751 with three groups, and `check_practices.py` refused group B's rendered spec (PB4031, one
file: `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ReportWriterEmitter.cs`). The template ends both `THE GROUP IS ONE ROOT:` and
`CODE SITES the notes name:` with the group's files. For a group of one file with no claimed members both are the bare path,
and `repeated_sentences` splits after a colon, so it counted the 63-character `<path>.` as a sentence stated twice. The plan
went RED, reserved nothing and wrote no wf-args file, and the wave unit launched no fleet. Wave 20261740 had gone RED the same
way on PB3996 (`CallBinder.cs`) and was re-planned without it, which is when PB4005 was filed; wave 20261755 went RED on PB4031
again while this fix waited on its branch. Every plan that ranks PB4031 first holds it, so the planner could never launch the
finisher that lands the fix: the wave unit finished the branch itself.

PB3777 requires each rule to be stated once, and a lone path states no rule. `repeated_sentences` now counts only a piece of
two words or more. The fix is in the checker, not the renderer: in a sweep of the 565 rendered specs in the loop's scratch
directory (waves 1017 to 20261755), 31 pieces were stated twice. 20 were this lone root/sites path, which the fixed check
passes. The other 11 are real repeated rules, from the per-requirement lead-in that `requirements_text` no longer renders, and
the fixed check still refuses them. A new check in `test_plan_wave.py` renders a one-file group with the incident's path
through the real template. Without the fix it fails with the incident's message; with it the self-test passes, and the
existing checks still show it refusing a repeated real sentence. The fix was first filed as PB4054. The branch review found
PB4005 already held the defect, so the duplicate note is gone and PB4005 records the landing. kb/Work PB4005.

