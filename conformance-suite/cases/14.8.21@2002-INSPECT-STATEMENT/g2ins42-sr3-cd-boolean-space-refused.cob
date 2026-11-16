      *>--- conformance-case
      *> id: g2ins42-sr3-cd-boolean-space-refused
      *> rules: [2002-SR-14.8.21.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.2 SR3 - "when identifier-1 is of class boolean,
      *> the figurative constant is of class boolean and only the
      *> figurative constant ZERO may be specified". CB is a boolean
      *> record item under a 2002 output CD, so SPACE at the INSPECT
      *> line is refused at 2002 (COBOL1757). The accepted ZERO form on
      *> the same CD item is g2ins41-sr3-cd-figurative-class line 2.
      *> Why it can fail: the compiler accepts SPACE for a boolean CD
      *> item or reports another code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS42.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C1 PIC 99 VALUE 0.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CR.
          05 CB PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT CB TALLYING C1 FOR ALL SPACE.
           DISPLAY "C1=" C1.
           STOP RUN.
       END PROGRAM G2INS42.
