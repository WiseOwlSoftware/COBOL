      *>--- conformance-case
      *> id: pb6018-missing-period-before-numeric-paragraph-85
      *> rules: [1985-SR-IV.7.3.3-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6018. §1985:IV.7.3.3 (cite.py: OK): "Successive
      *> sentences or entries either begin in area B", and a sentence
      *> is continued "by starting subsequent line(s) in area B"
      *> (§1985:IV.7.2.2, cite.py: OK). A paragraph-name may be all
      *> digits ("With the exception of section-names, paragraph-names,
      *> segment-numbers, and level-numbers, all user-defined words
      *> must contain at least one alphabetic character",
      *> §1985:IV.4.2.2.1.1). 100 begins its line in area A and is
      *> followed by the period, so it is the next paragraph-name,
      *> never a second DISPLAY operand: ONE report, at 100 (it drew
      *> COBOL3735 alone: the DISPLAY read 100 as its second operand).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NPNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           DISPLAY "X"
       100.
           DISPLAY A.
           STOP RUN.
