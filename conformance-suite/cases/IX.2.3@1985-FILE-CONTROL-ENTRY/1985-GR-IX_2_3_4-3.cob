      *>--- conformance-case
      *> id: 1985-GR-IX_2_3_4-3
      *> rules: [1985-GR-IX.2.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.4 GR3 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXSEQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO IXDATA
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS OUT-KEY.
           SELECT IN-FILE ASSIGN TO IXDATA
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IN-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE LABEL RECORDS ARE STANDARD.
       01  OUT-REC.
           05  OUT-KEY  PIC X.
           05  OUT-DATA PIC X(5).
       FD  IN-FILE LABEL RECORDS ARE STANDARD.
       01  IN-REC.
           05  IN-KEY   PIC X.
           05  IN-DATA  PIC X(5).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           MOVE "D" TO OUT-KEY.
           MOVE "DELTA" TO OUT-DATA.
           WRITE OUT-REC INVALID KEY DISPLAY "BAD WRITE".
           MOVE "A" TO OUT-KEY.
           MOVE "ALPHA" TO OUT-DATA.
           WRITE OUT-REC INVALID KEY DISPLAY "BAD WRITE".
           MOVE "B" TO OUT-KEY.
           MOVE "BRAVO" TO OUT-DATA.
           WRITE OUT-REC INVALID KEY DISPLAY "BAD WRITE".
           CLOSE OUT-FILE.
           OPEN INPUT IN-FILE.
           PERFORM READ-PARA UNTIL EOF-FLAG = "Y".
           CLOSE IN-FILE.
           STOP RUN.
       READ-PARA.
           READ IN-FILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N" DISPLAY IN-DATA.
