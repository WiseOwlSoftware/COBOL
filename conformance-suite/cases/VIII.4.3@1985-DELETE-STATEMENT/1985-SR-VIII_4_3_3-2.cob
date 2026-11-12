      *>--- conformance-case
      *> id: 1985-SR-VIII_4_3_3-2
      *> rules: [1985-SR-VIII.4.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.3.3 SR2 (THE DELETE STATEMENT). A refusal: the
      *> rule forbids line 20 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6826 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELREL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO DISK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  RKEY PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           OPEN I-O RFILE.
           MOVE 1 TO RKEY.
           DELETE RFILE.
           CLOSE RFILE.
           STOP RUN.
