      *>--- conformance-case
      *> id: pb6256_figurative_zero_numeric_relation
      *> rules: [GR-8.3.3.6.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6256 - a relation against figurative ZERO is the
      *> SAME comparison as the relation against the numeric
      *> literal 0.
      *> §2023:8.3.3.6.4 GR4: "The zero format represents the
      *> numeric value '0', one or more of the boolean character
      *> '0', or one or more of the character '0' in the computer's
      *> runtime coded character set, depending on context."
      *> Opposite a numeric operand the context is numeric, so ZERO
      *> is the numeric value 0.
      *> §2023:8.8.4.2.4: "For operands whose class is numeric, a
      *> comparison is made with respect to the algebraic value of
      *> the operands regardless of the manner in which their usage
      *> is described." and "When native arithmetic is in effect,
      *> comparison proceeds by the rules of native arithmetic."
      *> Each leg can fail on its own arm of the one numeric
      *> comparison: the exact wide product 1E60 (past the Int128
      *> carrier) was a fatal size error against ZERO; the SDIDI
      *> function value 0.5 must keep its fraction; an unsigned
      *> 16-byte COMP-5 holding 2^128-1 was a C# overload
      *> ambiguity; a native BINARY-LONG or FLOAT-LONG operand
      *> takes the native arm.
      *> Expected values: A*A = 1E60 > 0; A*NEG = -1E60 < 0;
      *> NUMVAL("0.5") = 0.5 > 0; U = 0.5 > 0; UBIG = 2^128-1 > 0;
      *> UZ = 0; BL = 0; BLN = -3 < 0; FL = 0; FLP = 0.25 > 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6256ZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(30)  VALUE 100000000000000000000000000000.
       01 NEG  PIC S9(30) VALUE -100000000000000000000000000000.
       01 T    PIC X(3)   VALUE "0.5".
       01 U    PIC 9V9    VALUE 0.5.
       01 BL   BINARY-LONG VALUE 0.
       01 BLN  BINARY-LONG VALUE -3.
       01 FL   FLOAT-LONG VALUE 0.
       01 FLP  FLOAT-LONG VALUE 0.25.
       01 UBIG PIC 9(31) COMP-5.
       01 UZ   PIC 9(31) COMP-5 VALUE 0.
       PROCEDURE DIVISION.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(UBIG) TO UBIG
           IF A * A = ZERO
               DISPLAY "WIDE-EQ-ZERO TRUE"
           ELSE
               DISPLAY "WIDE-EQ-ZERO FALSE"
           END-IF
           IF A * A = 0
               DISPLAY "WIDE-EQ-0 TRUE"
           ELSE
               DISPLAY "WIDE-EQ-0 FALSE"
           END-IF
           IF A * A > ZERO
               DISPLAY "WIDE-GT-ZERO TRUE"
           ELSE
               DISPLAY "WIDE-GT-ZERO FALSE"
           END-IF
           IF ZERO < A * A
               DISPLAY "ZERO-LT-WIDE TRUE"
           ELSE
               DISPLAY "ZERO-LT-WIDE FALSE"
           END-IF
           IF A * NEG < ZERO
               DISPLAY "NEGWIDE-LT-ZERO TRUE"
           ELSE
               DISPLAY "NEGWIDE-LT-ZERO FALSE"
           END-IF
           IF FUNCTION NUMVAL(T) = ZERO
               DISPLAY "NUMVAL-EQ-ZERO TRUE"
           ELSE
               DISPLAY "NUMVAL-EQ-ZERO FALSE"
           END-IF
           IF FUNCTION NUMVAL(T) > ZERO
               DISPLAY "NUMVAL-GT-ZERO TRUE"
           ELSE
               DISPLAY "NUMVAL-GT-ZERO FALSE"
           END-IF
           IF U = ZERO
               DISPLAY "U-EQ-ZERO TRUE"
           ELSE
               DISPLAY "U-EQ-ZERO FALSE"
           END-IF
           IF UBIG = ZERO
               DISPLAY "UWIDE-EQ-ZERO TRUE"
           ELSE
               DISPLAY "UWIDE-EQ-ZERO FALSE"
           END-IF
           IF UBIG = 0
               DISPLAY "UWIDE-EQ-0 TRUE"
           ELSE
               DISPLAY "UWIDE-EQ-0 FALSE"
           END-IF
           IF ZERO < UBIG
               DISPLAY "ZERO-LT-UWIDE TRUE"
           ELSE
               DISPLAY "ZERO-LT-UWIDE FALSE"
           END-IF
           IF UZ = ZERO
               DISPLAY "UZERO-EQ-ZERO TRUE"
           ELSE
               DISPLAY "UZERO-EQ-ZERO FALSE"
           END-IF
           IF BL = ZERO
               DISPLAY "BINLONG-EQ-ZERO TRUE"
           ELSE
               DISPLAY "BINLONG-EQ-ZERO FALSE"
           END-IF
           IF BLN < ZERO
               DISPLAY "BINLONG-LT-ZERO TRUE"
           ELSE
               DISPLAY "BINLONG-LT-ZERO FALSE"
           END-IF
           IF FL = ZERO
               DISPLAY "FLOAT-EQ-ZERO TRUE"
           ELSE
               DISPLAY "FLOAT-EQ-ZERO FALSE"
           END-IF
           IF FLP > ZERO
               DISPLAY "FLOAT-GT-ZERO TRUE"
           ELSE
               DISPLAY "FLOAT-GT-ZERO FALSE"
           END-IF
           EVALUATE A * A
               WHEN ZERO
                   DISPLAY "EVALUATE-WIDE ZERO"
               WHEN OTHER
                   DISPLAY "EVALUATE-WIDE OTHER"
           END-EVALUATE
           STOP RUN.
