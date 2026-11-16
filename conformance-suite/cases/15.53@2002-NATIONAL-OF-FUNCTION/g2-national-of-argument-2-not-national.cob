      *>--- conformance-case
      *> id: g2-national-of-argument-2-not-national
      *> rules: [2002-AR-15.53.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.53.2 AR2 - "Argument-2 shall be of category national and
      *> shall be one character in length." Argument-2 here is an
      *> alphanumeric PIC X item, which the rule forbids: the program must
      *> be REFUSED at the NATIONAL-OF reference (beside the accepted case
      *> g2-national-of-argument-2-national-one-char, which is the witness).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NOF02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AN1     PIC X VALUE "#".
       01  N4      PIC N(4).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION NATIONAL-OF("AB" AN1) TO N4
           STOP RUN.
