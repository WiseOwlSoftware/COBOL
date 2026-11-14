      *>--- conformance-case
      *> id: pb5425-converting-function-85
      *> rules: [1985-SR-VI.6.18.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5425: the size is static, so the syntax rule asks it.
      *> §1985:VI.6.18.3 SR9: "The size of literal-5 or the data item
      *> referenced by identifier-7 must be equal to the size of
      *> literal-4 or the data item referenced by identifier-6".
      *> UPPER-CASE (W1) is eight positions, literal-4 'AB' two: refused
      *> at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5425R5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 PIC X(8) VALUE "ABCDABCD".
       01  W2 PIC X(2) VALUE "yz".
       01  W3 PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       P1.
           INSPECT W1 CONVERTING 'AB' TO FUNCTION UPPER-CASE (W1).
           DISPLAY W1 "|".
           STOP RUN.
