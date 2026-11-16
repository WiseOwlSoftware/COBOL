**2026-11-16, ruled CONFORMS by the 1985 text (the Mythos lane's rulings on the ten open readings of the 1985 code reads; row `1985-SR-VI.5.10.3-5`; tracked copy `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, sections 3 and 4, which this note shares with kb/Work PB5737).** The section follows unchanged. The note stays open until the store's records are adjudicated on the next records branch.

## 3 and 4. An item below an OCCURS DEPENDING ON entry is not itself "a variable occurrence data item" in the rules (PB5737, PB5799)

**Ruling: CONFORMS, both rows. The 1985 text controls.**

The glossary sentence (`§1985:III.2`, "A variable occurrence data item is a table element which is repeated a
variable number of times", and such an item must contain the DEPENDING ON clause "or be subordinate to such an
item") can be read to make every item below an OCCURS DEPENDING ON entry a variable occurrence data item. The
rules do not use the term that way, and one of them cannot:

- `§1985:VI.5.8.3 SR1b` forbids the OCCURS clause in an entry that "Has a variable occurrence data item
  subordinate to it". If every item below an OCCURS DEPENDING ON entry were one, the entry
  `05 T OCCURS 1 TO 4 TIMES DEPENDING ON N.` followed by `10 A PIC X.` would have a variable occurrence data item
  subordinate to it, and SR1b would forbid the very OCCURS clause that makes the table. Format 2 on a group entry
  is plainly provided (`§1985:VI.5.8.3 SR3` speaks of a key that is "an entry subordinate to the entry containing
  the OCCURS clause"). So in SR1b the term means the entry that bears the DEPENDING ON phrase.
- `§1985:VI.5.15.6 GR5` uses it the same way: it speaks of "the DEPENDING ON phrase in the OCCURS clause specified
  for the variable occurrence data item", and it lists apart (GR5b) "It is a variable occurrence data item" and
  (GR5c) "It is a data item that is subordinate to a variable occurrence data item", which would be one case if
  the subordinate item were one.

With that meaning, `§1985:VI.5.8.3 SR1b` forbids an OCCURS entry ABOVE an OCCURS DEPENDING ON entry, which the
compiler refuses (`COBOL0854`), and leaves a fixed OCCURS inside such a table legal: `g1.cob` is legal and row 3
conforms. `§1985:VI.5.10.3 SR5` ("Neither the original definition nor the redefinition can include a variable
occurrence data item") forbids a REDEFINES pair that CONTAINS an OCCURS DEPENDING ON entry, which the compiler
refuses (`COBOL0855`, the control `r2.cob`), and leaves a pair inside such a table legal, as the same rule's
"data-name-2 may be subordinate to an item whose data description entry contains an OCCURS clause" says without
excepting format 2: `r1.cob` is legal and row 4 conforms.

GnuCOBOL does the same, which the ruling does not rest on: `cobc/field.c` lines 1422 to 1426 refuse a REDEFINES
only when `cb_field_variable_size` finds an OCCURS DEPENDING ON inside the redefinition or the original. No
register entry is owed. PB5737 and PB5799 close.
