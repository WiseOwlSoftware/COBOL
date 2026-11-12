      *>--- conformance-case
      *> id: 1985-SR-XI_4_3_3-1
      *> rules: [1985-SR-XI.4.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.3.3 SR1 (THE RETURN STATEMENT). A refusal: the rule
      *> forbids line 25 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6827 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RETSR1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT-FILE ASSIGN TO SORTWK1.
       DATA DIVISION.
       FILE SECTION.
       SD  SRT-FILE.
       01  SRT-REC.
           05 SRT-KEY PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X(5).
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SRT-FILE ON ASCENDING KEY SRT-KEY
               INPUT PROCEDURE IS IN-PARA
               OUTPUT PROCEDURE IS OUT-PARA.
           STOP RUN.
       IN-PARA.
           MOVE "BBBBB" TO SRT-REC.
           RELEASE SRT-REC.
       OUT-PARA.
           RETURN SRT-FILE INTO SRT-REC
               AT END MOVE "Y" TO WS-EOF.
