      *>--- conformance-case
      *> id: 1985-GR-VIII_2_6_3-1
      *> rules: [1985-GR-VIII.2.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Each WRITE carries INVALID KEY: the file has no USE AFTER
      *> STANDARD EXCEPTION procedure, so §1985:VIII.4.9.3 SR3
      *> requires the phrase (kb/Work PB5076).
      *> §1985:VIII.2.6.3 GR1 (THE I-O-CONTROL PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RERUNREL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELFILE ASSIGN TO "RELDATA"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS WS-KEY.
       I-O-CONTROL.
           RERUN ON SYSRR EVERY 100 RECORDS OF RELFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RELFILE.
       01  REL-REC         PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-KEY          PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RELFILE.
           MOVE "REC 1 OK" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "INVALID KEY".
           CLOSE RELFILE.
           OPEN INPUT RELFILE.
           READ RELFILE
               AT END DISPLAY "NO RECORD".
           DISPLAY REL-REC.
           CLOSE RELFILE.
           DISPLAY "DONE".
           STOP RUN.
