      *>--- conformance-case
      *> id: 1985-GR-XI_4_3_4-2
      *> rules: [1985-GR-XI.4.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.3.4 GR2 (THE RETURN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RETTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC X.
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "DONE".
           STOP RUN.
       IN-PROC.
           MOVE "C" TO SORT-KEY.
           RELEASE SORT-REC.
           MOVE "A" TO SORT-KEY.
           RELEASE SORT-REC.
           MOVE "B" TO SORT-KEY.
           RELEASE SORT-REC.
       OUT-PROC.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SORT-FILE
                   AT END
                       MOVE "Y" TO EOF-FLAG
                       DISPLAY "AT END"
                   NOT AT END
                       DISPLAY SORT-KEY
               END-RETURN
           END-PERFORM.
