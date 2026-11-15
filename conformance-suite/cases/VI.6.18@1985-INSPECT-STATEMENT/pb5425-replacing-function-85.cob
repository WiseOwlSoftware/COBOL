      *>--- conformance-case
      *> id: pb5425-replacing-function-85
      *> rules: [1985-SR-VI.6.18.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5425: the size is static, so the syntax rule asks it.
      *> §1985:VI.6.18.3 SR7: "The size of literal-3 or the data item
      *> referenced by identifier-5 must be equal to the size of
      *> literal-1 or the data item referenced by identifier-3".
      *> UPPER-CASE returns a value the length of argument-1, four:
      *> refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5425R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 PIC X(8) VALUE "ABCDABCD".
       01  W2 PIC X(2) VALUE "yz".
       01  W3 PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       P1.
           INSPECT W1 REPLACING ALL 'AB' BY FUNCTION UPPER-CASE (W3).
           DISPLAY W1 "|".
           STOP RUN.
