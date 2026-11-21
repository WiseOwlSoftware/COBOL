      *>--- conformance-case
      *> id: 1985-GR-X_4_1-p1
      *> rules: [1985-GR-X.4.1-p1, 1985-GR-X.4.1-p2, 1985-SR-X.4.1.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.1 p1; §1985:X.4.1 p2; §1985:X.4.1.1 p1 (DATA DIVISION
      *> IN THE INTER-PROGRAM COMMUNICATION MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X41A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 W-N PIC 9(3) VALUE 123.
       77 W-S PIC XX VALUE "AB".
       PROCEDURE DIVISION.
       P-1.
           CALL "X41B" USING W-N W-S.
           DISPLAY "CALLER W-N=" W-N.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X41B.
       DATA DIVISION.
       LINKAGE SECTION.
       77 L-N PIC 9(3).
       77 L-S PIC XX.
       PROCEDURE DIVISION USING L-N L-S.
       P-1.
           DISPLAY "CALLEE L-N=" L-N " L-S=" L-S.
           ADD 1 TO L-N.
           EXIT PROGRAM.
       END PROGRAM X41B.
       END PROGRAM X41A.
