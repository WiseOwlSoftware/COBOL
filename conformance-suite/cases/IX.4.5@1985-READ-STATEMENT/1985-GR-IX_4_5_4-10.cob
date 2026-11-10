      *>--- conformance-case
      *> id: 1985-GR-IX_4_5_4-10
      *> rules: [1985-GR-IX.4.5.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR10 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ATEND1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "IXF"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY  PIC X(4).
           05 IX-DATA PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IXF.
       ERR-PARA.
           DISPLAY "USE EXECUTED " WS-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IXF.
           MOVE "0001" TO IX-KEY.
           MOVE "FIRST" TO IX-DATA.
           WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           READ IXF AT END DISPLAY "UNEXPECTED END".
           DISPLAY "READ1 " IX-KEY.
           READ IXF
               AT END DISPLAY "AT END " WS-STAT
               NOT AT END DISPLAY "NOT AT END"
           END-READ.
           DISPLAY "AFTER READ".
           CLOSE IXF.
           OPEN INPUT IXF.
           READ IXF AT END DISPLAY "UNEXPECTED END".
           DISPLAY "READ2 " IX-KEY.
           READ IXF
               NOT AT END DISPLAY "NOT AT END"
           END-READ.
           DISPLAY "AFTER USE".
           CLOSE IXF.
           STOP RUN.
