      *>--- conformance-case
      *> id: pb5122_numeric_test_operands_2002
      *> rules: [SR-8.8.4.4.3-8]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5122 - the NUMERIC test over the three operands COBOL-85 barred. §2023:8.8.4.4.3 SR8 (the 2002
      *> text's SR2 the same): "If the NUMERIC phrase is specified, identifier-1 shall reference a data item whose
      *> usage is display or national or whose category is numeric." An alphabetic item (AL "ABC", not numeric: F),
      *> a group of a signed item and an alphanumeric one (GS, tested as its alphanumeric content: S1 holds 12 with
      *> an embedded sign in its last character, so the content is not all digits: F), and a USAGE COMP numeric
      *> item (CP 123, valid numeric content: T).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5122NT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AL PIC AAA VALUE "ABC".
       01  GS.
           05  S1 PIC S99 VALUE 12.
           05  S2 PIC XX VALUE "12".
       01  CP PIC 999 COMP VALUE 123.
       PROCEDURE DIVISION.
       P1.
           IF AL IS NUMERIC DISPLAY "AL T" ELSE DISPLAY "AL F".
           IF GS IS NUMERIC DISPLAY "GS T" ELSE DISPLAY "GS F".
           IF CP IS NUMERIC DISPLAY "CP T" ELSE DISPLAY "CP F".
           STOP RUN.
