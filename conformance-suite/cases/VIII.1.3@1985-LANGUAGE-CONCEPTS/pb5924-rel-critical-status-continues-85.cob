      *>--- conformance-case
      *> id: pb5924-rel-critical-status-continues-85
      *> rules: [1985-GR-VIII.1.3.4-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VIII.1.3.4 (cite.py: OK): "any that begin with the digit 3 or 4"
      *> Relative file: 3x and 4x statuses are critical; with a FILE STATUS clause
      *> and no USE procedure the run unit continues (DOC-A.1-103).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CRITRFS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "NOSUCHREL85P11.DAT"
               ORGANIZATION IS RELATIVE
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT RFILE.
           DISPLAY "AFTER OPEN " WS-STAT.
           READ RFILE
               AT END DISPLAY "AT END"
               NOT AT END DISPLAY "NOT AT END"
           END-READ.
           DISPLAY "AFTER READ " WS-STAT.
           STOP RUN.
