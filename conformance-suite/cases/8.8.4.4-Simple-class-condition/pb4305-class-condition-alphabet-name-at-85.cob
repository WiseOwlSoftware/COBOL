      *>--- conformance-case
      *> id: pb4305-class-condition-alphabet-name-at-85
      *> rules: [GR-8.8.4.4.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_class_condition_alphabet_name_2002. The class
      *>   condition with an alphabet-name is a 2002 introduction (2002
      *>   Annex F.2 item 16). Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NAN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET MYA IS STANDARD-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           IF S IS MYA DISPLAY "YES" END-IF
           STOP RUN.
