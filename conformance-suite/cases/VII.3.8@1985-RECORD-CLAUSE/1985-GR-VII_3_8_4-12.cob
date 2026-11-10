      *>--- conformance-case
      *> id: 1985-GR-VII_3_8_4-12
      *> rules: [1985-GR-VII.3.8.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.4 GR12 (THE RECORD CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RECINTO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT VFILE ASSIGN TO DISK
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  VFILE
           LABEL RECORDS ARE STANDARD
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON REC-LEN.
       01  VREC                PIC X(10).
       WORKING-STORAGE SECTION.
       01  REC-LEN             PIC 99.
       01  WS-AREA             PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT VFILE.
           MOVE "ABCDEFGHIJ" TO VREC.
           MOVE 3 TO REC-LEN.
           WRITE VREC.
           CLOSE VFILE.
           MOVE ALL "*" TO WS-AREA.
           MOVE 0 TO REC-LEN.
           OPEN INPUT VFILE.
           READ VFILE INTO WS-AREA
               AT END DISPLAY "EOF".
           CLOSE VFILE.
           DISPLAY "[" WS-AREA "]".
           DISPLAY "LEN=" REC-LEN.
           STOP RUN.
