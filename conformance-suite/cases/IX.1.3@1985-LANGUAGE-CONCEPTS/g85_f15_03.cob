      *>--- conformance-case
      *> id: g85_f15_03
      *> rules: [1985-GR-IX.1.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GR4 - permanent error: any specified exception
      *> procedures are executed. Legs: (a) CLOSE WITH LOCK then OPEN
      *> INPUT fails with the permanent-error status 38 and the USE
      *> procedure runs; (b) OPEN INPUT of a non-optional file that is
      *> not present fails with 35 and the USE procedure runs. Each OPEN
      *> is then repeated once. A failed OPEN leaves the connector
      *> closed, which is WiseOwl COBOL's documented corrected state
      *> (docs/conformance/7/DOC-A.1-105), so the second OPEN is a fresh
      *> OPEN that meets the same lock or absence again and runs the USE
      *> procedure again; it does not witness persistence. A leg fails
      *> if the OPEN succeeds, if the status is not the derived 38 or
      *> 35, or if the USE procedure is skipped. NOT WITNESSED: the
      *> third sentence of §1985:IX.1.3.4 GR4 (the condition 'remains in
      *> effect for all subsequent input-output operations'). In indexed
      *> I-O it is reached only through status '30' on an open connector
      *> (indexed files have no '34'), and '30' cannot be raised
      *> deterministically. This case does not cover that clause.
      *> Control continuing after the USE procedure for a critical
      *> status is the implementor's choice (§1985:IX.4.8.4 GR6), and
      *> WiseOwl COBOL continues with the next statement
      *> (docs/conformance/3.5, kb/Work PB5591). Statuses 35 and 38:
      *> §1985:IX.1.3.4 GRL2.4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1503.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO G85F15IA
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS F1-KEY
               FILE STATUS IS WS-STAT1.
           SELECT F2 ASSIGN TO G85F15IN
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS F2-KEY
               FILE STATUS IS WS-STAT2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  F1-REC.
           05 F1-KEY  PIC X(4).
           05 F1-DATA PIC X(6).
       FD  F2
           LABEL RECORDS ARE STANDARD.
       01  F2-REC.
           05 F2-KEY  PIC X(4).
           05 F2-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT1 PIC XX VALUE SPACES.
       01  WS-STAT2 PIC XX VALUE SPACES.
       01  WS-USES1 PIC 9 VALUE 0.
       01  WS-USES2 PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR1-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       ERR1-PARA.
           ADD 1 TO WS-USES1.
           DISPLAY "USE F1 " WS-USES1 " STATUS " WS-STAT1.
       ERR2-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F2.
       ERR2-PARA.
           ADD 1 TO WS-USES2.
           DISPLAY "USE F2 " WS-USES2 " STATUS " WS-STAT2.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           MOVE "ABCDEFGHIJ" TO F1-REC.
           WRITE F1-REC.
           DISPLAY "WRITE F1 " WS-STAT1.
           CLOSE F1 WITH LOCK.
           DISPLAY "CLOSE F1 " WS-STAT1.
           OPEN INPUT F1.
           DISPLAY "OPEN F1 #1 " WS-STAT1 " USES " WS-USES1.
           OPEN INPUT F1.
           DISPLAY "OPEN F1 #2 " WS-STAT1 " USES " WS-USES1.
           OPEN INPUT F2.
           DISPLAY "OPEN F2 #1 " WS-STAT2 " USES " WS-USES2.
           OPEN INPUT F2.
           DISPLAY "OPEN F2 #2 " WS-STAT2 " USES " WS-USES2.
           STOP RUN.
