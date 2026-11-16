      *>--- conformance-case
      *> id: g2lk01-function-formal-call-overflow-returning
      *> rules: [2002-SR-13.6.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.6.2 SR5 - "A formal parameter of a function shall not
      *>   be used as a receiving operand." Reached through the 2002-only
      *>   (archaic) CALL format 1, ON OVERFLOW: §2002:14.8.4.2 SR9
      *>   "Identifier-3 is a receiving operand", so RETURNING P-X stores
      *>   into the function's formal parameter P-X. Expected: refused
      *>   (COBOL2747) at the CALL line. P-R is the returning item, not a
      *>   formal parameter, so MOVE 1 TO P-R is legal. Legal twin with a
      *>   working-storage receiver: g2lk03_function_archaic_receivers_local.
      *> Fails if: the receiving-operand check misses CALL ... RETURNING
      *>   in the ON OVERFLOW format.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2LK01.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P-X     PIC 9(4).
       01  P-R     PIC 9(5).
       PROCEDURE DIVISION USING P-X RETURNING P-R.
           CALL "G2NOSUCH" RETURNING P-X ON OVERFLOW CONTINUE END-CALL.
           MOVE 1 TO P-R.
           GOBACK.
       END FUNCTION G2LK01.
