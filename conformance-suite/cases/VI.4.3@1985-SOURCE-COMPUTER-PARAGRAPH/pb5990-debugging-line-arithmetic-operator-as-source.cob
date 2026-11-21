      *>--- conformance-case
      *> id: pb5990-debugging-line-arithmetic-operator-as-source
      *> rules: [1985-GR-VI.4.3.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: the unit has no WITH DEBUGGING MODE, so the D line
      *> below is a comment line, but the program must also be correct
      *> with it taken as source. As source `A+B` has no space around the
      *> binary operator (COBOL2993), so the program is refused at the
      *> D line: the as-source reading runs the parse-tree screen source
      *> text gets (kb/Work PB5990).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5990F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 1.
       01 X PIC 99.
       PROCEDURE DIVISION.
       P1.
      D    COMPUTE X = A+B.
           STOP RUN.
