      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_2_1-p1
      *> rules: [1985-GR-IV.6.4.2.2.1-p1]
      *> accept: [85]
      *> files: [P05CPY]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.2.1 p1 (Compiler Directing Statements and
      *> Compiler Directing Sentences). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-07.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P05DIR.
      * 1985 IV.6.4.2.2: the compiler directing verbs COPY, ENTER,
      * REPLACE and USE, each a compiler directing sentence (one
      * statement ended by the separator period).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A  PIC X(8) VALUE "ORIGINAL".
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE ==W-B== BY ==W-A==.
           DISPLAY "R1 " W-B.
           REPLACE OFF.
           COPY P05CPY.
           DISPLAY "DONE".
           STOP RUN.
