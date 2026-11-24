      *>--- conformance-case
      *> id: pb5677_figurative_digit_to_noninteger_2002
      *> rules: [2002-GR-14.8.24.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5677 - MOVE LOW-VALUE to a noninteger numeric and a
      *> numeric-edited item, where the PROGRAM COLLATING SEQUENCE makes
      *> LOW-VALUE the digit 5 (ALPHABET ALF5 IS "5").
      *> §2002:14.8.24.3 GR4b: a figurative constant sender has "the
      *>   same number of digits as the number of digits in the
      *>   receiving operand and the figurative constant is replicated
      *>   in this item"; "if the receiving item is not an integer, the
      *>   number of digits includes both those to the right and the
      *>   left of the decimal point". The sender is thus the unsigned
      *>   integer of as many 5s as the receiver has digits, aligned by
      *>   decimal point: the fraction digits are zero, the high-order
      *>   integer digits truncate. (§2014:14.9.24.3 GR6d.3.b says the
      *>   same.) EXPECTED, line by line:
      *>  NV PIC 9(3)V99 (5 digits): 55555 is 55555.00, stored 555.00,
      *>   shown through the REDEFINES as 55500 (never 55555).
      *>  NE PIC ZZ9.99 (5 digit positions): 555.00 (never the image
      *>   555555 with its decimal point overwritten).
      *>  NEB PIC $$9.99 (4 digit positions): 5555 edits as $55.00.
      *>  NS PIC S9(3)V99 SIGN TRAILING SEPARATE (5 digits): the sender
      *>   is unsigned, so the sign is positive: 55500+.
      *>  NI PIC 9(3): 555, the integer receiver.
      *>  NP PIC 9(3)V99 COMP-3 and NB PIC 9(3)V99 COMP: 555.00 in
      *>   their own representations.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5677E.
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
           STOP RUN.
