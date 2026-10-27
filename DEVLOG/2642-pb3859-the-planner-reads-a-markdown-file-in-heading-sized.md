## Entry 2642 — 2026-10-26 17:59 PDT — PB3859: the planner reads a markdown file in heading-sized units, so different rows of CONFORMANCE.md serialize

The owner asked why `docs/CONFORMANCE.md` held four of the pre-fuzz groups (PB3852) behind one another, since git merges
edits to different lines cleanly. The cause is the file-set partition, not git: `source_units.markdown_units` makes one
unit per heading, and the Annex A.1 register is one ~390-line section, so a branch editing DOC-A.1-1 meets one editing
DOC-A.1-92. PB3643's replay agrees: 17 of 199 merges that changed the file on both sides conflicted, all on the same
paragraph or row. The owner overrode the planner for tonight with a hand-planned wave whose claims name rows; PB3859
records the defect and its fix direction (a keyed table row is its own unit), for the operator's process lane after the
twelve pre-fuzz notes land.

