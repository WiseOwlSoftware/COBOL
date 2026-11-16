## The 2002 golden lane, ninth landing: 131 witnesses at --std 2002 for 11 "wave u" files, 2002 GAP 1,899 to 1,801

The gl2002 lane (kb/Work PB5597) gives each 2002 row a witness of its own at `--std 2002`. This landing integrates 11
"wave u" files. These hold the rows that a 2002 read left needs-own-witness, undecided or uninterpretable. They cover
CALL, CANCEL, ALLOCATE, CLOSE, COMPUTE, CONTINUE, DELETE, DISPLAY, DIVIDE, EVALUATE, EXIT, FREE, GENERATE, GO TO,
GOBACK, INITIALIZE, INSPECT, INVOKE, MERGE, MOVE, MULTIPLY, OPEN, READ, RELEASE and SET, plus 30 rows of intrinsic
functions (ABS to VARIANCE).

**The shape.** 103 rows by work kind: 58 needs-own-witness and 45 undecided. The writers reported 94 new-golden, 4
existing-golden, 2 not-closable, 2 defect-suspected and 1 adjudication-needed. No writer deferred a row. The refuters
overturned 23 verdicts: does-not-exercise-rule 13, expected-value 9 and convention 1. The fixers applied all of them.

**What landed.** 131 cases, each validated on this branch's build, since no validate stage ran for these files:
- 62 new cases are accepted at [2002], and 2 existing 2002 cases (g2con01, g2exo01) widen their `rules` lists. run.py
  at 2002 on a manifest of exactly these 64 gave PASS 64, FAIL 0.
- 67 refusals have `refuse-at: ["2002"]`, and each was refused at `--std 2002` with its expected code.
- Mechanical repairs, with no expectation changed: CRLF to LF. Two drafts shared the PROGRAM-ID R1S, so the ids of
  g2call10 and g2call12 became G2C10M/G2C10S and G2C12M/G2C12S. g2ini02 and g2mov20 gained the header's
  `source-format` line, which generate.py requires.

**Records.** `record_verdicts.py --edition 2002` wrote 98 CONFORMS store files, and the 2002 GAP went from 1,899 to
1,801 on the branch. With main 111a265843 merged in (golden-2002-7 among it), it reads 1,707. Every test-ref runs at 2002 on this tree. Two records rest on a refusal alone, and both are SR rows that grant
no form (2002-SR-14.8.14.2-1, 2002-SR-14.8.19.2-4).

**Defects**, each re-run on this build:
- PB5762 (new): a table passed BY REFERENCE with a variable subscript loses EC-BOUND-SUBSCRIPT checking on every
  subscripted reference. An out-of-range MOVE reads past the table, and the CALL calls the program, against
  `§2002:14.8.4.3 GR3a`. It happens at 2002 and 2023. It blocks 2002-GR-14.8.4.3-3, whose draft g2call13 is held.
- PB5763 (new): a MERGE with an OUTPUT PROCEDURE that EC-SORT-MERGE-SEQUENCE terminates leaves its USING files open,
  against `§2002:14.8.23.3 GR6`. docs/conformance/3/D-SMA.md (b) and pb1036's leg G record that open state as
  intended. It blocks 2002-GR-14.8.23.3-6.
- PB5612 (a finding beside it): the 2023-only fatal disposition for SORT and MERGE also reaches MERGE GIVING
  -SEQUENCE at 2002. Row 2002-GR-14.8.23.3-6 was added to the note.

**Not closed.**
- 2002-GR-14.8.13.3-3 (EXIT RAISING LAST EXCEPTION) needs adjudication, so it was not recorded. The question is
  whether "currently raised" (`§2002:14.8.13.3 GR3c1`) means the run unit's last exception status or only a condition
  raised in the current element. g2exi04 lands with the leg on which both readings agree.
- 2002-GR-14.8.3.3-8: storage that ALLOCATE leaves uninitialized has undefined content, so no output can contradict
  the rule.
- 2002-GR-14.8.13.3-4: the activating runtime element would have to be non-COBOL, and the continuation is
  implementor-defined.
- The two defect rows above.
