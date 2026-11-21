      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_1_1-p3
      *> rules: [1985-GR-IV.4.2.2.1.1.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1.1 p3 (User-Defined Words). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.2.2.1.1.1-p3: a condition-name abbreviates the
      * relation "conditional variable = one of its values"; SET
      * condition-name TO TRUE moves its value to the variable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P01COND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 99 VALUE 05.
          88 C-LIST VALUE 03 05 07.
          88 C-NINE VALUE 09.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF C-LIST DISPLAY "A=T" ELSE DISPLAY "A=F".
           IF V = 03 OR V = 05 OR V = 07
               DISPLAY "B=T" ELSE DISPLAY "B=F".
           IF C-NINE DISPLAY "C=T" ELSE DISPLAY "C=F".
           SET C-NINE TO TRUE.
           DISPLAY "V=" V.
           IF C-NINE DISPLAY "D=T" ELSE DISPLAY "D=F".
           SET C-LIST TO TRUE.
           DISPLAY "W=" V.
           STOP RUN.
