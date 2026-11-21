      *>--- conformance-case
      *> id: 1985-GR-VI_7_2-p3
      *> rules: [1985-GR-VI.7.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.2 p3 (DEBUGGING IN THE NUCLEUS MODULE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       REPLACE ==XMODE== BY ==WITH DEBUGGING MODE==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P03RPL.
      * 1985 VI.7.2 p3: the presence of WITH DEBUGGING MODE is decided
      * after REPLACE: XMODE becomes the clause, so the D line runs.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ XMODE.
       OBJECT-COMPUTER. XYZ.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    DISPLAY "DEBUG LINE COMPILED".
           DISPLAY "END".
           STOP RUN.
