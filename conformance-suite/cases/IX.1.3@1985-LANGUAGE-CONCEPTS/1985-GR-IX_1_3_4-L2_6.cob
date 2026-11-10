      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_4-L2_6
      *> rules: [1985-GR-IX.1.3.4-L2.6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GRL2.6 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXSTAT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXDISK
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IX-KEY
               FILE STATUS IS IX-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05  IX-KEY   PIC X.
           05  IX-DATA  PIC X(9).
       WORKING-STORAGE SECTION.
       01  IX-STAT      PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "A" TO IX-KEY.
           MOVE "FIRST" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY CONTINUE.
           DISPLAY IX-STAT.
           MOVE "A" TO IX-KEY.
           MOVE "SECOND" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY CONTINUE.
           DISPLAY IX-STAT.
           CLOSE IX-FILE.
           STOP RUN.
