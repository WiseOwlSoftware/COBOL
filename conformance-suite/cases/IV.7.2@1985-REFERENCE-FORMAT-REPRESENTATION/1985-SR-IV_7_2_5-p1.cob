      *>--- conformance-case
      *> id: 1985-SR-IV_7_2_5-p1
      *> rules: [1985-SR-IV.7.2.5-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.2.5 p1 (REFERENCE FORMAT REPRESENTATION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11PTA.
      * 1985 IV.7.2.5: the text words and the separator space of
      * pseudo-text may start in either area A or area B. Below,
      * SHOW-IT (pseudo-text-1) and P2 (pseudo-text-2, a paragraph
      * name, an area-A item once placed: XII.2.4 GR9, XII.3.4 GR10)
      * begin in area A (col 8).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X  PIC X(4) VALUE "OK-1".
       01 W-Y  PIC X(4) VALUE "OK-2".
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE ==
       SHOW-IT== BY ==DISPLAY W-X==
                   ==PARA-TWO== BY ==
       P2. DISPLAY W-Y==.
           SHOW-IT.
       PARA-TWO.
           STOP RUN.
