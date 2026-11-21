      *>--- conformance-case
      *> id: 1985-GR-III_2-p180
      *> rules: [1985-GR-III.2-p180]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p180 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p180: with no PROGRAM COLLATING SEQUENCE clause a
      * nonnumeric comparison uses the native collating sequence. The
      * comparisons agree with the native ordinal positions (ORD),
      * and with an explicit NATIVE alphabet as program sequence
      * (p180_native_collating_explicit.cob prints the same lines).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P180.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C1 PIC X VALUE "1".
       01  C2 PIC X VALUE "A".
       01  C3 PIC X VALUE "a".
       01  C4 PIC X VALUE " ".
       01  OKS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           IF C1 < C2 DISPLAY "1<A" ELSE DISPLAY "1>=A".
           IF C2 < C3 DISPLAY "A<a" ELSE DISPLAY "A>=a".
           IF C4 < C1 DISPLAY "SP<1" ELSE DISPLAY "SP>=1".
           IF C1 < C2 AND FUNCTION ORD (C1) < FUNCTION ORD (C2)
               ADD 1 TO OKS.
           IF C1 NOT < C2 AND FUNCTION ORD (C1) NOT < FUNCTION ORD (C2)
               ADD 1 TO OKS.
           IF C2 < C3 AND FUNCTION ORD (C2) < FUNCTION ORD (C3)
               ADD 1 TO OKS.
           IF C2 NOT < C3 AND FUNCTION ORD (C2) NOT < FUNCTION ORD (C3)
               ADD 1 TO OKS.
           IF OKS = 2
               DISPLAY "P180 OK"
           ELSE
               DISPLAY "P180 FAIL " OKS.
           STOP RUN.
