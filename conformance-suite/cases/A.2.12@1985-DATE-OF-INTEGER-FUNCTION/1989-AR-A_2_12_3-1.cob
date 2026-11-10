      *>--- conformance-case
      *> id: 1989-AR-A_2_12_3-1
      *> rules: [1989-AR-A.2.12.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.12.3 AR1 (THE DATE-OF-INTEGER FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DOI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9(8).
       PROCEDURE DIVISION.
       P1.
           COMPUTE W = FUNCTION DATE-OF-INTEGER (1).
           DISPLAY W.
           COMPUTE W = FUNCTION DATE-OF-INTEGER (366).
           DISPLAY W.
           STOP RUN.
