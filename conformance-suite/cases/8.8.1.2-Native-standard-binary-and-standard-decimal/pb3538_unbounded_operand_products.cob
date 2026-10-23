      *>--- conformance-case
      *> id: pb3538_unbounded_operand_products
      *> rules: [GR-8.8.1.2-3, GR-14.7.4.3-2, GR-14.7.5-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538's sibling arms under native arithmetic: a value
      *> only bounded by its carrier (a function result) or by the
      *> magnitude of its operands' PICTUREs, as an operand of a product,
      *> a quotient, a subscript and a DIVIDE REMAINDER back-multiply.
      *> 8.8.1.2 3) evaluates left to right and 14.7.4.3 2) truncates
      *> into the receiver; 14.7.5 5) licenses a size error only for a
      *> VALUE outside the intermediate's range (CONFORMANCE.md
      *> DOC-A.1-123 / DOC-A.1-179), and no value here is near it:
      *>   MEAN3  MEAN(1.5 2) = 1.75, cubed 5.359375, stored 5.3593;
      *>   SUB    TE (8 / 2 / 2 / 2 / 2 * 6 + 1) = TE (4) = 4;
      *>   BIGQ   999999999999999999 / 0.000000000000000003 / 10 ** 18
      *>          = 333333333333333333;
      *>   DREM   8 / 3 = 2 remainder 2 (14.9.12.4 GR6, GR7) with a
      *>          binary quotient receiver whose digits are its container's.
      *> Why each leg can fail: a product of three scale-18 function
      *> values used to be multiplied at scale 54 and end the run unit
      *> with EC-SIZE-OVERFLOW; the subscript's nested quotients
      *> compounded their scale like any other (kb/Work PB3538); a
      *> quotient by a tiny divisor must not lose the digits its
      *> receiver keeps; and the remainder's back-multiply must stay exact.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538UNB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9 VALUE 8.
       01 B    PIC 9 VALUE 2.
       01 T    PIC 9 VALUE 3.
       01 F    PIC 9V9(18) VALUE 1.5.
       01 G    PIC 9V9(18) VALUE 2.
       01 BIG  PIC 9(18) VALUE 999999999999999999.
       01 TINY PIC V9(18) VALUE 0.000000000000000003.
       01 Q5   PIC S9(4) COMP-5 VALUE 0.
       01 R    PIC 9(4)V9(4) VALUE 0.
       01 W    PIC 9(18)V9(4) VALUE 0.
       01 RM   PIC 9(4)V9(4) VALUE 0.
       01 TB.
          05 TE PIC 9 OCCURS 5.
       01 E8   PIC Z(3)9.9(4).
       01 EW   PIC Z(17)9.9(4).
       01 EQ   PIC -(4)9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO TE (1). MOVE 2 TO TE (2). MOVE 3 TO TE (3).
           MOVE 4 TO TE (4). MOVE 5 TO TE (5).
           COMPUTE R = FUNCTION MEAN (F G) * FUNCTION MEAN (F G)
               * FUNCTION MEAN (F G).
           MOVE R TO E8.
           DISPLAY "MEAN3 " E8.
           MOVE TE (A / B / B / B / B * 6 + 1) TO R.
           MOVE R TO E8.
           DISPLAY "SUB " E8.
           COMPUTE W = BIG / TINY / 1000000000000000000 + 0.
           MOVE W TO EW.
           DISPLAY "BIGQ " EW.
           DIVIDE A BY T GIVING Q5 REMAINDER RM.
           MOVE Q5 TO EQ.
           MOVE RM TO E8.
           DISPLAY "DREM " EQ " " E8.
           STOP RUN.
