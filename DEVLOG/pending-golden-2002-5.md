## The 2002 golden lane, fifth landing: 121 witnesses at --std 2002 for 17 more files and the PB5625 rows, 2002 GAP 2,077 to 1,983

The gl2002 lane (kb/Work PB5597) gives each 2002 row a witness of its own at `--std 2002`. This landing integrates 17
of the lane's 85 input files (PROPERTY, REDEFINES, SAME AS, SOURCE, SYNCHRONIZED and USAGE clauses; the procedure
division structure; ACCEPT, EXIT and WRITE (second part); the ANNUITY, DAY-OF-INTEGER, EXP, LENGTH, LOG10 and ORD
functions; B.1 locale use) and the 12 rows that kb/Work PB5625's adjudication decided, each judged under the reading
that adjudication gave it.

**The shape.** 103 rows by work kind: 62 needs-own-witness, 22 refused-evidence, 11 own-verdict-from-pointer and 8
refused-needs-accepting-case. The writers reported 92 new-golden, 5 existing-golden, 5 not-closable and 1 open. No
writer deferred a row. The refuters overturned 15 verdicts: does-not-exercise-rule 6, convention 4, citation 2,
expected-value 2 and evidence-kind 1. The fixers applied all 15.

**What landed.** 121 cases, each validated on this branch's build, since no validate stage ran for these files:
- 60 new cases are accepted at [2002]. run.py at 2002 on a manifest of exactly these 60 gave PASS 60, FAIL 0.
- 61 refusals have `refuse-at: ["2002"]`, and each was refused at `--std 2002` with its expected code.
- Nine PROGRAM-IDs collided, with each other or with earlier cases, and were renumbered with no expectation changed.
- `docs/conformance/3/D-USAGE-NATIONAL-CLASS-2002.md` and `D-CASE-MAPPING.md` (PB5625 readings B and D) now name
  the cases that pin them at 2002.

**Records.** `record_verdicts.py --edition 2002` wrote 95 store files: CONFORMS 93 and DOCUMENTED-NON-SUPPORT 2. The
2002 GAP went from 2,077 to 1,983.
- The two non-support records are 2002-GR-13.16.45.2-3 (the REQUIRED clause) and 2002-GR-14.8.1.3-21 (the screen
  format of ACCEPT). Each pointer is DOCUMENTED-NON-SUPPORT, and each 2002 licence is `§2002:3.1.5` with `§2002:B.3`
  item 1. Their a42 witnesses were refused at 2002 on this build (COBOL1560 and COBOL1707).
- 2002-GR-13.16.40.3-1 was already CONFORMS (PB5598). The writer's record widens its witnesses, and the earlier
  witness and notes are kept.
- The writer's code-location for 2002-GR-A.2.2.3-6 was prose. It now carries its 1985 twin's stored code-location,
  which the writer named.

**Defect.** PB5729 (new): a REDEFINES subordinate to the second or later record of a multi-record FD overflows the
compiler's stack in `StorageFormPass.Classify` (exit 127, at 2002 and 2023). It was re-run on this build. It came from
the REDEFINES writer as a side finding, and no open row waits on it.

**Not closed.**
- 2002-GR-16.1.1.1-2 (a failed New leaves the object reference null). The fixer's witness `g2oonew01` invokes NEW on a
  class holding `PIC X OCCURS 2147483647`. Host safety (kb/Work PB5695, PB5696) forbids running a program of that size,
  so the case is held, and the row needs a small witness or a compile-time refusal.
- 2002-GR-12.3.4.6.3-3. The record is DOCUMENTED-NON-SUPPORT and its only witness is a refusal, `g2col03`, but the row
  has no pointer. Rule 2's exception needs a non-support pointer, so the record was dropped. The case lands as a
  refusal.
- The 5 not-closable rows and the 1 open row:
  - 2002-GR-14.1.3-7: the returning item's initial value is undefined, so no output can contradict it.
  - 2002-GR-14.8.47.3-20: a reel/unit swap during a WRITE, and no medium here has reels.
  - 2002-RV-15.80.2-3: no listing, and the object code carries no compilation time.
  - 2002-DOC-B.1-91: the default locale under the adapter's pinned environment.
  - 2002-DOC-B.1-92 (open): it needs a witness that depends on the category.
  - 2002-DOC-B.1-93: the switch is in a non-COBOL module.
