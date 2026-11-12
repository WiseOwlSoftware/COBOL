      *>--- conformance-case
      *> id: 1985-SR-VIII_4_9_3-3
      *> rules: [1985-SR-VIII.4.9.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.3 SR3 (THE WRITE STATEMENT). A refusal: the rule
      *> forbids line 18 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6826 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELWR.
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
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "ABC" TO REL-REC.
           WRITE REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
