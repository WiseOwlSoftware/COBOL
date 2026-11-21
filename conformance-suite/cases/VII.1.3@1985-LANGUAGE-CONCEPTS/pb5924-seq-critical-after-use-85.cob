      *>--- conformance-case
      *> id: pb5924-seq-critical-after-use-85
      *> rules: [1985-GR-VII.1.3.5-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VII.1.3.5 (cite.py: OK): "the implementor determines what action is taken after the execution of any applicable USE AFTER STANDARD EXCEPTION procedure"
      *> After the USE procedure for a critical status (35, 47), control returns
      *> to the statement after the failing one (DOC-A.1-218, GnuCOBOL).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CRITUSE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "NOSUCH85P11.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE
           LABEL RECORDS ARE STANDARD.
       01  SREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SFILE.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN INPUT SFILE.
           DISPLAY "AFTER OPEN " WS-STAT.
           READ SFILE AT END DISPLAY "AT END" END-READ.
           DISPLAY "AFTER READ " WS-STAT.
           STOP RUN.
