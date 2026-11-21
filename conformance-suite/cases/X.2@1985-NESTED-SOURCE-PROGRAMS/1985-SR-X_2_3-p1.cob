      *>--- conformance-case
      *> id: 1985-SR-X_2_3-p1
      *> rules: [1985-SR-X.2.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.3 p1 (NESTED SOURCE PROGRAMS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-99 g2: X.2.3 p1 at --std 85. A source program presented in
      * the general format's order: identification, environment,
      * data and procedure divisions, a contained source-program and
      * end-program-headers. Expected: INNER RAN then STRUCT OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-FLAG PIC X VALUE "N" IS GLOBAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "INNER".
           IF WS-FLAG = "Y"
               DISPLAY "STRUCT OK"
           ELSE
               DISPLAY "STRUCT FAIL"
           END-IF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       IN-PARA.
           DISPLAY "INNER RAN".
           MOVE "Y" TO WS-FLAG.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM OUTER.
