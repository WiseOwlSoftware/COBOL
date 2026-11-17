# Rulings on the re-reads of slices 1985-245 and 1985-246 (batch 19)

Written 2026-11-16 by the Mythos lane session (Claude Opus 5.5, main thread). Read pass 18: run wf_0c984f78-af3 of
`coderead-1985/coderead-sample.js` on a compiler built at main 6450516b5e (`E:/COBOL-wt/coderead85-tree-d`). Rows:
the KEPT row `1985-GR-XIII.4.5.4-18` and six rows whose fixes landed (PB5881, PB5883, PB5884 x2, PB5885, PB5888).
`python -I tally.py 245 246`: 7 rows; 5 CONFORMS with the refuter UPHELD; 1 DOUBTFUL (`1985-SR-IX.4.5.3-2`); 1
OVERTURNED (`1985-SR-XIII.4.9.3-4`). The five agreeing rows import as the tally gives them. Every citation below
answered `python scripts/spec/cite.py --check` OK in this session.

## 1985-SR-IX.4.5.3-2: the doubt is REJECTED; the row CONFORMS on the recorded implicit-key reading

The refuter's doubt: the compiler admits, as the KEY of READ, a group item lying at the record key's character
positions in another record description of the file, and refuses a numeric item, an alphanumeric-edited item and a
shorter prefix there (`out/refute-1985-245/src/rk_*.cob`). The refuter says only an unstated reading draws that line.

Ruled: the reading is stated and recorded. `§1985:IX.4.5.3 SR2` asks for "a data item specified as a record key";
`§1985:IX.2.7.4 GR3` says the key's identical character positions "are implicitly referenced as keys for all other
record description entries of that file". kb/Work PB354 (landed) chose the implicit-key reading. kb/Work PB5794 (landed) and my
batch-16 ruling (`kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-16.md`, mechanism 2) refined it: the implicit key is
the character positions of an alphanumeric key (`§1985:IX.2.7.3 SR2` makes every record key alphanumeric), so an
item there that names those positions as a key must be one a key could be (alphanumeric; a group item is), and a
numeric or alphanumeric-edited item with its own description, or an item covering only some of the positions, does
not. At 1985 the compiler's test (the same class, category and usage as the key) and that one (category
alphanumeric, every alphanumeric item being DISPLAY) admit the same items. CONFORMS. The row's notes name kb/Work
PB5794 for the reading.

## 1985-SR-XIII.4.9.3-4: the refuter is UPHELD; new defect kb/Work PB5890 (rejects legal source, every edition)

`§1985:XIII.4.9.3 SR4`: "A USE BEFORE REPORTING procedure must not alter the value of any control data item." The
compiler refuses (COBOL2378) five statements that alter no control data item. Program
`E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-246/rcorr.cob`: in a USE BEFORE REPORTING procedure, with
`CONTROL IS CTL` and CTL (PIC X(2)) the first item of group GRP, while SRC has no item named CTL:
- `MOVE CORRESPONDING SRC TO GRP`, `ADD CORRESPONDING SRC TO GRP` and `SUBTRACT CORRESPONDING SRC FROM GRP` store
  only the corresponding pairs (`§1985:VI.6.19.4 GR1`: "selected items within identifier-1 are moved to selected
  items within identifier-2"), and CTL has no corresponding item;
- `INITIALIZE GRP REPLACING NUMERIC DATA BY 7` (and `BY 0`) initializes an elementary item "only if it belongs to
  the category specified" (`§1985:VI.6.17.4 GR2`), and CTL is alphanumeric.
Its control `rcorr2.cob` (CTL outside GRP) compiles and runs (EXIT 0). The same five refusals at `--std 2023`.

Root (the refuter's reading of the code; to be confirmed by the fix): `BoundStores` (`StoreKindVisitor`) answers a
store for `Visit(BoundCorresponding)` through `Hit(n.ReceivingGroup)` and for `Visit(BoundInitialize)` through
`Hit(n.Written)` before `InitStores(n.Actions)`, so the whole receiving group counts as altered, and
`ReportWriterBinder.SameStorageLine` matches any group that contains the control item. kb/Work PB5885 (landed,
pull request 1838) made `BoundStores` the ONE answer to "what does this statement store into"; this is a defect in
that answer for the verbs that store into SELECTED subordinate items. The fix asks, for CORRESPONDING and for
INITIALIZE, only the elementary items actually stored: the corresponding pairs; INITIALIZE's actions after its
category selection (REPLACING), its exclusions (index data items, items subordinate to a REDEFINES, FILLER where the
edition excludes it: read each edition's INITIALIZE rules) and its default. Sweep every other caller of that answer
(the OO property screen and the debug screen the refuter names): say for each whether it needs the group (the
receiving operand as written) or the items stored, and give each the one it needs from the one visitor, never a
second visitor. Every edition unless an edition's own text differs. Harm `rejects_legal_source`, severity MINOR.

## Leads that are not yet anything
- The reader of slice 245: COBOL0864 cites `§2023:14.9.30.3 SR11` at `--std 85` (a diagnostic naming the wrong
  edition's rule; it does not change what is refused). Joins the slice-244 lead of the same kind.
- The reader of slice 246: a store reached from a USE BEFORE REPORTING procedure through a PERFORM of a USE AFTER
  ERROR section, or through CALL BY REFERENCE, is accepted (the lexical reading of SR4). Nobody has ruled whether SR4
  is lexical.
