      *>--- conformance-case
      *> id: g85_f25_04
      *> rules: [1985-GR-IX.4.6.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.4 GR2 - sequential-access REWRITE after a
      *> successful READ replaces the record that READ accessed (K002
      *> becomes DELTA4; K001 and K003 unchanged). Fails if another
      *> record changes or the REWRITE is not 00. A second REWRITE with
      *> no READ between breaks the precondition: §1985:IX.1.3.4 status
      *> 43; USE runs and K002 keeps DELTA4. Fails if 43 is not
      *> reported or ECHO55 is written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2504.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO F2504IX
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY   PIC X(4).
           05 IX-DATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-EOF  PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IX-FILE.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P1.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY. MOVE "ALPHA1" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           MOVE "K002" TO IX-KEY. MOVE "BRAVO2" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           MOVE "K003" TO IX-KEY. MOVE "CHARL3" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           CLOSE IX-FILE.
           OPEN I-O IX-FILE.
           READ IX-FILE AT END DISPLAY "EOF".
           READ IX-FILE AT END DISPLAY "EOF".
           MOVE "DELTA4" TO IX-DATA.
           REWRITE IX-REC INVALID KEY DISPLAY "RW1 IK".
           DISPLAY "RW1 " WS-STAT.
           MOVE "K002" TO IX-KEY.
           MOVE "ECHO55" TO IX-DATA.
           REWRITE IX-REC INVALID KEY DISPLAY "RW2 IK".
           DISPLAY "RW2 " WS-STAT.
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ IX-FILE
                   AT END DISPLAY "EOF"
                          MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "REC " IX-REC
               END-READ
           END-PERFORM.
           CLOSE IX-FILE.
           STOP RUN.
