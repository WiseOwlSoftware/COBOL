      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_1-3
      *> rules: [1985-SR-IV.4.3.8.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.1 SR3 (Uniqueness of Reference). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QUALTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC-A.
           05  GRP-X.
               10  FLD    PIC X(3) VALUE "AAA".
       01  REC-B.
           05  GRP-Y.
               10  FLD    PIC X(3) VALUE "BBB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY FLD OF REC-A.
           DISPLAY FLD IN GRP-Y.
           STOP RUN.
