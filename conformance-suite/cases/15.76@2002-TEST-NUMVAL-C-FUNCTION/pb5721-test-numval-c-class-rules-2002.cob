      *>--- conformance-case
      *> id: pb5721-test-numval-c-class-rules-2002
      *> rules: [2002-AR-15.76.2-1, 2002-AR-15.76.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5721 - COBOL-2002 states TEST-NUMVAL-C's own, narrower argument rules. §2002:15.76.2 AR1 (cite.py
      *> --check: OK): "Argument-1 shall be of class alphanumeric or national"; AR2: "Argument-2, if specified, shall be of
      *> the same class as argument-1". The edited categories are class alphanumeric, and AR2 puts no rule on argument-2's
      *> characters, so an edited argument-1 and a currency string holding a digit are legal. From 2014 the function
      *> takes NUMVAL-C's argument rules whole (§2014:15.87.2 AR1 -> §2014:15.63.2 AR1, AR2: category, and no digit in
      *> argument-2): pb5721-test-numval-c-category-rules-2014 is refused there.
      *> NE is " $1.50" (PIC $$9.99 holding 1.5): a conforming NUMVAL-C argument-1, so 0. AE is "12 34": the "3" after the
      *> space that ends the digits is the character in error, position 4. "U1 12.5" with the currency string "U1"
      *> (a literal, then a data item) conforms: 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5721P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AE  PIC XXBXX VALUE "12 34".
       01 NE  PIC $$9.99.
       01 CUR PIC XX VALUE "U1".
       01 T   PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1.5 TO NE
           MOVE FUNCTION TEST-NUMVAL-C(NE) TO T
           DISPLAY "NE " T
           MOVE FUNCTION TEST-NUMVAL-C(AE) TO T
           DISPLAY "AE " T
           MOVE FUNCTION TEST-NUMVAL-C("U1 12.5" "U1") TO T
           DISPLAY "LIT " T
           MOVE FUNCTION TEST-NUMVAL-C("U1 12.5" CUR) TO T
           DISPLAY "ITEM " T
           STOP RUN.
