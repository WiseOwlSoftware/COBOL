      *>--- conformance-case
      *> id: pb5425-characters-function-85
      *> rules: [1985-SR-VI.6.18.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5425: the size is static, so the syntax rule asks it.
      *> §1985:VI.6.18.3 SR8: "When the CHARACTERS phrase is used,
      *> literal-3 or the size of the data item referenced by
      *> identifier-5 must be one character in length". UPPER-CASE (W2)
      *> is two positions, not one: refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5425R3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 PIC X(8) VALUE "ABCDABCD".
       01  W2 PIC X(2) VALUE "yz".
       01  W3 PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       P1.
           INSPECT W1 REPLACING CHARACTERS BY FUNCTION UPPER-CASE (W2).
           DISPLAY W1 "|".
           STOP RUN.
