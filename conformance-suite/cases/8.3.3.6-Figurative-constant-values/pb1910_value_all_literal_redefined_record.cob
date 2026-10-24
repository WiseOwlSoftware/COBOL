      *>--- conformance-case
      *> id: pb1910_value_all_literal_redefined_record
      *> rules: [GR-8.3.3.6.4-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1910VA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> ISO 8.3.3.6.4 GR2 - "when this figurative constant is associated
      *> with a data item ... the string of characters is repeated
      *> character by character until the size of the resultant string
      *> is greater than or equal to the number of character positions in
      *> the associated data item" (cite.py --check 8.3.3.6.4 -> 2) OK).
      *> A Format-6 VALUE ALL literal-1 is that rule's string, so
      *> PIC X(6) VALUE ALL "." holds six periods whether or not another
      *> description REDEFINES the record.  The record-struct lane did;
      *> the image-backed lane (a record that is the base of a REDEFINES)
      *> seeded the parse text ALL"." instead.
       01 PLAIN.
          05 P1 PIC 9(2) VALUE 12.
          05 P2 PIC X(6) VALUE ALL ".".
       01 BASE.
          05 B1 PIC 9(2) VALUE 12.
          05 B2 PIC X(6) VALUE ALL ".".
          05 B3 PIC X(4) VALUE ALL "ab".
          05 B4 PIC X(3) VALUE ALL SPACES.
       01 ALIAS REDEFINES BASE.
          05 FILLER PIC X(2).
          05 A2 PIC X(6).
          05 A3 PIC X(4).
          05 A4 PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "PLAIN=[" PLAIN "]"
           DISPLAY "BASE=[" BASE "]"
           DISPLAY "ALIAS-A2=[" A2 "]"
           DISPLAY "ALIAS-A3=[" A3 "]"
           STOP RUN.
