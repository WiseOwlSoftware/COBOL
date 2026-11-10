      *>--- conformance-case
      *> id: 1985-GR-IX_4_4_4-7
      *> rules: [1985-GR-IX.4.4.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR7 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LABTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY  PIC X(4).
           05 IX-DATA PIC X(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "0001" TO IX-KEY.
           MOVE "LABELS OK" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE AT END MOVE "EOF" TO IX-DATA.
           DISPLAY IX-DATA.
           CLOSE IX-FILE.
           STOP RUN.
