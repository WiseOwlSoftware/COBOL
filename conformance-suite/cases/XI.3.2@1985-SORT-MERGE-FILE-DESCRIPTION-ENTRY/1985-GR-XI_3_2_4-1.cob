      *>--- conformance-case
      *> id: 1985-GR-XI_3_2_4-1
      *> rules: [1985-GR-XI.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.3.2.4 GR1 (THE SORT-MERGE FILE DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SDDATA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE
           DATA RECORD IS SORT-REC.
       01  SORT-REC.
           05 SORT-KEY PIC X(5).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M1.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-SEC
               OUTPUT PROCEDURE IS OUT-SEC.
           STOP RUN.
       IN-SEC SECTION.
       I1.
           MOVE "BBBBB" TO SORT-KEY.
           RELEASE SORT-REC.
           MOVE "AAAAA" TO SORT-KEY.
           RELEASE SORT-REC.
       OUT-SEC SECTION.
       O1.
           RETURN SORT-FILE AT END DISPLAY "EOF".
           DISPLAY SORT-KEY.
           RETURN SORT-FILE AT END DISPLAY "EOF".
           DISPLAY SORT-KEY.
