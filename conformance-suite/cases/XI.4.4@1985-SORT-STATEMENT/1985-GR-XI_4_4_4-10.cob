      *>--- conformance-case
      *> id: 1985-GR-XI_4_4_4-10
      *> rules: [1985-GR-XI.4.4.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR10 (THE SORT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTOUT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC X(3).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA SECTION.
       M-1.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "DONE".
           STOP RUN.
       IN-PROC SECTION.
       I-1.
           MOVE "CCC" TO SORT-REC.
           RELEASE SORT-REC.
           MOVE "AAA" TO SORT-REC.
           RELEASE SORT-REC.
           MOVE "BBB" TO SORT-REC.
           RELEASE SORT-REC.
       OUT-PROC SECTION.
       O-1.
           PERFORM O-READ.
           IF EOF-FLAG = "N"
               DISPLAY SORT-REC
               GO TO O-1.
       HELPERS SECTION.
       O-READ.
           RETURN SORT-FILE AT END MOVE "Y" TO EOF-FLAG.
