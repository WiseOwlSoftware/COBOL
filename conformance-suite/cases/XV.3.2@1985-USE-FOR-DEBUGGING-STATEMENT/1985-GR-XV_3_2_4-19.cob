      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-19
      *> rules: [1985-GR-XV.3.2.4-19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR19 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the 1985 audit's
      *> drafts pass (a draft that mismatched on an older build, re-run
      *> to MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGSRT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ WITH DEBUGGING MODE.
       OBJECT-COMPUTER. XYZ.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SR-REC.
           05 SR-KEY PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON SRT-IN SRT-OUT.
       DBG-1.
           IF DEBUG-NAME = "SRT-IN" AND DEBUG-CONTENTS = "SORT INPUT"
               DISPLAY "SRT-IN SORT INPUT"
           ELSE
               IF DEBUG-NAME = "SRT-OUT"
                  AND DEBUG-CONTENTS = "SORT OUTPUT"
                   DISPLAY "SRT-OUT SORT OUTPUT"
               ELSE
                   DISPLAY "WRONG".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           SORT SORT-FILE ON ASCENDING KEY SR-KEY
               INPUT PROCEDURE IS SRT-IN
               OUTPUT PROCEDURE IS SRT-OUT.
           DISPLAY "DONE".
           STOP RUN.
       SRT-IN SECTION.
       SRT-IN-1.
           MOVE "A" TO SR-KEY.
           RELEASE SR-REC.
       SRT-OUT SECTION.
       SRT-OUT-1.
           RETURN SORT-FILE AT END DISPLAY "EMPTY".
