      *>--- conformance-case
      *> id: pb5203_add_giving_two_addends
      *> rules: [FMT-14.9.2.2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> ISO 14.9.2.2 Format 2 (and 1985 VI.6.6.2 Format 2) prints
      *> ADD {identifier-1 | literal-1} ... TO {identifier-2 |
      *> literal-2} GIVING, where only the word TO is optional: the
      *> braces make identifier-2 or literal-2 required (14.9.2.2;
      *> 5.2.6.3 "shall be explicitly specified"). So TO may be left
      *> out and the last operand before GIVING is then identifier-2.
      *> Each leg fails if an addend is dropped: A B GIVING D stores
      *> 5 + 7 = 12 (a lost B stores 5); A 1 TO B GIVING D stores
      *> 5 + 1 + 7 = 13; the literal leg 2 TO 3 GIVING D stores 5.
      *> The refused one-addend form is pb5203-add-giving-one-operand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5203A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3) VALUE 5.
       01 B PIC 9(3) VALUE 7.
       01 D PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       P1.
           ADD A B GIVING D.
           DISPLAY D.
           ADD A 1 TO B GIVING D.
           DISPLAY D.
           ADD 2 TO 3 GIVING D.
           DISPLAY D.
           STOP RUN.
