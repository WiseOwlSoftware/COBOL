      *>--- conformance-case
      *> id: 1985-GR-IX_4_5_4-12
      *> rules: [1985-GR-IX.4.5.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR12 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXREAD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "IXREAD.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05 IX-KEY   PIC X(4).
           05 IX-DATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-FS       PIC XX VALUE "ZZ".
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IX-FILE.
       IX-ERR-PARA.
           DISPLAY "USE PROCEDURE, STATUS " WS-FS.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "0001FIRST" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W1 INVALID KEY".
           MOVE "0003THIRD" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W3 INVALID KEY".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE NEXT AT END DISPLAY "N1 AT END".
           DISPLAY "READ NEXT " IX-KEY " " WS-FS.
           MOVE "0002" TO IX-KEY.
           READ IX-FILE
               INVALID KEY DISPLAY "READ 0002 INVALID KEY " WS-FS.
           READ IX-FILE NEXT
               AT END DISPLAY "N2 AT END " WS-FS
               NOT AT END DISPLAY "N2 READ " IX-KEY " " WS-FS.
           STOP RUN.
