      *>--- conformance-case
      *> id: 1985-GR-VI_5_3_4-2
      *> rules: [1985-GR-VI.5.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.3.4 GR2 (THE DATA DESCRIPTION ENTRY). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COND88.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP.
           88  GRP-OK VALUE "AB12".
           05  G-A PIC XX VALUE "AB".
           05  G-B PIC 99 VALUE 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF GRP-OK
               DISPLAY "GROUP TRUE"
           ELSE
               DISPLAY "GROUP FALSE".
           MOVE "XY" TO G-A.
           IF GRP-OK
               DISPLAY "GROUP TRUE"
           ELSE
               DISPLAY "GROUP FALSE".
           STOP RUN.
