      *>--- conformance-case
      *> id: 1985-SR-VII_4_5_3-1
      *> rules: [1985-SR-VII.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.3 SR1 (THE REWRITE STATEMENT). A refusal: the
      *> rule forbids line 17 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6827 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O F1.
           READ F1 AT END STOP RUN.
           REWRITE F1-REC FROM F1-REC.
           CLOSE F1.
           STOP RUN.
