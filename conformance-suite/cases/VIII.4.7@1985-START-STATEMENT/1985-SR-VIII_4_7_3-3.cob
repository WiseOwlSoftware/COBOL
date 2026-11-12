      *>--- conformance-case
      *> id: 1985-SR-VIII_4_7_3-3
      *> rules: [1985-SR-VIII.4.7.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.7.3 SR3 (THE START STATEMENT). A refusal: the rule
      *> forbids line 20 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6826 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRTCHK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO WS-KEY.
           OPEN INPUT REL-FILE.
           START REL-FILE KEY IS EQUAL TO WS-KEY.
           CLOSE REL-FILE.
           STOP RUN.
