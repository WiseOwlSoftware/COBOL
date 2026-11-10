      *>--- conformance-case
      *> id: 1985-FMT-VIII_4_8_2
      *> rules: [1985-FMT-VIII.4.8.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.8.2 (THE USE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USETST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO RELF
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RFILE.
       ERR-PARA.
           DISPLAY "USE " FS.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           MOVE "ABC" TO RREC.
           WRITE RREC.
           CLOSE RFILE.
           OPEN INPUT RFILE.
           READ RFILE.
           DISPLAY "READ " RREC.
           READ RFILE.
           DISPLAY "AFTER " FS.
           CLOSE RFILE.
           STOP RUN.
