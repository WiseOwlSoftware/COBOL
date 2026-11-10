## Entry 3419 — 2026-11-10 00:25 PST — Errata C32's 2023 citation names the sub-item that holds its words; the self-test HANG and its orphans filed (PB3032, PB5098)

The tail of the PB5062 branch, after its first part landed with #1444. Review R1-1 found that Errata C32 (SET general rule 2 of the
1985 standard) and the notes that carry its grounds cited §2023:14.9.39.4 GR2 b) for "the table element associated with
index-name-2"; GR2 b) covers an identifier-1 that names an index data item, and the words are in GR2 a) 3. b., cited GR2a3b
(cite.py --check OK). The citation is fixed in the Errata entry, its flag, PB5031, PB5051 and PB5045, where it first appeared.
PB5021 records the KEPT/CHANGED conflict the review found between INSPECT general rule 3 and the PB5092 BACKWARD ruling. PB3032
gains the evidence of the 2026-11-09 hang: three gates ran the orchestrate self-tests at once and all 21 fake-claude cases
stopped before launching their unit; PB5098 is the runner's HANG stop, which killed each self-test's own process and left its
children running as orphans.

