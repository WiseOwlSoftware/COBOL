## Entry 2708 — 2026-10-28 16:55 PDT — File PB4005: PB3777's once-check refuses a spec that names a long root path twice

Wave 20261740's first plan went RED on group D (PB3996, a Format 1 CALL that accepts OMITTED). The rendered spec names
its one root file, `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/CallBinder.cs`, after `THE GROUP IS ONE ROOT:` and
again after `CODE SITES the notes name:`. `check_practices.repeated_sentences` splits after each colon, so the bare path
with its full stop (65 characters) counts as a sentence, and its second copy trips the 60-character once-check. The check
is meant to refuse a repeated rule, and here it refused a repeated datum.

The wave unit re-planned with `--max-groups 3` and launched groups A to C without D. PB4005 records the defect, its
reproduction and the acceptance: decide whether the root cause is the duplicate CODE SITES clause or the sentence
splitter, then add a self-test seen RED first and sweep the other rendered fragments that state a path after a colon.
No code changes in this landing.

PB2100 records its predecessor branch `worktree-wf_c731503d-cbd-1` (wave 20261733 group A, SPLIT) as ABANDONED: every one of
its 13 commits is patch-equivalent on main (`git cherry`), carried by w20261734a into #736. The record lets
`prune_worktrees.py --apply --abandon` remove it.

