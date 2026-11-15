      *>--- conformance-case
      *> id: g2mov17_alphabetic_to_numeric
      *> rules: [2002-SR-14.8.24.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR8 - "table 17, Validity of types of MOVE
      *>   statements, specifies the validity of the move". Table 17's
      *>   Alphabetic row has No under Numeric, Numeric-edited: the
      *>   MOVE of the alphabetic SA to the numeric R9 is refused at its
      *>   line. Fails if a No cell is admitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV17.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SA  PIC A(3) VALUE "ABC".
       01 R9  PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE SA TO R9.
           STOP RUN.
