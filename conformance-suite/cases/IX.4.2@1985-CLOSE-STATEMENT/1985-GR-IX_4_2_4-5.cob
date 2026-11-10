      *>--- conformance-case
      *> id: 1985-GR-IX_4_2_4-5
      *> rules: [1985-GR-IX.4.2.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.2.4 GR5 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLSTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IDX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05  IDX-KEY     PIC X(4).
           05  IDX-DATA    PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT         PIC XX.
       01  WS-SAVE         PIC X(10) VALUE "0001RECORD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           WRITE IDX-REC FROM WS-SAVE
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IDX-FILE.
           IF WS-STAT = "00"
               DISPLAY "CLOSE OK"
           ELSE
               DISPLAY "CLOSE FAILED".
           DISPLAY WS-SAVE.
           OPEN INPUT IDX-FILE.
           READ IDX-FILE AT END DISPLAY "NO RECORD".
           DISPLAY IDX-KEY " " IDX-DATA.
           CLOSE IDX-FILE.
           STOP RUN.
