      *>--- conformance-case
      *> id: pb5677_figurative_digit_to_noninteger_85
      *> rules: [1985-GR-VI.6.19.4-3, 1985-GR-VI.6.19.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5677 - MOVE LOW-VALUE to a noninteger numeric and a
      *> numeric-edited item, where the PROGRAM COLLATING SEQUENCE makes
      *> LOW-VALUE the digit 5 (ALPHABET ALF5 IS "5": "5" is the
      *> character of the lowest ordinal position). The rules, each
      *> checked with cite.py:
      *> - §1985:VI.6.19.4 GR3: "All other figurative constants belong
      *>   to the category alphanumeric" (all but ZERO and SPACE).
      *> - §1985:IV.4.2.2.2.3: the figurative's string "is repeated
      *>   character by character on the right until the size of the
      *>   resultant string is greater than or equal to the number of
      *>   character positions in the associated data item".
      *> - §1985:VI.6.19.4 GR4b.3: "data is moved as if the sending
      *>   operand were described as an unsigned numeric integer", and
      *>   GR4b: "alignment by decimal point and any necessary zero
      *>   filling takes place".
      *> So the sender is the unsigned integer whose digits are as many
      *> 5s as the receiver has character positions, aligned by decimal
      *> point: the fraction digits are zero, the high-order integer
      *> digits truncate. EXPECTED, line by line:
      *>  NV PIC 9(3)V99 (5 positions): 55555 is 55555.00, stored 555.00,
      *>   shown through the REDEFINES as 55500 (never 55555).
      *>  NE PIC ZZ9.99 (6 positions): 555555 edited: 555.00 (never the
      *>   image 555555 with its decimal point overwritten).
      *>  NEB PIC $$9.99 (6 positions): two integer digits: $55.00.
      *>  NS PIC S9(3)V99 SIGN TRAILING SEPARATE (6 positions): the
      *>   sender is unsigned, so the sign is positive (GR4b1): 55500+.
      *>  NI PIC 9(3) (3 positions): 555, the integer receiver.
      *>  NP PIC 9(3)V99 COMP-3 and NB PIC 9(3)V99 COMP: the same value,
      *>   555.00, in their own representations.
      *>  LIT: the nonnumeric literal "55555" into NV and "555555" into
      *>   NE move by the same GR4b3: the same 55500 and 555.00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5677D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. G2HOST
           PROGRAM COLLATING SEQUENCE IS ALF5.
       SPECIAL-NAMES.
           ALPHABET ALF5 IS "5".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NV   PIC 9(3)V99.
       01 X5 REDEFINES NV PIC X(5).
       01 NE   PIC ZZ9.99.
       01 NEB  PIC $$9.99.
       01 NS   PIC S9(3)V99 SIGN TRAILING SEPARATE.
       01 XS REDEFINES NS PIC X(6).
       01 NI   PIC 9(3).
       01 NP   PIC 9(3)V99 COMP-3.
       01 NB   PIC 9(3)V99 COMP.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE LOW-VALUE TO NV NE NEB NS NI NP NB.
           DISPLAY "NV=" X5 " NE=[" NE "] NEB=[" NEB "]".
           DISPLAY "NS=" XS " NI=" NI " NP=" NP " NB=" NB.
           MOVE ZERO TO NV NE.
           MOVE "55555" TO NV.
           MOVE "555555" TO NE.
           DISPLAY "LIT NV=" X5 " NE=[" NE "]".
           STOP RUN.
