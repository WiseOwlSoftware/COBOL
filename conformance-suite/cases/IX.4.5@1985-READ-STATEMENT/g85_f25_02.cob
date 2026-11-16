      *>--- conformance-case
      *> id: g85_f25_02
      *> rules: [1985-GR-IX.4.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR2 - for a file in sequential access mode the
      *> NEXT phrase is optional and has no effect: READ with and
      *> without NEXT, interleaved, walk the file exactly as GR4c/GR4d
      *> select (K001, K002, K003, then the at end condition, status
      *> 10) and NEXT changes neither the record selected nor the at
      *> end outcome. Fails if a NEXT READ skips, repeats or reports at
      *> end early.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2502.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO F2502IX
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
       P1.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY. MOVE "ALPHA1" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           MOVE "K002" TO IX-KEY. MOVE "BRAVO2" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           MOVE "K003" TO IX-KEY. MOVE "CHARL3" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY "N1 " IX-REC.
           READ IX-FILE RECORD AT END DISPLAY "EOF".
           DISPLAY "P2 " IX-REC.
           READ IX-FILE NEXT AT END DISPLAY "EOF".
           DISPLAY "N3 " IX-REC.
           READ IX-FILE NEXT RECORD
               AT END DISPLAY "N4 EOF " WS-STAT
               NOT AT END DISPLAY "N4 " IX-REC
           END-READ.
           CLOSE IX-FILE.
           STOP RUN.
