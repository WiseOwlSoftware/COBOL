## The text-layer diff anchors an Amendment insertion at its place (PB6144)

`scripts/spec/text_layer_diff.py` (PB6135) excused a Markdown-only insertion as AMENDMENT-APPLIED when any run of three or
more of its words occurred anywhere in the note's Amendment entry, and the entry's tail carried the next table row's page
cell. Both round-4 review probes passed (a misplaced duplicated phrase, a stray `page`). One rule, `_stands_between`, now
checks a side with no words at its place: the page's words on each side must stand consecutively around the hunk in the
amended text, for a drop as before and now for an insertion; a hunk at the page's edge is never anchored. `entry_text`
ends an entry before the next row's page cell. Self-test case_15 plants both probes and two correct insertions that still
pass. Re-run over all 976 pages: AMENDMENT-APPLIED 57 → 45 (insertions 33 → 21), UNCOVERED 787 → 798, UNEXPLAINED 602 →
603, LIKELY-ERROR 126 unchanged (`kb/Work/PB6144/2026-11-24-anchored.md`).
