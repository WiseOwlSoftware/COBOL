      *>--- conformance-case
      *> id: pb5132-multiply-receivers-over-18-digits-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5132 item 1 - the MULTIPLY arm of PB5075 on its own.
      *> §1985:VI.6.4.4 SR2: "The composite of operands, which is a
      *> hypothetical data item resulting from the superimposition of
      *> specified operands in a statement aligned on their decimal
      *> points ... must not contain more than 18 decimal digits." In
      *> MULTIPLY the composite is that of the two receivers
      *> (§1985:VI.6.20.3 SR3): A10 (9(10)) and F9 (V9(9)) superimpose
      *> to 10 + 9 = 19 digits. This program holds no other verb, so
      *> the refusal can come only from the MULTIPLY arm; the
      *> all-verbs program is pb5075-composite-over-18-digits-85.
      *> Expected: COBOL0805 at --std 85 (31 digits is the 2002 cap,
      *> so the program conforms from 2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5132M1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A10 PIC 9(10) VALUE 1.
       01  F9  PIC V9(9) VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MULTIPLY 2 BY A10 F9.
           STOP RUN.
