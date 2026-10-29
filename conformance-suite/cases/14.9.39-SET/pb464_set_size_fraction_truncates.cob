*>--- conformance-case
*> id: pb464_set_size_fraction_truncates
*> rules: [GR-14.9.39.4-37]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB464 - SET [SIZE OF] data-name-3 TO arithmetic-expression-5 (ISO 14.9.39 Format 16, COBOL-2023)
      *> with a nonnegative amount that is NOT an integer.
      *> ISO 14.9.39.4 GR37 second sentence: "If arithmetic-expression-5 evaluates to a number that is not an
      *> integer, the result of arithmetic-expression-5 is truncated to the nearest integer." The sentence pairs
      *> "truncated" with "nearest" and so reads two ways: toward zero (2.7 gives 2) or to the closer integer
      *> (2.7 gives 3). DETERMINATION: toward zero. The standard's own vocabulary settles it: "truncation" is a
      *> named rounding mode (14.7.4.3 GR2: no ROUNDED phrase is as if ROUNDED MODE IS TRUNCATION), and the
      *> standard defines that mode with the same idiom ("rounded to the nearest value nearer to zero",
      *> 14.7.4.3 GR10; 11.9.11.2 GR2 e). docs/CONFORMANCE.md section 7 carries the determination.
      *> The fraction is discarded and nothing else happens: no exception condition is set (the amount is
      *> nonnegative, GR37's first sentence, and within the maximum size, GR38), so EXCEPTION-STATUS stays 31 spaces.
      *> The fractional negative case (GR37 first sentence) is 2023/ec_storage_not_avail's, not this one's.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES AND NOT FROM A RUN (each line: amount, length, content):
      *> 2.7 LEN=02 [AB]      toward zero gives 2 (nearest-integer rounding would give 3).
      *> 2.2 LEN=02 [AB]      toward zero gives 2 (both readings agree).
      *> 2.5 LEN=02 [AB]      toward zero gives 2 (a half is where round-half-away would give 3).
      *> 0.9 LEN=00 []        the fraction alone is discarded: length 0, and NO exception (it is nonnegative).
      *> 8.9 LEN=08 [ABCDEFGH] below the maximum size 9 once truncated, so no clamp and no exception.
      *> GROW LEN=04 [AB  ]   4.9 grows "AB" to 4 positions, the added positions spaces (GR39).
      *> EXACT LEN=02 [AB]   2.99999999999999999: the amount is read as the exact decimal it is: truncated it is 2, and
      *>                      the binary64 nearest it (3.0) is not the evaluated value.
      *> 9.5 LEN=09 [ABCDEFGHI] EC[EC-STORAGE-NOT-AVAIL]  GR38 asks "evaluates to a number greater than the
      *>                      maximum size" of the EVALUATED value, as GR37's sign test does (a fractional -0.5
      *>                      raises): 9.5 is greater than 9, so the length is the maximum and the condition is set.
      *>                      It comes last because the condition, once set, stays in EXCEPTION-STATUS.
       >>TURN EC-STORAGE-NOT-AVAIL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB464TRUNC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D    PIC X DYNAMIC LENGTH LIMIT IS 9.
       01 WS-A    PIC 9V9 VALUE 2.7.
       01 WS-B    PIC 9V9 VALUE 2.2.
       01 WS-H    PIC 9V9 VALUE 2.5.
       01 WS-Z    PIC 9V9 VALUE 0.9.
       01 WS-E    PIC 9V9 VALUE 8.9.
       01 WS-G    PIC 9V9 VALUE 4.9.
       01 WS-P    PIC 9V9(17) VALUE 2.99999999999999999.
       01 WS-M    PIC 9V9 VALUE 9.5.
       01 WS-N    PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDEFGHI" TO WS-D.
           SET SIZE OF WS-D TO WS-A.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "2.7 LEN=" WS-N " [" WS-D "] EC[" FUNCTION EXCEPTION-STATUS "]".
           MOVE "ABCDEFGHI" TO WS-D.
           SET SIZE OF WS-D TO WS-B.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "2.2 LEN=" WS-N " [" WS-D "] EC[" FUNCTION EXCEPTION-STATUS "]".
           MOVE "ABCDEFGHI" TO WS-D.
           SET SIZE OF WS-D TO WS-H.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "2.5 LEN=" WS-N " [" WS-D "] EC[" FUNCTION EXCEPTION-STATUS "]".
           MOVE "ABCDEFGHI" TO WS-D.
           SET SIZE OF WS-D TO WS-Z.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "0.9 LEN=" WS-N " [" WS-D "] EC[" FUNCTION EXCEPTION-STATUS "]".
           MOVE "ABCDEFGHI" TO WS-D.
           SET SIZE OF WS-D TO WS-E.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "8.9 LEN=" WS-N " [" WS-D "] EC[" FUNCTION EXCEPTION-STATUS "]".
           MOVE "AB" TO WS-D.
           SET SIZE OF WS-D TO WS-G.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "GROW LEN=" WS-N " [" WS-D "] EC[" FUNCTION EXCEPTION-STATUS "]".
           MOVE "ABCDEFGHI" TO WS-D.
           SET SIZE OF WS-D TO WS-P.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "EXACT LEN=" WS-N " [" WS-D "] EC[" FUNCTION EXCEPTION-STATUS "]".
           MOVE "ABCDEFGHI" TO WS-D.
           SET SIZE OF WS-D TO WS-M.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "9.5 LEN=" WS-N " [" WS-D "] EC[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
