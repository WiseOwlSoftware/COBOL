      *>--- conformance-case
      *> id: pb5132-divide-receivers-over-18-digits-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5132 item 1 - the DIVIDE arm of PB5075 on its own.
      *> §1985:VI.6.4.4 SR2: "The composite of operands, which is a
      *> hypothetical data item resulting from the superimposition of
      *> specified operands in a statement aligned on their decimal
      *> points ... must not contain more than 18 decimal digits." In
      *> DIVIDE the composite is that of the two receivers
      *> (§1985:VI.6.11.3 SR3): C18 (9(18)) and D2 (V99) superimpose
      *> to 18 + 2 = 20 digits. This program holds no other verb, so
      *> the refusal can come only from the DIVIDE arm; the all-verbs
      *> program is pb5075-composite-over-18-digits-85.
      *> Expected: COBOL0805 at --std 85 (31 digits is the 2002 cap,
      *> so the program conforms from 2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5132D1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C18 PIC 9(18) VALUE 1.
       01  D2  PIC V99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           DIVIDE 2 INTO C18 D2.
           STOP RUN.
