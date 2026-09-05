      *> reject-at: 85 2002 2014 2023
      *> ISO 14.9.51.2 (PDF pages 815/816, RENDERED) gives the WRITE statement exactly
      *> two lock-related brackets - [ retry-phrase ] and [ WITH LOCK | WITH NO LOCK ] -
      *> in BOTH formats. There is no IGNORING LOCK alternative anywhere in WRITE, and
      *> 5.2.1 admits only what the general format prints.
      *> It was accepted until kb/Work PB331 because WRITE and REWRITE shared READ's
      *> merged record-lock rule, which carried READ's IGNORING LOCK with it. The
      *> REWRITE twin is negative/pb331-rewrite-ignoring-lock (14.9.35.2, page 740).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB331WIG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb331wig.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQF.
           MOVE "AAAA" TO SQ-REC.
           WRITE SQ-REC IGNORING LOCK.
           CLOSE SQF.
           STOP RUN.
