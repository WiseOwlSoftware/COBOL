## Eight notes and four finding files from the Mythos lane's batch-3 rulings of 2026-11-15

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed at `--std 85` (and `--std 2023`), and the leads.

- PB5793 (defect): a comma or semicolon separator inside a subscript list is refused where a space may stand (`W-E (I +, 1)`), at every edition (`§1985:IV.4.2.1 SR2`).
- PB5794 (defect): the KEY of READ and START on an indexed file is matched to a record key by offset and width only, so a numeric item at an alphanumeric key's position is accepted, at every edition; the alphanumeric equal-width twin is a lead nobody has ruled.
- PB5795 (defect, rated medium by the checker): at `--std 85` a word that is an implementor-name is accepted as the computer-name of SOURCE-COMPUTER and OBJECT-COMPUTER.
- PB5796 (defect): EVALUATE accepts a THRU range between a figurative constant and an operand of another class (`WHEN SPACE THRU 9`), at every edition.
- PB5797 (defect): a contained program's EXTERNAL or GLOBAL table may depend on a counter declared in the containing program, at every edition.
- PB5798 (defect): at `--std 85` IF with NEXT SENTENCE and END-IF is accepted, which `§1985:VI.6.16.3 SR3` forbids.
- PB5799 (adjudication, open): does REDEFINES SR5 bar a redefinition inside an OCCURS DEPENDING ON table at `--std 85`? The text admits both readings; the same glossary sentence decides the row held by PB5737.
- PB5800 (adjudication, open): must LINE-COUNTER be qualified when the other report description is in a contained program or is GLOBAL (`1985-SR-IV.4.3.8.1-L2.9`)?

Four dated finding files beside existing notes: `kb/Work/PB5772/2026-11-15-a-third-row.md` (the row `1985-SR-VI.5.11.3-4` joins), `kb/Work/PB5734/2026-11-15-inspect-tallying-counter.md` (INSPECT TALLYING with a reference-modified counter, row `1985-SR-VI.6.18.3-6`), `kb/Work/PB5785/2026-11-15-the-expected-result.md` (the ruling on which result READ INTO must give, copied unchanged) and the source copied unchanged as `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-3.md`.

Row 11 of the batch (`1985-SR-IX.2.5.3-5`) is REJECTED by the ruling: the compiler conforms at 85, so no note is filed for it.

**The rate so far at 1985:** 36 of 420 rows read are contradicted (the sample 6 of 60, batch 1 9 of 120, batch 2 10 of 120, batch 3 11 of 120), about 9 %; 3 more undecided.
