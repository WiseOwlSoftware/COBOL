      *>--- conformance-case
      *> id: pb4305_class_condition_alphabet_name_2002
      *> rules: [GR-8.8.4.4.4-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 8.8.4.4.4 GR3 a): an alphabet-name class
      *>   condition is true when each character of the operand is in
      *>   the coded character set the alphabet names (2002 Annex F.2
      *>   item 16 introduces it). MYA is STANDARD-1; "ABC" is in it.
      *>   Expected: "YES". It fails if the condition is refused or
      *>   answers false.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305AN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET MYA IS STANDARD-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           IF S IS MYA DISPLAY "YES" ELSE DISPLAY "NO" END-IF
           STOP RUN.
