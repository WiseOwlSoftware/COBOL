# Ruling: `1985-GR-XIII.4.5.4-18` takes a TEST, not a derivation; kb/Work PB5880 is withdrawn

Ruled 2026-11-16 about 18:15 PST by the Mythos lane session (Claude Mythos 5.2). Input for records branch 4: the
tracked copy is a dated file beside `kb/Work/PB5819.md`
(`kb/Work/PB5819/2026-11-16-the-row-takes-a-test.md`), which supersedes
`2026-11-16-the-row-can-be-witnessed.md` (that file stays as history). NOT YET TRACKED.

## What four drafts and three refuters established
`§1985:XIII.4.5.4 GR18`: "The minimum and maximum record sizes for a file are established at the time the file is
created and must not subsequently be changed." (cite.py OK)
1. No program's expected output follows from the 1985 text ALONE: each of the two results the text ties to a
   file's fixed attributes (status 39 at OPEN, status 04 at READ, `§1985:VII.1.3.5`) passes through an element the
   text leaves to the implementor (`§1985:XIII.4.5.4 GR6`: which attributes an OPEN validates;
   `§1985:XIII.3.2.4 GR2`: the report writer logical record structure).
2. No arm of section 1.1 of the conformance-review design fits. `unpopulatable-antecedent`: the rule's triggers are
   reached (first and third refuter). `definitional`: its first half is about the file the implementation creates
   (second refuter). `indistinguishable-consequent` naming GR6's row covers the OPEN route only.
   `unfalsifiable-under-determination`, even with two keys: OVERTURNED on its bound (the third refuter's second
   reading, `E:/COBOL-coord/scratch/my-20261115/unwitnessable/refute5/refute6.json`): a compiler that records a
   file's sizes when an OPEN creates it, judges a READ's status 04 by them, and wrongly overwrites them at a later
   OPEN EXTEND keeps both determined meanings (no OPEN validates a size; the bytes are the same text lines) and
   makes the rule false, observably.
3. The second refuter's program `g18rd2` is no witness and this compiler's output for it is no defect: its expected
   lines assumed a record structure the text leaves to the implementor (third refuter, first reading).

## The ruling
**The row closes on the third refuter's program `r6a`, as a test.** The design says an overturned derivation
becomes a test, and the overturn itself supplies what a witness needs:
- **Its expected output is computed, not copied.** The file is created under RECORD CONTAINS 12 CHARACTERS with two
  12-column lines, extended through the SAME description with one more line, and read through a sequential
  description of 12-character records. GR18 gives the sizes in force at the READ (those of the creation, 12,
  unchanged by the OPEN EXTEND); `§1985:VII.1.3.5` gives status 04 for a record whose length does not conform to
  them and 00 otherwise; and the two elements the text hands to the implementor are DOCUMENTED, so the standard's
  own answer is the documented one: the medium holds each line as its characters and a line end
  (`docs/conformance/7/DOC-A.1-159.md`), another description reads those bytes as fixed blocks
  (`DOC-A.1-146.md`, (a) and (c')), and no OPEN compares a record size on a sequential file (`DOC-A.1-129.md`).
  So: OPEN OUTPUT 00, OPEN EXTEND 00, OPEN INPUT 00, three whole 12-byte blocks READ 00, the residue (the three
  line ends: 6 bytes where the line end is CR LF, 3 where it is LF) READ 04, then the at end condition, 10.
  The case displays the STATUS of each READ and never the record area, so one expected output holds on every host.
- **It can fail on the rule.** The refuter built the compiler that fails it (sizes recorded at creation, changed at
  OPEN EXTEND): that compiler prints 04 on the three whole blocks. That is the difference from the draft case
  `g85_z03`, which was overturned (kb/Work PB5819) because it had no READ, so no compiler that breaks the rule
  printed anything else. A compiler that keeps no size at all prints the same as this one; it does not break the
  rule in any program, so it is no objection to the witness.
- The case's comment says all of this in three sentences: the rule, the two documented elements its expected
  output passes through (by register key), and the compiler that fails it.

**kb/Work PB5880 is WITHDRAWN before it was filed on main.** The decision to let the sixth arm name several keys
rested on this row alone, and the row does not stand under that arm. Nothing else needs it, so it is not built
(CLAUDE.md: work that did not need doing is a defect). Its implementer (wave 20262096 group A) was stopped at
18:14 after one commit; its branch `worktree-agent-a5cf4fc50e49c81d2` is ABANDONED and never lands. The id PB5880
stays unused.

## What records branch 4 does for this row
1. The conformance case, fixed-form, `--std 85`, under the directory of the 1985 Report Writer OPEN statement
   (find the clause directory with the cases of `1985-GR-XIII.4.5.4-*` rows; the cases beside it are the model).
   Source to adapt: `E:/COBOL-coord/scratch/my-20261115/unwitnessable/refute5/r6a.cob` (its run:
   `refute5/run/r6a-001`). Change it to display statuses only (`READ 00`, `READ 04`, `READ 10`), keep the loop
   bound, and handle the notice the runtime writes to stderr as the harness expects for a case of this kind.
2. The row's own-edition `test-ref` through the tool that records one for a KEPT row (kb/Work PB5675), never by
   hand; then `python scripts/spec/inventory_view.py --summary`: 1985 reads 0 GAP.
3. The two refuter records as history, copied unchanged to `tests/version-matrix/derivation-refutations/`:
   `unwitnessable/refute5/pb5822-1985-kept-5.jsonl` (DOUBTFUL) and `pb5822-1985-kept-6.jsonl` (OVERTURNED).
4. `kb/Work/PB5819.md`: `status: landed`, `blocked_by: []`, `closes_rows` as kb/Work PB5756 writes a 1985 row; the
   dated file above; and `kb/Work/PB5815/2026-11-16-the-row-that-waited-takes-a-test.md` (two lines: the row did
   not become the case `g18rd2`; it closed on another program, see PB5819).
5. The two register entries say they hold at every edition and name the 1985 rules (each through cite.py):
   `DOC-A.1-129.md` names `§1985:XIII.4.5.4 GR6` and the same sentence of the three other I-O modules;
   `DOC-A.1-159.md` names `§1985:XIII.3.2.4 GR2`. Then `python scripts/doc_audits.py --write-register`.
6. The lead of kb/Work PB5829 about the sequential sibling (`1985-GR-VII.4.3.4-25`, case `g85_bf25_01`) is NOT
   settled here: a dated file beside PB5829 says this ruling gives the test that case is to be measured against
   (does a compiler exist that breaks the rule and fails it?).
