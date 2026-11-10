      *>--- conformance-case
      *> id: 1985-GR-XI_4_3_4-6
      *> rules: [1985-GR-XI.4.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.3.4 GR6 (THE RETURN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RETINTO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SR-KEY   PIC 9.
           05  SR-DATA  PIC X(3).
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ENT      PIC X(4) OCCURS 3 TIMES.
       01  WS-EOF       PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           MOVE SPACES TO TBL.
           SORT SORT-FILE ON ASCENDING KEY SR-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY ENT (1) ENT (2).
           STOP RUN.
       IN-PROC SECTION.
       IN-PARA.
           MOVE "2ABC" TO SORT-REC.
           RELEASE SORT-REC.
           MOVE "1XYZ" TO SORT-REC.
           RELEASE SORT-REC.
       OUT-PROC SECTION.
       OUT-PARA.
           PERFORM RET-PARA UNTIL WS-EOF = "Y".
           GO TO OUT-EXIT.
       RET-PARA.
           RETURN SORT-FILE INTO ENT (SR-KEY)
               AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               DISPLAY SORT-REC " " ENT (SR-KEY).
       OUT-EXIT.
           EXIT.
