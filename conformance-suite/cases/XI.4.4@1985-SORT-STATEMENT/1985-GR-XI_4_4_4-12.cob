      *>--- conformance-case
      *> id: 1985-GR-XI_4_4_4-12
      *> rules: [1985-GR-XI.4.4.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR12 (THE SORT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTGIV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO INFILE.
           SELECT OUT-FILE ASSIGN TO OUTFILE.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE.
       01  IN-REC PIC X(5).
       FD  OUT-FILE.
       01  OUT-REC PIC X(5).
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IN-FILE.
           MOVE "CHARL" TO IN-REC.
           WRITE IN-REC.
           MOVE "ALPHA" TO IN-REC.
           WRITE IN-REC.
           MOVE "BRAVO" TO IN-REC.
           WRITE IN-REC.
           CLOSE IN-FILE.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               USING IN-FILE GIVING OUT-FILE.
           OPEN INPUT OUT-FILE.
           READ OUT-FILE AT END DISPLAY "EARLY END".
           DISPLAY OUT-REC.
           READ OUT-FILE AT END DISPLAY "EARLY END".
           DISPLAY OUT-REC.
           READ OUT-FILE AT END DISPLAY "EARLY END".
           DISPLAY OUT-REC.
           READ OUT-FILE AT END DISPLAY "END OF FILE".
           CLOSE OUT-FILE.
           STOP RUN.
