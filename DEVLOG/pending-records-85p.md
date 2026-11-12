## PB4995: the 14 1985 rows with no stored file are KEPT, and they wait for PB479's 2023 rows

Fourteen open 1985 rows had no file in `tests/version-matrix/inventory-1985/`. Twelve are the rows kb/Work PB4995 lists as having no change record; the other two are `1985-GR-IV.6.4.2.1.1-3` and `1985-SR-X.4.6.3-2`. On origin/main d457dec4d5 the 1985 view reported 321 GAP rows, and exactly these 14 have no store file.

Each pass planned them, and each failed for the same reason:

- the audit's adjudicate pass answered OPEN;
- the adjudicate-712 fleet decided KEPT but left `to` empty and held the records back (`adj-712-uncatalogued.json`); its round 2 withdrew the one record that named a target, `GR-13.18.27.4-1`, which states no part of the rule;
- classify-unrecorded-1985 left all 14 UNDECIDED.

The 2023 text states each requirement only in prose or in Table 12, and the 2023 catalog has no row for that text: §2023:14.5.1 and Table 12 for the conditional statements, §2023:14.5.2 for the conditional phrases, §2023:10.4, §2023:14.6.2.2 and §2023:8.4.2.1. A KEPT record must name a 2023 rule. This is the gap kb/Work PB479 holds. No script skipped the rules.

I re-adjudicated all 14 from both texts and decided each one KEPT. Every citation passed `cite.py --check`. A dry run of the 14 records through `record_verdicts.py --edition 1985` refused every one: "a KEPT record names the rule(s) of the next edition it carries into". So nothing was written, and the 1985 GAP stays at 321. The records are in `E:/COBOL-coord/scratch/records-85p/kept-batch.json`, ready to re-run once PB479 admits those 2023 paragraphs and Table 12 as catalog rows. The forensic record is `kb/Work/PB4995/2026-11-12-records-85p.md`.
