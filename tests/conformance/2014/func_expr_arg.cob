      *> P7 Step 12 probe - FUNCTION arguments are real arithmetic expressions (ISO 8.4.3.2 SR8):
      *> compound arguments with * / ** and parentheses (8.8.1; ** folds LEFT per 8.8.1.2 r3), the
      *> 8.7.1/8.3.3.3.2 space-vs-adjacent sign discrimination (MAX(A -4) = two args, MAX(A - 4) = one),
      *> table(ALL) expansion (15.3) beside expression args, and string-channel numeric arguments
      *> (division + numeric-edited de-edit through the ONE expression renderer - the deleted static
      *> channel could render neither).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FNEXPARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3) VALUE 12.
       01 B PIC 9(3) VALUE 4.
       01 C PIC 9(3) VALUE 5.
       01 ARR VALUE "40537".
           02 IND OCCURS 5 TIMES PIC 9.
       01 WS-ED PIC Z9 VALUE 34.
       01 R PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MAX(A / B, C).
           DISPLAY "DIV=" R.
           COMPUTE R = FUNCTION MAX(A * B, (C + 1) / 2, 3 + 4).
           DISPLAY "CMPD=" R.
           COMPUTE R = FUNCTION MAX(2 ** 3 ** 2, 1).
           DISPLAY "POW=" R.
           COMPUTE R = FUNCTION MAX(A -4).
           DISPLAY "ADJ=" R.
           COMPUTE R = FUNCTION MAX(A - 4).
           DISPLAY "SUB=" R.
           COMPUTE R = FUNCTION MAX(-4 7).
           DISPLAY "SGN=" R.
           COMPUTE R = FUNCTION SUM(IND(ALL)) + FUNCTION MIN(A / B, 2).
           DISPLAY "ALL=" R.
           COMPUTE R = FUNCTION ORD(FUNCTION CHAR(66 / 2 + 1)).
           DISPLAY "ORD=" R.
           COMPUTE R = FUNCTION ORD(FUNCTION CHAR(WS-ED)).
           DISPLAY "EDT=" R.
           STOP RUN.
       END PROGRAM FNEXPARG.
