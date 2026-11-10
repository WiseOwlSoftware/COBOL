      *>--- conformance-case
      *> id: 1985-FMT-IV_6_3_2_2_1
      *> rules: [1985-FMT-IV.6.3.2.2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.3.2.2.1 (Data Division Structure). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DDSTRUCT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC-A.
           05  FLD-A PIC X(3) VALUE "ONE".
       77  ITEM-B PIC X(3) VALUE "TWO".
       01  REC-C PIC X(5) VALUE "THREE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY FLD-A.
           DISPLAY ITEM-B.
           DISPLAY REC-C.
           STOP RUN.
