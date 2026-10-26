*>--- conformance-case
*> id: pb3605-initialize-replacing-numeric-by-space
*> rules: [SR-14.9.25.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3605 - ISO 14.9.20.3 SR4 makes the implicit MOVE of INITIALIZE REPLACING ask the whole MOVE
      *> question: "a MOVE statement with identifier-2 or literal-1 as the sending item and an item of the
      *> specified category as the receiving operand shall be valid". MOVE SPACE TO <a numeric item> is valid at
      *> no edition (14.9.25.3 SR5; the 2002 text's own SR5 reads "The figurative constant SPACE shall not be
      *> moved to a numeric or numeric-edited item"), so REPLACING NUMERIC DATA BY SPACE is refused at 85, 2002,
      *> 2014 and 2023, naming SR5, and never as the 2023 removal COBOL0902.
      *>   cite.py --check 14.9.20.3 "For each of the other categories specified in the REPLACING phrase, a MOVE
      *>     statement with identifier-2 or literal-1 as the sending item" -> OK §14.9.20.3 4)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3605I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N PIC 9(3).
          05 A PIC X(3).
       PROCEDURE DIVISION.
           INITIALIZE G REPLACING NUMERIC DATA BY SPACE
           STOP RUN.
