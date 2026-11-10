      *>--- conformance-case
      *> id: 1985-GR-IX_4_5_4-6
      *> rules: [1985-GR-IX.4.5.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR6 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDSHARE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXDATA
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS A-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  REC-A.
           05  A-KEY      PIC X(4).
           05  A-DATA     PIC X(6).
       01  REC-B.
           05  B-KEY      PIC X(4).
           05  B-DATA     PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO A-KEY.
           MOVE "ALPHA1" TO A-DATA.
           WRITE REC-A INVALID KEY DISPLAY "BADWRITE".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE AT END DISPLAY "EOF".
           DISPLAY B-KEY.
           DISPLAY B-DATA.
           CLOSE IX-FILE.
           STOP RUN.
