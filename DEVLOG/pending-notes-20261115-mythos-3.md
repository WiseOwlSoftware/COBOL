## Nine notes from the Mythos lane's rulings of 2026-11-15, and the 1985 inventory bullet loses its figures

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed at `--std 85` (and `--std 2023`), and the leads.

- PB5771 (defect, MAJOR): at `--std 85` a relation on a BLANK WHEN ZERO item that holds spaces is evaluated as if the item were zero, where 1985 compares characters (wrong answer). `pb509_blank_when_zero_relation` carries `accept: [85]` since 369468ccc6 and pins the wrong behaviour; its band loses 85 with the fix (a registrar change).
- PB5772 (defect): at `--std 85` a level 66 entry may rename another level 66 entry (`§1985:VI.5.11.3 SR4`).
- PB5773 (defect): SEARCH ALL accepts a parenthesised WHEN condition that Format 2 does not have, at every edition; the 2023 row `FMT-14.9.37.2` is wrong too.
- PB5774 (defect): at `--std 85` an index data item named in a CONTROL clause is refused on a 2023 rule (`§1985:XIII.3.7.4 GR3b` provides for it).
- PB5775 (defect): a report group name that is also a Working-Storage name is resolved without the ambiguity being reported (GENERATE, USE BEFORE REPORTING); 2023 rows `FMT-14.9.16.2`, `FMT-14.9.49.2`.
- PB5776 (defect, MAJOR): STRING into a group receiver holding an OCCURS DEPENDING ON table with its count item inside stores short and reports overflow; 2023 row `GR-13.18.38.4-8`.
- PB5777 (defect, MAJOR): UNSTRING with no DELIMITED BY examines such a group receiver at its maximum size; 2023 row `GR-14.9.48.4-11`.
- PB5778 (defect): at `--std 85` a CORRESPONDING operand is refused when a group ancestor is USAGE IS INDEX, where the rule is about the operand's own entry.
- PB5779 (decision): `1985-GR-XIII.4.5.4-9`: OPEN WITH NO REWIND completes with status 07 on this implementation, no program can witness the rule, and a 1985 entry in `docs/conformance/` is owed before the row closes.

The sources are copied unchanged beside kb/Work PB5683 as `2026-11-15-rulings-code-reads-batch-1.md` and `2026-11-15-adjudications-evening.md`.

**The rate so far at 1985:** 15 of 180 rows read are contradicted (the sample 6 of 60, this batch 9 of 120), about 8 %.

**The plan edit.** The bullet `THE 1985 INVENTORY` in `docs/REARCHITECTURE_PLAN.md` section 0 now names the command that computes the figures (`python scripts/spec/inventory_view.py --summary`) and the two lines of work (own-edition witnesses; the code read of every KEPT row, kb/Work PB5683) and carries no figure and no stamp. Every 1985 landing rewrote the hand-written figures and the "run ... at main ..." stamp, so every 1985 landing collided on that line (kb/Work PB5730's landing stopped on it on 2026-11-15). `git grep` of scripts, tests and .claude found nothing that reads the bullet.
