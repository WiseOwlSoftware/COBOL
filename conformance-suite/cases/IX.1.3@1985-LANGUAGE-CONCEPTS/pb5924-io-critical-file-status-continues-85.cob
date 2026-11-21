      *>--- conformance-case
      *> id: pb5924-io-critical-file-status-continues-85
      *> rules: [1985-GR-IX.1.3.4-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IX.1.3.4 (cite.py: OK): "the implementor determines what action is taken after the execution of any applicable USE AFTER STANDARD EXCEPTION procedure"
      *> Critical statuses 35 and 42 with no USE procedure but a FILE STATUS clause: after the normal
      *> error processing execution continues (WiseOwl's determination, docs/conformance/7/DOC-A.1-103).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IOCRIT2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "IOCRIT85.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF.
       01  IX-REC.
           05  IX-KEY   PIC X(4).
           05  IX-DATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT      PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IXF.
           DISPLAY "AFTER-OPEN " WS-STAT.
           CLOSE IXF.
           DISPLAY "AFTER-CLOSE " WS-STAT.
           DISPLAY "END".
           STOP RUN.
