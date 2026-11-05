*>--- conformance-case
*> id: pb4635_parenthesized_numeric_arguments
*> rules: [AR-15.37.3-3, AR-15.12.3-1, RV-15.37.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 - ISO §15.37.3 r3: "argument-3 shall be an integer data item or integer literal." (cite.py --check 15.37.3
      *> OK), and §15.12.3 r1 for BASECONVERT's bases. A BARE data item or literal is admitted at those positions; the
      *> same item or literal written in grouping parentheses is "an arithmetic expression enclosed in parentheses"
      *> (§8.8.1.1; §8.8.1.2 r1: the parenthesized result "is treated" as one operand), which is neither, and is refused
      *> (negative/pb4635-findstr-parenthesized-item, pb4635-baseconvert-parenthesized-literal). In a position that takes
      *> a NUMERIC argument (§15.3, §8.4.3.2.3 r8: an argument may be "an arithmetic expression") the parentheses change
      *> nothing, so the parenthesized forms below still compile and evaluate.
      *>
      *> EXPECTED VALUES. FIND-STRING (§15.37.4 r2: argument-3 "represents the number of matches to ignore"): "ABC" occurs
      *> in "ABCABCABC" at 1, 4 and 7, so ignoring I2 = 2 matches (bare item, then bare literal) returns 7. ABS((I2)) = 2,
      *> MOD((7) (I2)) = 7 mod 2 = 1, MAX((I2) (TE(1))) with I2 = 2 and TE(1) = 2 is 2, so the sum is 5. The bare BASECONVERT
      *> of "1A" from base 16 to base 10 is 26 (0x1A).
      *>
      *> THE FUNCTION TYPE SURVIVES THE PARENTHESES (review R1-2). §15.7.1: an Integer argument gives an Integer ABS, and
      *> §15.3 item 6 admits "an arithmetic expression that will always result in an integer value", which (I2) over an
      *> integer item is. So ABS((I2)) is an integer function: FACTORIAL's argument is an integer position that
      *> §8.4.3.2.3 SR11 bars a numeric function from, and FACTORIAL(2) = 2 (0002 in T); and the count of PERFORM ... TIMES
      *> is MAX((I2) 1) = 2, so CNT reaches 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4635PARENNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X9   PIC X(9) VALUE "ABCABCABC".
       01 I2   PIC 9 VALUE 2.
       01 B16  PIC 99 VALUE 16.
       01 TB.
          05 TE PIC 9 OCCURS 3 VALUE 2.
       01 T    PIC 9(4).
       01 CNT  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" I2)
           DISPLAY "ITEM=" T
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" 2)
           DISPLAY "LITERAL=" T
           COMPUTE T = FUNCTION ABS((I2)) + FUNCTION MOD((7) (I2))
               + FUNCTION MAX((I2) (TE(1)))
           DISPLAY "NUMERIC=" T
           DISPLAY "BASES=" FUNCTION BASECONVERT("1A" B16 10)
           COMPUTE T = FUNCTION FACTORIAL(FUNCTION ABS((I2)))
           DISPLAY "INTTYPE=" T
           PERFORM FUNCTION MAX((I2) 1) TIMES
               ADD 1 TO CNT
           END-PERFORM
           DISPLAY "TIMES=" CNT
           STOP RUN.
       END PROGRAM PB4635PARENNUM.
