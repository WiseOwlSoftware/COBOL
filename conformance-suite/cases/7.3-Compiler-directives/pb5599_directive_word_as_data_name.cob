      *>--- conformance-case
      *> id: pb5599_directive_word_as_data_name
      *> rules: [SR-7.3.3-7]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5599 - a compiler-directive word is reserved within the
      *> context of the directive that specifies it, so outside a
      *> directive it is an ordinary user-defined word: SOURCE-FORMAT and
      *> LISTING are on every edition's clause 8.12 table and are data
      *> names here.
      *>   cite.py: OK  §2002:7.2.2 SR7  "A compiler-directive word is
      *>   reserved within the context of the compiler directive in
      *>   which it is specified"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5599DN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SOURCE-FORMAT PIC 9 VALUE 1.
       01 LISTING PIC 9 VALUE 2.
       PROCEDURE DIVISION.
           DISPLAY SOURCE-FORMAT LISTING.
           STOP RUN.
