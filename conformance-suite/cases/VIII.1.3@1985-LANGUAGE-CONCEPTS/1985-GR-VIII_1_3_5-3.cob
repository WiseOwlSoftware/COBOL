      *>--- conformance-case
      *> id: 1985-GR-VIII_1_3_5-3
      *> rules: [1985-GR-VIII.1.3.5-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.5 GR3 (LANGUAGE CONCEPTS.). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELIK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELFIL ASSIGN TO "RELF"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RELFIL
           LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4).
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RELFIL.
       ERR-PARA.
           DISPLAY "USE " FS.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P1.
           OPEN OUTPUT RELFIL.
           MOVE 1 TO RK.
           MOVE "A" TO RREC.
           WRITE RREC.
           CLOSE RELFIL.
           OPEN INPUT RELFIL.
           MOVE 5 TO RK.
           READ RELFIL.
           DISPLAY "AFTER " FS.
           CLOSE RELFIL.
           STOP RUN.
