      *>--- conformance-case
      *> id: 1985-GR-VI_6_19_4-1
      *> rules: [1985-GR-VI.6.19.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.19.4 GR1 (THE MOVE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MOVCORR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC.
           05  A    PIC X(3) VALUE "ABC".
           05  B    PIC 9(3) VALUE 123.
           05  C    PIC X(2) VALUE "ZZ".
       01  DST.
           05  B    PIC 9(5) VALUE 0.
           05  D    PIC X(2) VALUE "QQ".
           05  A    PIC X(3) VALUE SPACES.
       PROCEDURE DIVISION.
       P1.
           MOVE CORRESPONDING SRC TO DST.
           DISPLAY A OF DST.
           DISPLAY B OF DST.
           DISPLAY D OF DST.
           STOP RUN.
