## Entry 3328 — 2026-11-08 11:51 PST — PB4954: SET format rows carry their introducing edition, so COBOL2112 lists only the formats the edition prints

PB4939 gave `SetFormatSelection.Row` an optional 1985 form. Two residues: a row with no 1985 form would print 2023 wording
at 85, and the 2002 and 2014 lists still named later formats.

Re-probe (`SET WS-N UP BY 1`, `--std 2002` and `2014`): both listed Format 14 (a dynamic-capacity-table format) beside
Formats 10 and 2. Fix: `Row` carries `IntroducedIn` (the `EditionInfo` year). Formats 1 and 2 begin at 85; 5, 7, 9 and 10
at 2002 (`§2002:14.8.35.1` prints them, cite-checked); 8 and 14 at 2014; 16 and 17 at 2023. `ReportNoFormat` lists
the rows `InEdition`; a row reached outside its edition is labelled as a later format instead of printed in other-edition
wording. The note called Format 14 2023-only; Annex E (2014 against 2023) lists no SET Format 14 change and the construct
registry has OCCURS DYNAMIC at 2014, so Format 14 begins at 2014.

Pins: drift tests tie `IntroducedIn` to the 1985 wording (a row has 1985 wording exactly when it begins at 85) and to the
construct registry for Formats 5, 8, 9 and 10; a unit test compiles one statement at the four editions and checks which
formats the message names; golden `pb4954-set-no-format-list-omits-later-formats-at-2002`. No inventory row closed.

