      *>--- conformance-case
      *> id: 1985-GR-III_2-p163
      *> rules: [1985-GR-III.2-p163]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p163 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p163: AND and OR connect conditions, NOT negates.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P163.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       01  B PIC 9 VALUE 2.
       01  OKS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           IF A = 1 AND B = 2 ADD 1 TO OKS.
           IF A = 9 OR B = 2 ADD 1 TO OKS.
           IF NOT A = 9 ADD 1 TO OKS.
           IF A = 1 AND B = 9 OR A = 1 ADD 1 TO OKS.
           IF NOT (A = 1 AND B = 2)
               CONTINUE
           ELSE
               ADD 1 TO OKS.
           IF A = 9 AND B = 2 OR A = 9
               MOVE 0 TO OKS.
           IF OKS = 5
               DISPLAY "P163 OK"
           ELSE
               DISPLAY "P163 FAIL " OKS.
           STOP RUN.
