      *>--- conformance-case
      *> id: 1985-SR-VI_5_11_3-5
      *> rules: [1985-SR-VI.5.11.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.11.3 SR5 (THE RENAMES CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RENQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC-1.
           05  FLD-A PIC X(2) VALUE "AA".
           05  FLD-B PIC X(2) VALUE "BB".
       01  REC-2.
           05  FLD-A PIC X(2) VALUE "CC".
           05  FLD-B PIC X(2) VALUE "DD".
           05  FLD-C PIC X(2) VALUE "EE".
       66  REN-X RENAMES FLD-A OF REC-2 THRU FLD-B OF REC-2.
       PROCEDURE DIVISION.
       P1.
           DISPLAY REN-X.
           STOP RUN.
