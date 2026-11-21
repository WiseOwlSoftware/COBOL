      *>--- conformance-case
      *> id: 1985-GR-X_1_3_8_3-p1
      *> rules: [1985-GR-X.1.3.8.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.3 p1 (Scope of Names). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XOUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T GLOBAL.
          05 E PIC X OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       P-1.
           MOVE "ABC" TO T.
           SET IX TO 1.
           CALL "XIN".
           DISPLAY "OUTER E(IX)=" E (IX).
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XIN.
       PROCEDURE DIVISION.
       I-1.
           SET IX TO 3.
           DISPLAY "INNER E(IX)=" E (IX).
           EXIT PROGRAM.
       END PROGRAM XIN.
       END PROGRAM XOUT.
