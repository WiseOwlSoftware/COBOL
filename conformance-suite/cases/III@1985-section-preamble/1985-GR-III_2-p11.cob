      *>--- conformance-case
      *> id: 1985-GR-III_2-p11
      *> rules: [1985-GR-III.2-p11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p11 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p11: an arithmetic statement or expression gives
      * the mathematically correct result.
      * Expect: C1 +041.5 OK / C2 0.3 OK / C3 2.50 OK / C4 -12 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11AR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1  PIC S999V9 SIGN LEADING SEPARATE.
       01  R2  PIC 9V9.
       01  R3  PIC 9V99.
       01  R4  PIC S99 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       P0.
           COMPUTE R1 = 7 * 6 - 2 / 4.
           IF R1 = 41.5 DISPLAY "C1 " R1 " OK"
           ELSE DISPLAY "C1 " R1 " BAD".
           ADD 0.1 0.2 GIVING R2.
           IF R2 = 0.3 DISPLAY "C2 " R2 " OK"
           ELSE DISPLAY "C2 " R2 " BAD".
           DIVIDE 10 BY 4 GIVING R3.
           IF R3 = 2.5 DISPLAY "C3 " R3 " OK"
           ELSE DISPLAY "C3 " R3 " BAD".
           SUBTRACT 20 FROM 8 GIVING R4.
           IF R4 = -12 DISPLAY "C4 " R4 " OK"
           ELSE DISPLAY "C4 " R4 " BAD".
           STOP RUN.
