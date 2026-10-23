*>--- conformance-case
*> id: pb91_native_overflow_outside_arithmetic
*> rules: [GR-14.7.5-2, GR-14.7.5-3, GR-14.7.5-5, GR-14.7.5-L3.2,
*>    DOC-A.1-179]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB91 - a NATIVE (Int128-carrier) intermediate that overflows, or a zero divisor, in a NON-arithmetic context
      *> under >>TURN EC-SIZE checking is the size error condition 14.7.5's no-phrase rule 3 names ("if an arithmetic
      *> operation on an intermediate data item ... would cause the new nonzero value to be farther from zero ... than
      *> is allowed for the intermediate data item"), disposed per 14.6.13.1.3 exactly like PB75's decimal carrier: a
      *> USE declarative runs and RESUME AT NEXT STATEMENT continues after the offending statement (#5); an enclosing
      *> exception-checking PERFORM's WHEN handler runs (#4); an arithmetic statement's own ON SIZE ERROR still takes
      *> precedence (#1). Before PB91 the checked kernels were selected only by the arithmetic statement's phrase, so
      *> `IF A * A > 5` wrapped silently (item 179 (1) documents the wrap for the checking-OFF case only) and a
      *> zero divisor in a condition returned 0.
      *> WHAT STILL LEAVES THE NATIVE INTERMEDIATE: a SUM at the top of the Int128 carrier. S holds
      *> HIGHEST-ALGEBRAIC of PIC S9(19) COMP-5, 2^127 - 1 (15.43.4 r2, DOC-A.1-208), so S + 1 is a value no
      *> Int128 intermediate holds, and every overflowing leg below adds to it (the checked AddChecked kernel).
      *> A PRODUCT never leaves it (kb/Work PB1143, PB3538): A = 10^20 - 1, A * A is about 10^40 and is formed
      *> exactly (the 256-bit lane, the SDIDI past 77 digits), and so is the product of two FUNCTION ABS(A)
      *> values - an operand with no digit bound of its own is bounded by its Int128 carrier - so R7 and R8 are
      *> simply TRUE. (This golden used to make the ABS product overflow through a checked Int128 multiply at
      *> the summed scale, which PB3538 removed: it raised for the SCALE, not the value.)
       >>TURN EC-SIZE-OVERFLOW EC-SIZE-ZERO-DIVIDE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB91NOVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(20) VALUE 99999999999999999999.
       01 S PIC S9(19) COMP-5.
       01 Z PIC 9 VALUE 0.
       01 R PIC 9(5) VALUE 7.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW EC-SIZE-ZERO-DIVIDE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(S) TO S.
           DISPLAY "R1".
           IF S + 1 > 5
               DISPLAY "GT" ELSE DISPLAY "LE" END-IF.
           DISPLAY "R2".
           IF 5 / Z > 1 DISPLAY "GT2" ELSE DISPLAY "LE2" END-IF.
           DISPLAY "R3".
           DISPLAY "V=" FUNCTION ABS(S + 1).
           DISPLAY "R4".
           PERFORM
               IF S + 1 + 1 > 5
                   DISPLAY "GT3" END-IF
           WHEN EC-SIZE-OVERFLOW
               DISPLAY "WHEN=" FUNCTION EXCEPTION-STATUS
               RESUME AT NEXT STATEMENT
           END-PERFORM.
           DISPLAY "R5".
           COMPUTE R = S + 1
               ON SIZE ERROR DISPLAY "PHRASE=" FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "NOSIZE"
           END-COMPUTE.
           DISPLAY "R6 R=" R.
           IF A - 1 > 5 DISPLAY "STILL-FINE" END-IF.
           DISPLAY "R7".
           IF A * A > 5 DISPLAY "EXACT-GT" ELSE DISPLAY "EXACT-LE" END-IF.
           DISPLAY "R8".
           IF FUNCTION ABS(A) * FUNCTION ABS(A) > 5 DISPLAY "UNBOUNDED-GT" ELSE DISPLAY "UNBOUNDED-LE" END-IF.
           STOP RUN.
