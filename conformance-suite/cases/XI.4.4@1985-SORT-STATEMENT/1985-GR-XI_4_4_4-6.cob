      *>--- conformance-case
      *> id: 1985-GR-XI_4_4_4-6
      *> rules: [1985-GR-XI.4.4.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR6 (THE SORT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTPH.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO INFILE.
           SELECT OUT-FILE ASSIGN TO OUTFILE.
           SELECT SRT-FILE ASSIGN TO SRTWORK.
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE
           LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(3).
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(3).
       SD  SRT-FILE.
       01  SRT-REC.
           05 SRT-KEY PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IN-FILE.
           MOVE "CCC" TO IN-REC.
           WRITE IN-REC.
           MOVE "AAA" TO IN-REC.
           WRITE IN-REC.
           MOVE "BBB" TO IN-REC.
           WRITE IN-REC.
           CLOSE IN-FILE.
           SORT SRT-FILE ON ASCENDING KEY SRT-KEY
               USING IN-FILE GIVING OUT-FILE.
           OPEN INPUT OUT-FILE.
           READ OUT-FILE AT END MOVE "EOF" TO OUT-REC.
           DISPLAY OUT-REC.
           READ OUT-FILE AT END MOVE "EOF" TO OUT-REC.
           DISPLAY OUT-REC.
           READ OUT-FILE AT END MOVE "EOF" TO OUT-REC.
           DISPLAY OUT-REC.
           CLOSE OUT-FILE.
           OPEN INPUT IN-FILE.
           CLOSE IN-FILE.
           DISPLAY "DONE".
           STOP RUN.
