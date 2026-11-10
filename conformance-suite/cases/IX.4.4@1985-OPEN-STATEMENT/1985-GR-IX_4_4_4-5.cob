      *>--- conformance-case
      *> id: 1985-GR-IX_4_4_4-5
      *> rules: [1985-GR-IX.4.4.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR5 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXDATA
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05  IX-KEY  PIC X(4).
           05  IX-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IX-FILE.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY.
           MOVE "ALPHA" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE AT END DISPLAY "NO RECORD".
           DISPLAY "INPUT " IX-DATA.
           CLOSE IX-FILE.
           OPEN I-O IX-FILE.
           READ IX-FILE AT END DISPLAY "NO RECORD".
           DISPLAY "I-O " IX-DATA.
           MOVE "OMEGA" TO IX-DATA.
           REWRITE IX-REC INVALID KEY DISPLAY "REWRITE FAILED".
           CLOSE IX-FILE.
           OPEN EXTEND IX-FILE.
           MOVE "K002" TO IX-KEY.
           MOVE "DELTA" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "EXTEND FAILED".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE AT END DISPLAY "NO RECORD".
           DISPLAY "FINAL " IX-DATA.
           READ IX-FILE AT END DISPLAY "NO RECORD".
           DISPLAY "FINAL " IX-DATA.
           OPEN INPUT IX-FILE.
           STOP RUN.
