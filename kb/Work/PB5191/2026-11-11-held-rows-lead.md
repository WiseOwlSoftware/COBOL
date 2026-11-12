**Addendum (2026-11-11): the lead again, with a mislabelled message.** The set-1 refuter of `1985-GR-XIII.3.17.4-1` and the operator's refuters report this note's refusal in Working-Storage as well as in the Report Section.

Rerun on a build of origin/main d9dde0e7ba (2026-11-11): `E:/COBOL-coord/scratch/held-85/set1/refute/bz1.cob` (`01 X PIC S999 BLANK WHEN ZERO.`) exits 65 at --std 85 with two errors:

- `error COBOL1934: invalid PICTURE S999 ... the symbol 'S' is specified beside a BLANK WHEN ZERO clause`
- `error COBOL2405: data item 'X': the BLANK WHEN ZERO clause may be specified only for an elementary item described by its picture character-string as category numeric-edited or as numeric without the picture symbol 'S' (ISO §2023:13.18.8.3 SR1) — this item is of category alphanumeric`

The second message calls a PIC S999 item "category alphanumeric", which is wrong (misleads diagnosis). The fix of this note removes both refusals at 85. The category the message reports should be checked at 2023 too, where the refusal is right but the stated category is not.
