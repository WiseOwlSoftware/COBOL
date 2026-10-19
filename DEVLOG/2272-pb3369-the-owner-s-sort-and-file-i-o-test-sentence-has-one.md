## Entry 2272 — 2026-10-19 16:23 PDT — PB3369: the owner's SORT and file I/O test sentence has one home, MANDATORY-PRACTICES I11, and a drift test keeps it so

Wave 1131, group H. The note (filed 2026-10-18) found that the owner's standing test requirement (SORT, MERGE and file
I/O tests cover the abnormal and exception paths as fully as the success paths) reached a group only through a campaign
lead note's `## Standing requirements`, so each campaign needed a hand copy of the sentence and the copies could drift.
The 2026-10-18 landing already put the rule into MANDATORY-PRACTICES I11 and into every rendered implementer spec and
finisher brief, which closed the notes in no campaign and the hand-dispatched finishers. This change finishes what the
note left.

**Re-probe.** On a fresh worktree of main (c82c9213f), `git grep "out test cases for sort"` found the sentence in
I11 and, as a campaign requirement, in kb/Work/PB3353.md and kb/Work/PB3330.md, and the `review` skill's branch form
already checked I11 on every diff that reaches a SORT, MERGE or file I/O path. The public base (v1.21.0) already
carries the generalized failure-path standard in `engineering-standards` and `review`.

**The copies.** PB3353.md held nothing in `## Standing requirements` but the sentence, so its section is now a
pointer to I11 under a heading the planner does not read. PB3330.md keeps the keyed engine's own bullets (its '30' and
'51' statuses, process death and refused write or sync at every point, interleaved run units, the fault-injecting
medium) and loses the bullets that restated I11's case list. Both notes point at I11. DESIGN-orchestrator-loop.md
section 9.1 and the workstream skill now say a lead's bullets are campaign-specific cases only.

**The drift test.** `scripts/orchestrator/test_plan_wave.py` section 9a refuses the sentence in the
`## Standing requirements` of any `kb/Work` note, requires I11 to carry it, and re-points the two checks that read the
real PB3353 note (membership binding of a real member, the text stating the membership rule) at PB3330, now the one
lead with campaign-specific bullets. The old PB3353 section fails the new check, which was confirmed by running the
check's predicate over it.

**The decision the note asked for.** Whether the I11 paragraph should become a binding computed from the group's file
set. Measured over the 356 findings in the branch reviewers' findings files, waves 1116 to 1131: 25 are category
`requirement`; 21 of those name a missing exception-path test on a branch that changes a SORT, MERGE or file I/O path;
the other four are the planner's own binding defect, a held-slice bullet of PB3330, a code comment and a CONFORMANCE.md
row. No false I11 finding on a non-I/O branch, so the paragraph stays unconditional and no area list is added (the
register's `area` values are not normalized, so a computed binding would be a list to maintain).

**Owner test requirement.** The change reaches no SORT, MERGE or file I/O path: it edits two register notes, a planner
self-test, a design section and a skill sentence. The drift test is its witness.

Rows: none claimed, none closed. Gate: see the report.

