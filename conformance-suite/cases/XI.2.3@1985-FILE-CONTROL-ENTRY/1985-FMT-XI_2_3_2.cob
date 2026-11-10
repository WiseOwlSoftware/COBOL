      *>--- conformance-case
      *> id: 1985-FMT-XI_2_3_2
      *> rules: [1985-FMT-XI.2.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.2.3.2 (THE FILE CONTROL ENTRY). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTSEL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO "SORTWK".
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC X(3).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-SEC
               OUTPUT PROCEDURE IS OUT-SEC.
           STOP RUN.
       IN-SEC SECTION.
       IN-1.
           MOVE "CCC" TO SORT-REC.
           RELEASE SORT-REC.
           MOVE "AAA" TO SORT-REC.
           RELEASE SORT-REC.
           MOVE "BBB" TO SORT-REC.
           RELEASE SORT-REC.
       OUT-SEC SECTION.
       OUT-1.
           RETURN SORT-FILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               DISPLAY SORT-KEY
               GO TO OUT-1.
