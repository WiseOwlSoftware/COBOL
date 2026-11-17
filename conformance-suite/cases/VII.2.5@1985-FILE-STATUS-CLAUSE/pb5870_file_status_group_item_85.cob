      *>--- conformance-case
      *> id: pb5870_file_status_group_item_85
      *> rules: [1985-SR-VII.2.5.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5870, item 1. §1985:VII.2.5.3 SR2: the FILE STATUS
      *> item must be "a two-character data item of the category
      *> alphanumeric"; the 1985 text gives a group item the class
      *> alphanumeric and names no category for it. The determination
      *> D-85-GROUP-CATEGORY (docs/conformance/3/): a group item is of
      *> the category alphanumeric, as GnuCOBOL has it, so FSG, a group
      *> of two PIC 9 items, is a legal FILE STATUS item and the
      *> program compiles: it prints RAN. A compiler that held the
      *> group not alphanumeric would refuse the clause and print
      *> nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5870FSG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5870.dat"
               FILE STATUS IS FSG.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01 FSG.
          02 FSG1 PIC 9.
          02 FSG2 PIC 9.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RAN".
           STOP RUN.
