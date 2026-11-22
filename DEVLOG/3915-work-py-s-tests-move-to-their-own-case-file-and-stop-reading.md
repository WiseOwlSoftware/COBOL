## Entry 3915 — 2026-11-22 11:57 PST — work.py's tests move to their own case file and stop reading the live register (PB3202)

kb/Work PB3202, an R2 finding of the PB1754 review, says scripts/spec/work.py's register rules were tested only inside
the wave planner's test file, against the live register and tree. The filed scenario had two halves. Re-probed on
9f2fc36d66, the slice half no longer reproduces: PB4452 deleted `work.slice_file_set` and `open_slices`, so no slice
plant is left. The other half still reproduced. The planner's `case_147` (PB5428's conversion of
`test_plan_wave.py`) ran `r3_program_problems` over `work.load()`, which held 126 open R3 notes, plus a plant on
`Binding/BinderDriver.cs`. It filtered the result with `m.startswith(pid)`, which every message passes, because each
starts with the note's file name. Pointing the plant at a renamed folder returned "which does not exist", so the
Binding/ rename the R3 program plans would have turned the case red with no rule changed. work.py's frontmatter
reader, meanwhile, was tested only by an in-file `--self-test`.

The fix gives work.py one case file under the case layout, `scripts/selftests/spec/work/test_work.py`, with 25 cases
and 137 expectations. It holds the in-file self-test converted to cases: the reader against
`tests/version-matrix/work-frontmatter-cases.json` and its falsification, scalars, a fixture directory, upstream
records, addenda, views, process_only, unflagged kinds, the harm vocabulary, frontier_model and running_limit_min. It
also holds the thirteen work.py cases moved out of the planner's file (topology, tiers, the site rules, the R3 rules,
the edges removers) and the topology half of case_140. Each case builds its own register and, through
`scratch_repo.ScratchRepo`, its own committed tree; the harm-vocabulary case alone reads the real tree, because that
tree is its subject. The R3 case states the words of each rule and compares the complete list of (file, rule) pairs,
so a message it does not recognize fails by name. `sites_problems` and `r3_program_problems` now take the tree as
`repo`, defaulting to this repository. work.py loses its `--self-test`, the 578-line section and its
`selftest_layout.REMAINING` line. The planner's live check of the edges file (`edges_remover_problems(REAL)`) is
dropped, because `work.py check` already runs it in CI on every change. The planner keeps its own cases and still
passes.

Two mutations confirmed that the new cases can fail. Reading the live tree in `r3_program_problems`, and disabling the
drift-test wait rule, each turned the R3 case red. PB3838's site block named the moved members, and `check` refused
it until the block named the case file's members instead. PB5406, the conversion of `scripts/spec/m*.py` to `w*.py`,
now leaves work.py out of its file set.

