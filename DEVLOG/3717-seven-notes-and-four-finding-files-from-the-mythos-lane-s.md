## Entry 3717 — 2026-11-15 19:46 PST — Seven notes and four finding files from the Mythos lane's batch-2 rulings of 2026-11-15

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed at `--std 85` (and `--std 2023`), and the leads.

- PB5784 (defect): at `--std 85` the abbreviated condition `NOT NOT <` is refused, where the 1985 rules make the first NOT the logical operator and the second part of the relational operator (`§1985:VI.6.3.3 GRL2.1`); the IS-spelled twin compiles.
- PB5785 (defect, MAJOR): READ INTO where every record description is shorter than RECORD CONTAINS gives status 00 and a short move, at every edition; which of the two results the text supports is right is decided before the fix (the note states both, with their citations).
- PB5786 (defect, MAJOR): a contained program's own file-name does not hide its container's GLOBAL data-name of the same word, and the reverse, at every edition.
- PB5787 (defect): at `--std 85` an unqualified LINAGE-COUNTER in a contained program is refused on a 2023 rule (COBOL5997); sibling of the landed PB5166.
- PB5788 (defect, MAJOR): a report file with a CODE clause and RECORD CONTAINS 2 or 1 is written with three characters (`ABW`), at every edition; the lead (a report file with RECORD VARYING compiles at 85) is marked unruled.
- PB5789 (adjudication, open): `1985-GR-XIII.3.10.7-1` cannot be ruled while `§1985:XIII.3.10.7 GR1` is reconstructed text (the scan lacks folio XIII-30, kb/Work PB4952).
- PB5790 (analysis, a lead, no ruling): at `--std 85` SIGN SEPARATE with BLANK WHEN ZERO is refused (COBOL2422) where the printed Table 1 (page 605) permits it.

Four dated finding files beside existing notes: `kb/Work/PB5731/2026-11-15-a-run-time-consumer-write.md` (the WRITE record length; `wrong_answer` is owed to PB5731's flags), `kb/Work/PB1003/2026-11-15-report-footing-and-page-heading-at-85.md`, `kb/Work/PB5779/2026-11-15-the-close-twin.md`, and the source copied unchanged as `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-2.md`.

**The rate so far at 1985:** 25 of 300 rows read are contradicted (the sample 6 of 60, batch 1 9 of 120, batch 2 10 of 120), about 8 %.

