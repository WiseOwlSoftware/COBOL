      *>--- conformance-case
      *> id: 1985-SR-IV_5_2-p1
      *> rules: [1985-SR-IV.5.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.5.2 p1 (A COBOL SOURCE PROGRAM). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.5.2-p1 CONTROL: four divisions in order; a REPLACE
      * statement precedes the Identification Division (the exception)
      * and an end program header follows the Procedure Division.
      * Expect: ORDER OK
           REPLACE ==MSG== BY =="ORDER OK"==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIVOK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WISEOWL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W  PIC X(8) VALUE MSG.
       PROCEDURE DIVISION.
       P0.
           DISPLAY W.
           STOP RUN.
       END PROGRAM DIVOK.
