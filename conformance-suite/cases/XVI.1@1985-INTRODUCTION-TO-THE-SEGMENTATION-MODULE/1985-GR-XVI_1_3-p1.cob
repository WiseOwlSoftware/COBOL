      *>--- conformance-case
      *> id: 1985-GR-XVI_1_3-p1
      *> rules: [1985-GR-XVI.1.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.1.3 p1 (INTRODUCTION TO THE SEGMENTATION MODULE). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p06: segmentation reaches procedures only (1985 XVI.1.3
      * p1). Each explicit entry to independent SECTION 60 resets its
      * ALTERed GO TO (INITIAL each time) but the WORKING-STORAGE item
      * it adds to keeps counting (CNT 1, CNT 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       01  TRIP PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           GO TO SW.
       M-BACK.
           ADD 1 TO TRIP.
           IF TRIP < 2 GO TO SW.
           STOP RUN.
       IND-SEC SECTION 60.
       SW.
           GO TO NORM.
       NORM.
           ADD 1 TO CNT.
           DISPLAY "INITIAL CNT " CNT.
           ALTER SW TO PROCEED TO ALT.
           GO TO M-BACK.
       ALT.
           ADD 1 TO CNT.
           DISPLAY "ALTERED CNT " CNT.
           GO TO M-BACK.
