      *>--- conformance-case
      *> id: pb5075-composite-over-18-digits-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5075 - §1985:VI.6.4.4 item 2: "The composite of
      *> operands, which is a hypothetical data item resulting from the
      *> superimposition of specified operands in a statement aligned
      *> on their decimal points ... must not contain more than 18
      *> decimal digits." A10 (9(10)) and F9 (V9(9)) superimpose to 19
      *> in ADD and SUBTRACT (§1985:VI.6.6.3 SR3 a, §1985:VI.6.26.3
      *> SR3 a: all operands) and, as the two receivers, in MULTIPLY
      *> and DIVIDE (§1985:VI.6.20.3 SR3, §1985:VI.6.11.3 SR3). The
      *> compiler capped the composite at 31 at every edition, so each
      *> statement below compiled clean at --std 85.
      *> Expected: COBOL0805 at --std 85 (31 digits is the 2002 cap,
      *> so the program conforms from 2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5075N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A10 PIC 9(10) VALUE 1.
       01  F9  PIC V9(9) VALUE 0.
       01  C18 PIC 9(18) VALUE 1.
       01  D2  PIC V99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           ADD A10 TO F9.
           SUBTRACT A10 FROM F9.
           MULTIPLY 2 BY A10 F9.
           DIVIDE 2 INTO C18 D2.
           STOP RUN.
