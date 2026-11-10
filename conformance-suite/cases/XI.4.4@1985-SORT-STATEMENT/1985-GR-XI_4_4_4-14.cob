      *>--- conformance-case
      *> id: 1985-GR-XI_4_4_4-14
      *> rules: [1985-GR-XI.4.4.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR14 (THE SORT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTSEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC 9.
       WORKING-STORAGE SECTION.
       77  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-CTL SECTION 0.
       MAIN-PARA.
           DISPLAY "START".
       SORT-CTL SECTION 60.
       SORT-PARA.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "DONE".
           STOP RUN.
       IN-PROC SECTION 60.
       IN-PARA.
           MOVE 3 TO SORT-KEY.
           RELEASE SORT-REC.
           MOVE 1 TO SORT-KEY.
           RELEASE SORT-REC.
           MOVE 2 TO SORT-KEY.
           RELEASE SORT-REC.
       OUT-PROC SECTION 10.
       OUT-PARA.
           PERFORM OUT-LOOP UNTIL WS-EOF = "Y".
           GO TO OUT-END.
       OUT-LOOP.
           RETURN SORT-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N" DISPLAY SORT-KEY.
       OUT-END.
           EXIT.
