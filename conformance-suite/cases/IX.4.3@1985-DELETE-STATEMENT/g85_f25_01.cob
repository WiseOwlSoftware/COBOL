      *>--- conformance-case
      *> id: g85_f25_01
      *> rules: [1985-GR-IX.4.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.3.4 GR2 - sequential-access DELETE removes the
      *> record accessed by the immediately preceding successful READ
      *> (K002), and that record, not the one the prime key in the
      *> record area names: the program moves K003 into IX-KEY between
      *> the READ and the DELETE. Fails if K002 survives, K003 is
      *> removed, or DELETE is not 00. A second DELETE with no READ
      *> between breaks GR2's precondition: §1985:IX.1.3.4 status 43
      *> ('the last input-output statement executed for the file prior
      *> to the execution of a DELETE ... was not a successfully
      *> executed READ statement'); USE runs, no record is removed.
      *> Fails if 43 is not reported or a record disappears.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2501.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO F2501IX
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
           MOVE "K003" TO IX-KEY.
           DELETE IX-FILE RECORD.
           DISPLAY "DEL1 " WS-STAT.
           DELETE IX-FILE RECORD.
           DISPLAY "DEL2 " WS-STAT.
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
