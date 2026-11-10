      *>--- conformance-case
      *> id: 1985-FMT-XI_3_2_2
      *> rules: [1985-FMT-XI.3.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.3.2.2 (THE SORT-MERGE FILE DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SDREC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE
           RECORD CONTAINS 5 CHARACTERS
           DATA RECORD IS SORT-REC.
       01  SORT-REC.
           05 SORT-KEY PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA SECTION.
       M-1.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       I-1.
           MOVE "BBBBB" TO SORT-REC.
           RELEASE SORT-REC.
           MOVE "AAAAA" TO SORT-REC.
           RELEASE SORT-REC.
       OUT-PROC SECTION.
       O-1.
           RETURN SORT-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               DISPLAY SORT-REC
               GO TO O-1.
