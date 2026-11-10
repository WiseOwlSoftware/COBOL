      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-11
      *> rules: [1985-GR-XV.3.2.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR11 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANYCOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ITEM.
           02  WS-LINE   PIC X(6).
           02  FILLER    PIC X.
           02  WS-NAME   PIC X(30).
           02  FILLER    PIC X.
           02  WS-SUB-1  PIC X(5).
           02  FILLER    PIC X.
           02  WS-SUB-2  PIC X(5).
           02  FILLER    PIC X.
           02  WS-SUB-3  PIC X(5).
           02  FILLER    PIC X.
           02  WS-CONT   PIC X(12).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON P1.
       D1-A.
           MOVE DEBUG-ITEM TO WS-ITEM.
           DISPLAY "[" WS-SUB-1 "][" WS-SUB-2 "][" WS-SUB-3 "]".
       END DECLARATIVES.
       MAIN SECTION.
       M0.
           GO TO P1.
       P1.
           DISPLAY "P1".
           STOP RUN.
