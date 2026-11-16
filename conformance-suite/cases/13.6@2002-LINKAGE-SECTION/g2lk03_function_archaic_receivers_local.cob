      *>--- conformance-case
      *> id: g2lk03_function_archaic_receivers_local
      *> rules: [2002-SR-13.6.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.6.2 SR5 - "A formal parameter of a function shall not
      *>   be used as a receiving operand." POSITIVE HALF for the two
      *>   2002-only statement forms the negatives g2lk01 / g2lk02 use:
      *>   the same CALL ... RETURNING ... ON OVERFLOW and MOVE of an
      *>   alphanumeric figurative constant (ALL "7") TO a numeric item,
      *>   with a WORKING-STORAGE receiver W-N, are
      *>   accepted in a function; the formal P-X is only READ and the
      *>   returning item P-R (not a formal parameter) is stored into.
      *>   This shows the refusals are the SR5 check and not a refusal
      *>   of the statement forms.
      *> DERIVATION: G2NOSUCH cannot be located, so §2002:14.8.4.3 GR3g
      *>   "control is transferred to imperative-statement-1": W-F
      *>   becomes "O". MOVE ALL "7" TO W-N is legal (§2002:14.8.24.2
      *>   SR5 bars only SPACE; table 18: category alphanumeric), and
      *>   every character is a digit, so no EC-DATA-INCOMPATIBLE
      *>   (§2002:14.8.24.3 GR4); W-N is never shown. P-R = P-X + 1
      *>   = 41 + 1 = 42, PIC 9(5) -> 00042; the caller's N stays 0041.
      *> Fails if: either form is refused in a function, the ON OVERFLOW
      *>   branch is not taken (R=00000), or the formal is written.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2LK03F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-N     PIC 9(4).
       01  W-F     PIC X VALUE "N".
       LINKAGE SECTION.
       01  P-X     PIC 9(4).
       01  P-R     PIC 9(5).
       PROCEDURE DIVISION USING P-X RETURNING P-R.
           CALL "G2NOSUCH" RETURNING W-N
               ON OVERFLOW MOVE "O" TO W-F
           END-CALL.
           MOVE ALL "7" TO W-N.
           IF W-F = "O"
               COMPUTE P-R = P-X + 1
           ELSE
               MOVE 0 TO P-R
           END-IF.
           GOBACK.
       END FUNCTION G2LK03F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LK03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2LK03F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N       PIC 9(4) VALUE 41.
       01  R       PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE R = FUNCTION G2LK03F (N).
           DISPLAY "R=" R " N=" N.
           STOP RUN.
       END PROGRAM G2LK03.
