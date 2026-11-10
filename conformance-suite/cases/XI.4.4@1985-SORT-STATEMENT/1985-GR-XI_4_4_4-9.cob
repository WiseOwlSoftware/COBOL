      *>--- conformance-case
      *> id: 1985-GR-XI_4_4_4-9
      *> rules: [1985-GR-XI.4.4.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR9 (THE SORT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTUSE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO "INFILE".
           SELECT OUT-FILE ASSIGN TO "OUTFILE".
           SELECT SRT-FILE ASSIGN TO "SRTWORK".
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE.
       01  IN-REC PIC X(5).
       FD  OUT-FILE.
       01  OUT-REC PIC X(5).
       SD  SRT-FILE.
       01  SRT-REC.
           05 SRT-KEY PIC X(5).
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
           SORT SRT-FILE ON ASCENDING KEY SRT-KEY
               USING IN-FILE GIVING OUT-FILE.
           OPEN INPUT OUT-FILE.
           READ OUT-FILE AT END DISPLAY "END".
           DISPLAY OUT-REC.
           READ OUT-FILE AT END DISPLAY "END".
           DISPLAY OUT-REC.
           READ OUT-FILE AT END DISPLAY "END".
           DISPLAY OUT-REC.
           READ OUT-FILE AT END DISPLAY "END".
           CLOSE OUT-FILE.
           OPEN INPUT IN-FILE.
           READ IN-FILE AT END DISPLAY "NONE".
           DISPLAY IN-REC.
           CLOSE IN-FILE.
           STOP RUN.
