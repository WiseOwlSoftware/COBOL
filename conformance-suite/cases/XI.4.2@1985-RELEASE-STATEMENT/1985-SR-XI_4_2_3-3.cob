      *>--- conformance-case
      *> id: 1985-SR-XI_4_2_3-3
      *> rules: [1985-SR-XI.4.2.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.2.3 SR3 (THE RELEASE STATEMENT). A refusal: the rule
      *> forbids line 23 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6827 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-SECT SECTION.
       MAIN-PARA.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-SECT
               OUTPUT PROCEDURE IS OUT-SECT.
           STOP RUN.
       IN-SECT SECTION.
       IN-PARA.
           RELEASE SORT-REC FROM SORT-REC.
       OUT-SECT SECTION.
       OUT-PARA.
           RETURN SORT-FILE AT END CONTINUE.
