## PB3643: prose has no merge driver, measured, and `merge_drivers.py --hunks` is the measurement

**The note.** PB3643 (filed from PB3610's landing-log measurement) asked for a union merge driver for
`docs/CONFORMANCE.md` and `docs/rearchitecture/DESIGN-runtime-library.md`, "restricted to files whose edits are appends",
because those two files stopped 7 of 541 landing logs (5 and 2). The condition was the part to check.

**Re-probe.** `merge_drivers.py --conflicts E:/COBOL-coord/logs` reproduced the rate (5 and 2, both NOT covered). Then every
merge commit in history that touched each file was replayed as git's line merge of its merge base and its two parents
(`git merge-file --diff3`), each conflict hunk classified by whether its base section is empty. Over `CONFORMANCE.md`,
`DESIGN-runtime-library.md` and `DESIGN-test-build-ci.md`: 45 conflicted merges, 3 insertion hunks (both sides inserted at
one place) and 115 revision hunks (both sides changed text the base already held: one mechanism's paragraph, one Annex A.1
register row). The files are not append-only where they conflict.

**Candidates, on 32 conflicted merges (the first two files, as the first walk listed them).** A union driver would keep both versions of a revised
paragraph in documents that must describe the current compiler (CLAUDE.md rule 6); not run, 115 of 118 hunks are revisions.
An insertion-only driver resolved 1 of 32, in a different order from the person's resolution. A word-level three-way merge
of the hunks resolved 9 of 32: 6 identical to the person's resolution, 3 different (it kept text the person had removed).
A wrong prose merge passes every gate; the conflict a finisher resolves does not. So no driver: such a conflict is a
decision about what the text now says.

**What landed.** `scripts/merge_drivers.py --hunks <file>...` (`conflict_hunks`, `hunk_kind`, `history_hunks`; `union_text`
and the new classifier share one `merge_file`), so the next "add a driver for X" is answered by a command instead of an
assumption; three self-test arms, declared in `MergeDriverDriftTests`; the module docstring and `DESIGN-test-build-ci.md`
section 3.17.2 step 3 say that prose has no driver, why, and to rerun `--hunks` before proposing one. PB3643 is retired
with the table; it claims no inventory row. The same-paragraph conflicts are the planner's class (PB3640, PB3670), not the
merge's.

**Self-review.** Silent failure: a text git cannot merge is counted `unreadable`, never as a clean merge. A document line
that looks like a conflict marker is content (the marker is read only in the state that expects it; an arm pins it).
Duplication: the temp-file writing of three versions existed once in `union_text`; it is now `merge_file`, used by both.
No wrapper, shim or alias was added.

**Review round 1 (R1-1).** `history_hunks` listed merges with `git log --merges -- <paths>`, whose default history
simplification drops a merge that is TREESAME to one parent (a resolution that took one side whole) and the side history
behind it, and drops different merges for each set of paths asked about: `DESIGN-runtime-library.md` read 51/17/0/56 alone and
55/18/0/59 beside two other files. The walk is now one `--full-history` log per path (a merge both parents changed the file in
differs from at least one parent, so none is missed, and a file's counts do not depend on the other paths). The honest
figures: `CONFORMANCE.md` 199 both-changed, 17 conflicted, 1 insertion, 31 revision; `DESIGN-runtime-library.md` 59/22/1/79;
`DESIGN-test-build-ci.md` 50/6/1/5, so 45 conflicted merges, 3 insertion hunks and 115 revision hunks. The conclusion stands
(revisions are 97 % of the hunks). A third self-test arm pins a `-s ours` merge and the path-list independence.
