# PB6168: the branch review's R1 fixes never reached main

PB6168 landed, but its branch `closed-hierarchies-wave1` still holds two commits that main lacks (`git cherry origin/main
closed-hierarchies-wave1` at c5b98c9bed: 2da612c244 and daff9a68d8): the R1 review fix (`scripts/buildgraph.py`'s
`FIRST_TYPE` and `TYPE_DECL` and `drift_rules.py`'s `test_types` abstract flag know `closed`; the architecture-review
skill and design name the closed mechanism), the PB6170 site list, and the new note PB6206. The survey reads the branch
as superseded because its report's note is landed. Decision (resume unit 2026-11-27): land it. A finisher merges main
(the branch is idle since 2026-11-24), gates and enqueues it.
