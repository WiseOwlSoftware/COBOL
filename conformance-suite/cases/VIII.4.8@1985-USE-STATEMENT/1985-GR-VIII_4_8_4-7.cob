      *>--- conformance-case
      *> id: 1985-GR-VIII_4_8_4-7
      *> rules: [1985-GR-VIII.4.8.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.8.4 GR7 (THE USE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USEREL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELFIL ASSIGN TO "RELDATA"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RELFIL
           LABEL RECORDS ARE STANDARD.
       01  RF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RF-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RELFIL.
       RF-ERR-PARA.
           DISPLAY "IN USE " WS-FS.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RELFIL.
           CLOSE RELFIL.
           OPEN INPUT RELFIL.
           READ RELFIL.
           DISPLAY "AFTER READ " WS-FS.
           CLOSE RELFIL.
           STOP RUN.
