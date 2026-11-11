## Addendum 2026-11-11: the constant entry dates from ISO 2002, not 2023

Handover from the Mythos lane. PB5301's **Owed** paragraph asks for a "golden at the introducing edition (constants exist from 2023)", and its title and rule paragraph cite only 2023 clauses. The first half is wrong.

**The 2002 text.** `specs/ISO_COBOL_2002.md` carries the constant entry as clause 13.9 (2023 renumbered it 13.10). Citations, each run through `scripts/spec/cite.py --check`:

- `§2002:13.9` (Constant entry): "A constant entry defines a constant. A constant may be used in place of a literal." (OK). Its general format 13.9.1 prints `constant-name-1 CONSTANT [ IS GLOBAL ]` with the AS and FROM phrases; syntax rule 2, "Constant-name-1 may be used anywhere that a format specifies a literal of the class and category of constant-name-1", is in 13.9.2.
- `§2002:8.3.1.1.1.7` (Constant-name): "A constant-name identifies a constant, which is defined by a constant entry in the data division." (cite.py `--find` located the clause.)
- `§2014:13.10` is the same clause in 2014 (the heading "13.10 Constant entry" is in `specs/ISO_COBOL_2014.md`); `§2023:13.10` (OK).
- The 1985 text has no constant entry: its only "CONSTANT" hit outside figurative constants is the Appendix item "(4) Deletion of the CONSTANT SECTION of the Data Division" (a different, earlier feature). So the introducing edition is **2002**; the constant exists at 2002, 2014 and 2023, not at 85.

**The uniqueness rule in 2002.** `§2002:8.4.1.1 1)` (Qualification; OK, found by `cite.py --edition 2002 --find`): "No other name has the identical spelling." It is the same words as `§2023:8.4.2.2.1 1)`; 2002's clause number is 8.4.1.1, not 8.4.2.2.1. I did not locate the 2002 counterpart of `§2023:8.3.2.2` exception 3 (the allowance for one spelling shared by a constant-name and a data-name); the implementer reads that clause in the 2002 text before the golden is written. The 2002 and 2014 transcriptions in the repository are the reference, not the 2023 numbering.

**What this corrects in PB5301.**

1. Its **Owed** paragraph: "golden at the introducing edition (constants exist from 2023)" should read "(constants exist from 2002, §2002:13.9)". The refusal golden and the positive "a program that never references KK stays legal" golden are owed at 2002 and 2014 as well as 2023, each edition citing its own clause numbers (`§2002:8.4.1.1 1)`, `§2014`'s counterpart, `§2023:8.4.2.2.1 1)`), and `--std 85` has no constant entry to collide with (the constant entry itself is outside that edition, so no ambiguity case arises there; what 85 does with the entry is not this note's question).
2. The note cites only 2023 clauses; its **Rule** paragraph is correct for 2023 and, with the 2002 numbers above, for 2002 and 2014. The defect's scope is every edition that compiles a constant entry (2002, 2014, 2023), so a fix that gates the ambiguity refusal at 2023 would leave the 2002 and 2014 compilers accepting an ambiguous reference. The fix site (one candidate set across constants and data items) is edition-independent, as the note already says.
3. Nothing else in PB5301 changes: the repro, the sites, `status: open`, `inventory_rows: []`.
