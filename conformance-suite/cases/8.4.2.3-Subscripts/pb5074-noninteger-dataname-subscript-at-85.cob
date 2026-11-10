      *>--- conformance-case
      *> id: pb5074-noninteger-dataname-subscript-at-85
      *> rules: [FMT-8.4.2.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - the edition half of
      *>   pb5074_noninteger_dataname_subscript_2002. 1985 IV.4.3.8.2.3
      *>   SR5: "Data-name-2 may be qualified and must be a numeric
      *>   elementary item representing an integer." PIC 9V9 is no
      *>   integer (1985 III.2). Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074NSD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABC".
           05  E PIC X OCCURS 3.
       01  I PIC 9V9 VALUE 2.0.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY E (I)
           STOP RUN.
