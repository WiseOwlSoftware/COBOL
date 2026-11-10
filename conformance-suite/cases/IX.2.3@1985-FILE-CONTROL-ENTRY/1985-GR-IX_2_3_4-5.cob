      *>--- conformance-case
      *> id: 1985-GR-IX_2_3_4-5
      *> rules: [1985-GR-IX.2.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.4 GR5 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ASGTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO "IDXFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS OUT-KEY.
           SELECT IN-FILE ASSIGN TO "IDXFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IN-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC.
           05 OUT-KEY  PIC X(4).
           05 OUT-DATA PIC X(5).
       FD  IN-FILE
           LABEL RECORDS ARE STANDARD.
       01  IN-REC.
           05 IN-KEY   PIC X(4).
           05 IN-DATA  PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           MOVE "A001" TO OUT-KEY.
           MOVE "HELLO" TO OUT-DATA.
           WRITE OUT-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE OUT-FILE.
           OPEN INPUT IN-FILE.
           READ IN-FILE AT END DISPLAY "READ FAILED".
           DISPLAY IN-KEY " " IN-DATA.
           CLOSE IN-FILE.
           STOP RUN.
