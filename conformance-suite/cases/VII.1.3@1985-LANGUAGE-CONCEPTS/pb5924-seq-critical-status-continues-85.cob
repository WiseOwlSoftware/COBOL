      *>--- conformance-case
      *> id: pb5924-seq-critical-status-continues-85
      *> rules: [1985-GR-VII.1.3.5-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VII.1.3.5 (cite.py: OK): "any that begin with the digit 3 or 4"
      *> 3x and 4x statuses are critical; with a FILE STATUS clause and no USE
      *> procedure the run unit continues (docs/conformance DOC-A.1-103).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CRITFS.
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
       MAIN-PARA.
           OPEN INPUT SFILE.
           DISPLAY "AFTER OPEN " WS-STAT.
           READ SFILE
               AT END DISPLAY "AT END"
               NOT AT END DISPLAY "NOT AT END"
           END-READ.
           DISPLAY "AFTER READ " WS-STAT.
           STOP RUN.
