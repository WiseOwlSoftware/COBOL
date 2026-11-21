      *>--- conformance-case
      *> id: pb5924-rel-critical-after-use-85
      *> rules: [1985-GR-VIII.1.3.4-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VIII.1.3.4 (cite.py: OK): "the implementor determines what action is taken after the execution of any applicable USE AFTER STANDARD EXCEPTION procedure"
      *> Relative file: after the USE procedure for a critical status (35, 47),
      *> control returns to the next statement (DOC-A.1-218, GnuCOBOL).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CRITREL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "NOSUCHREL85P11.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RFILE.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN INPUT RFILE.
           DISPLAY "AFTER OPEN " WS-STAT.
           READ RFILE AT END DISPLAY "AT END" END-READ.
           DISPLAY "AFTER READ " WS-STAT.
           STOP RUN.
