      *>--- conformance-case
      *> id: pb1606-float-item-standard-decimal-below-2014
      *> rules: [GR-8.8.1.5.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.8.1.5.1 / §8.8.1.5.2 rule 1: the conversion of a floating-point
      *> data item into a standard-decimal intermediate data item exists only
      *> when standard-decimal arithmetic is in effect, and STANDARD-DECIMAL is
      *> a COBOL-2014 keyword of the ARITHMETIC clause: below 2014 the clause is
      *> refused with 0900 (and at 85 the OPTIONS paragraph's 0804 fires beside
      *> it). The positive companion is 8.8.1.5.2-Standard-decimal-intermediate-
      *> data-item/pb1606_float_item_conversion (kb/Work PB1606).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1606.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE FLOAT-LONG VALUE 0.1.
       01 D1 PIC V9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE D1 = F1
           DISPLAY D1
           STOP RUN.
