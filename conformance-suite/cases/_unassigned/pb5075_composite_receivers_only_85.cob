      *>--- conformance-case
      *> id: pb5075_composite_receivers_only_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5075 - the X3.23-1985 composite of operands. Each
      *> operand holds at most 18 digits and the composite must not
      *> exceed 18 (§1985:VI.6.4.4 item 2). For MULTIPLY and DIVIDE it
      *> is "the superimposition of all receiving data items of a given
      *> statement" (§1985:VI.6.20.3 SR3; §1985:VI.6.11.3 SR3), so
      *> F18 (PIC V9(18)) times or into R18 (PIC 9(18)) composes the
      *> one receiver R18, 18 digits, although all operands superimpose
      *> to 36. The compiler counted every operand at every edition and
      *> refused these statements (COBOL0805). S3 BY R18 is the shape of
      *> CCVS-85 NC101A. ADD uses every operand (§1985:VI.6.6.3 SR3 a):
      *> A9 (PIC 9(9)) and G9 (PIC 9(9)V9(9)) superimpose to 18, the
      *> boundary.
      *> Expected: R18 = 2 * .5 = 1; 1 / .5 = 2; 2 * 1.5 = 3; 3 + 1 = 4;
      *> G9 = 2.5 + 1 = 3.5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5075OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  F18 PIC V9(18) VALUE 0.5.
       01  R18 PIC 9(18) VALUE 2.
       01  S3  PIC 9(3)V9(3) VALUE 1.5.
       01  A18 PIC 9(18) VALUE 1.
       01  A9  PIC 9(9) VALUE 1.
       01  G9  PIC 9(9)V9(9) VALUE 2.5.
       PROCEDURE DIVISION.
       P1.
           MULTIPLY F18 BY R18.
           DISPLAY R18.
           DIVIDE F18 INTO R18.
           DISPLAY R18.
           MULTIPLY S3 BY R18.
           DISPLAY R18.
           ADD A18 TO R18.
           DISPLAY R18.
           ADD A9 TO G9.
           IF G9 = 3.5 DISPLAY "G9 OK" ELSE DISPLAY "G9 BAD".
           STOP RUN.
