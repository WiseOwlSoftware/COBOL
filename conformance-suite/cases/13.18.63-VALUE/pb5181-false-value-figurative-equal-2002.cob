      *>--- conformance-case
      *> id: pb5181-false-value-figurative-equal-2002
      *> rules: [SR-13.18.63.3-27]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5181 - the sibling of the THROUGH screen, through the same
      *> value reader. §2023:13.18.63.3 SR27 (cite.py --check OK): "The value of
      *> literal-4 shall not be equal to the value of any occurrence of
      *> literal-2." A figurative constant has a value (the fill character of
      *> its kind, repeated to the associated operand's size, §2023:8.3.3.6.4
      *> GR2), so WHEN SET TO FALSE IS " " beside VALUE SPACE names a value for
      *> which the condition-name is TRUE, and "M" falls inside LOW-VALUE THRU
      *> HIGH-VALUE: both refused from 2002, where the phrase exists.
      *> Fails if: the program compiles at 2002, 2014 or 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5181FV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X VALUE "M".
           88 C1 VALUE SPACE WHEN SET TO FALSE IS " ".
           88 C2 VALUE LOW-VALUE THRU HIGH-VALUE
               WHEN SET TO FALSE IS "M".
           88 C3 VALUE SPACE WHEN SET TO FALSE IS "Q".
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
