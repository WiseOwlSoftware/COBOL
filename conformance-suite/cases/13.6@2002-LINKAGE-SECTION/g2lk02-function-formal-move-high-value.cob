      *>--- conformance-case
      *> id: g2lk02-function-formal-move-high-value
      *> rules: [2002-SR-13.6.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.6.2 SR5 - "A formal parameter of a function shall not
      *>   be used as a receiving operand." Reached through the 2002-only
      *>   (archaic) MOVE of an alphanumeric figurative constant to a
      *>   numeric item, which §2002:14.8.24.2 SR5 leaves legal (only
      *>   SPACE is barred from a numeric receiver); §2002:14.8.24.2 SR4
      *>   "Identifier-2 is a receiving operand", so MOVE HIGH-VALUE TO
      *>   P-X stores into the formal parameter P-X. Expected: refused
      *>   (COBOL2747) at the MOVE line. Legal twin with a working-storage
      *>   receiver: g2lk03_function_archaic_receivers_local.
      *> Fails if: the receiving-operand check misses the figurative-
      *>   constant-to-numeric MOVE form.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2LK02.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P-X     PIC 9(4).
       01  P-R     PIC 9(5).
       PROCEDURE DIVISION USING P-X RETURNING P-R.
           MOVE HIGH-VALUE TO P-X.
           MOVE 1 TO P-R.
           GOBACK.
       END FUNCTION G2LK02.
