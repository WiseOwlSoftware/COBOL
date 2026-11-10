      *>--- conformance-case
      *> id: 1985-SR-VIII_2_4_3-5
      *> rules: [1985-SR-VIII.2.4.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.4.3 SR5 (THE ACCESS MODE CLAUSE). A refusal: the
      *> rule forbids line 20 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0862 there; a compiler that accepts it
      *> fails. Placed from written and run by the triage of the drafts
      *> that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELSTART.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO WS-KEY.
           OPEN INPUT REL-FILE.
           START REL-FILE INVALID KEY DISPLAY "NOT FOUND".
           CLOSE REL-FILE.
           STOP RUN.
