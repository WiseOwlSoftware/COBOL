## 1985 train F: the last 1985 train; the 1985 edition reads COMPLETE

Wave 20262132's train F lands in one pull request on main 5a97013803 (train E, #1935). It carries two fix branches, merged
into `train-1985-f` and gated once: group h (`train85e-e3-cat`, d6002bf228, reviewed CLEAN) and group j (`train85f-cascade`,
b965ed8578). It also carries the review lead R2-1 of group h. `python scripts/spec/edition_status.py --edition 1985` read
`=== EDITION 1985: NOT COMPLETE — 1 code reads · 4 open work notes ===` on train E's head and reads
`=== EDITION 1985: COMPLETE ===` on this train's head: 0 of 3,109 rule rows GAP, 0 code reads owed (1,357 stored), 0 open
work notes and 0 catalog prose holes (4,150 paragraphs measured).

**The read records' orphan test runs in the gate; every construct edge is quoted (h: PB6022, PB3965, PB5683, PB5924).**
`lineage_reads.unknown_rule_errors` is now the one orphan test. `lineage_reads.py --check` runs it, and so does
`import-reads` before it writes (PB6022). PB6020's moves already covered the three orphans. 122 construct rows had an
edition edge labelled "derived", "provisional" or "vendor transcription". Each one is now quoted from the 1985, 2002 and 2014
texts, every citation is checked, and every edge was confirmed. The VCR rows, doc comments and the regenerated registry
changed with them, and `ConstructEdgeProvenanceDriftTests` now refuses the labels (PB3965). FILES_DESIGN said RECORD IS
VARYING came in with 2002. It is in §1985:VII.3.8.2 Format 2, and the doc is corrected. PB5683 closes on its measure (no
1985 code read owed). PB5924 measures 0 prose holes at 1985 and narrows to the later editions. PB6039 is filed: 2002
floating-point literal limits.

**A refused file-clause operand is judged once (j: PB6041).** The 1985 code read of 1985-SR-VIII.2.4.3-1 found that every
refused RELATIVE KEY drew a second error on its file's START (COBOL0862, which named the key "(none)"). The sweep found
the same cascade in four more places: READ KEY IS and START KEY IS on a refused RECORD KEY or ALTERNATE RECORD KEY, START
without a KEY phrase, and the external-file rules for a refused RELATIVE KEY, FILE STATUS or LINAGE operand. `FileModel`
now records which clause operands were refused, and every user asks it first. A START over a refused clause binds to the
new `BoundRejected.OnRefusedClause`. Each of the 30 new cases is refused exactly once, at 85, 2002, 2014 and 2023. The code
read 1985-c04 is imported at eafbd52fe2.

**A table continued across pages is one table to `cite.py` (R2-1).** `§1989:A.2.4 Table 1` failed for the SQRT and
STANDARD-DEVIATION rows that `standard-compare-2002` quotes. `_resolve_table` bounded the table to its first page and never
read the parts captioned "Table 1: Table of Functions (Continued)". Now `text_units.continued_caption` reads that mark, and
the table is its first caption plus every continued caption of its number under the cited clause. A footnote between a
table and the caption below it belongs to the table. The sweep took every "(Continued)" caption in the 1985/1989, 2002 and
2014 transcriptions (2023 prints none) and quoted the first and last body row of each part. On the base 34 of the 48
quotations failed; on the head none does. `cite.py --self-test` and `text_units` case 11b carry the cases.

**Merge seams.** In `docs/VERSION_CHANGE_REFERENCE.md`, three conflicting hunks took h's quoted text, and main's canonical
designators inside them were applied again. RESOLVE's baseline took main's side and was rewritten (1,953 → 1,950 entries).
h's two `usage-object-reference-*-2002` rows carried the old sub-item spelling (GR14d1a), so they are now written
canonically (GR14d.1.a), and the registry was regenerated.
