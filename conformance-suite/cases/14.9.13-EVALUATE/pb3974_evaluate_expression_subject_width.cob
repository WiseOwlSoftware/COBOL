      *>--- conformance-case
      *> id: pb3974_evaluate_expression_subject_width
      *> rules: [GR-14.9.13.4-3, GR-14.9.13.4-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3974 - AN ARITHMETIC-EXPRESSION SELECTION SUBJECT IS HELD AT THE
      *> WIDTH OF ITS VALUE.  ISO 14.9.13.4 GR3 c) - "Any selection subject specified
      *> by arithmetic-expression-1 is assigned a numeric value according to the rules
      *> for evaluating an arithmetic expression" - and GR4 a) 6. compares that value
      *> with each selection object as "selection-subject = selection-object" (a range
      *> object as GR4 a) 5.'s ">= left-part AND <= right-part"), so the value held at
      *> the beginning of the statement must be the number the expression yields, to
      *> every digit.  The implementor's intermediate result item that holds it
      *> (14.9.25.4 GR1's "temp") used to be one fixed S9(21)V9(17) item, which kept 21
      *> of a product's integer digits and 17 of its fraction digits.
      *>
      *> Every value is a product or sum of the items below, so each expected line is
      *> exact arithmetic, never a run:  A = B = C = 9999999999 (10 digits), BIG =
      *> 999999999999999 (15 digits), N1 = -BIG, SM = 0.000000000001 (12 fraction
      *> digits).  Each EVALUATE reads its subject at TWO or more WHENs, so the subject
      *> is held, and its first WHEN is a value the subject is NOT equal to but which
      *> the truncated hold WOULD equal, or a WHEN that discriminates by one unit.
      *>
      *> S1  A * B * C is 30 integer digits.  WHEN 1 is false; WHEN A * B * C is true
      *>     (the held value is the product).  Truncated to 21 digits it was not.
      *> S2  BIG * BIG is 30 integer digits; WHEN BIG * BIG - 1 is false by one unit,
      *>     WHEN BIG * BIG + 0 is true.
      *> S3  N1 * BIG is the negative product - the sign survives the hold.
      *> S4  SM * SM is 1E-24, 24 fraction digits.  WHEN 0 is FALSE (1E-24 is not 0),
      *>     WHEN SM * SM is true.  Held at 17 fraction digits it was 0 and took WHEN 0.
      *> S5  BIG * BIG + BIG * BIG is 31 digits: WHEN 1 false, the same sum true.
      *> S6  Two subjects: A * B * C ALSO SM * SM paired with WHEN A * B * C ALSO
      *>     SM * SM - both held, both true.
      *> S7  A range object reads the subject twice (GR4 a) 5.): A * B * C is NOT in
      *>     0 THRU A * B * C - 1 and IS in A * B * C THRU A * B * C + 1.
      *> S8  BIG * BIG * ONE9 has an UPPER BOUND of 39 digits (15 + 15 + 9), past the 38-digit
      *>     scaled carrier, though its value (ONE9 = 1) is the 30-digit BIG * BIG.  The bound
      *>     is not the value, so the hold keeps the carrier's 38 digits and holds it whole:
      *>     WHEN 1 is false, WHEN BIG * BIG * ONE9 is true.
      *> S9  A NEGATIVE control: A * B * C is not A * B * C - 1 or + 1, so the last
      *>     WHEN OTHER is taken - an exact hold does not make unequal values equal.
      *> S10 SM * SM * BIG * BIG has 24 fraction digits: BIG squared is
      *>     999999999999998000000000000001, times 1E-24 is 999999.999999998000000000000001.
      *>     The fraction is exact (scales add), so the hold keeps all 24 digits and takes
      *>     the carrier's remaining positions as integer digits: WHEN 999999.999999998 is
      *>     false, the same expression true.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3974EV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(10) VALUE 9999999999.
       01 B    PIC 9(10) VALUE 9999999999.
       01 C    PIC 9(10) VALUE 9999999999.
       01 BIG  PIC 9(15) VALUE 999999999999999.
       01 N1   PIC S9(15) VALUE -999999999999999.
       01 SM   PIC V9(12) VALUE 0.000000000001.
       01 ONE9 PIC 9(9) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE A * B * C
              WHEN 1
                 DISPLAY "S1=WHEN-1"
              WHEN A * B * C
                 DISPLAY "S1=EXACT"
              WHEN OTHER
                 DISPLAY "S1=OTHER"
           END-EVALUATE
           EVALUATE BIG * BIG
              WHEN BIG * BIG - 1
                 DISPLAY "S2=MINUS-1"
              WHEN BIG * BIG + 0
                 DISPLAY "S2=EXACT"
              WHEN OTHER
                 DISPLAY "S2=OTHER"
           END-EVALUATE
           EVALUATE N1 * BIG
              WHEN 1
                 DISPLAY "S3=WHEN-1"
              WHEN N1 * BIG
                 DISPLAY "S3=EXACT"
              WHEN OTHER
                 DISPLAY "S3=OTHER"
           END-EVALUATE
           EVALUATE SM * SM
              WHEN 0
                 DISPLAY "S4=WHEN-0"
              WHEN SM * SM
                 DISPLAY "S4=EXACT"
              WHEN OTHER
                 DISPLAY "S4=OTHER"
           END-EVALUATE
           EVALUATE BIG * BIG + BIG * BIG
              WHEN 1
                 DISPLAY "S5=WHEN-1"
              WHEN BIG * BIG + BIG * BIG
                 DISPLAY "S5=EXACT"
              WHEN OTHER
                 DISPLAY "S5=OTHER"
           END-EVALUATE
           EVALUATE A * B * C ALSO SM * SM
              WHEN 1 ALSO ANY
                 DISPLAY "S6=WHEN-1"
              WHEN A * B * C ALSO SM * SM
                 DISPLAY "S6=EXACT"
              WHEN OTHER
                 DISPLAY "S6=OTHER"
           END-EVALUATE
           EVALUATE A * B * C
              WHEN 0 THRU A * B * C - 1
                 DISPLAY "S7=LOW-RANGE"
              WHEN A * B * C THRU A * B * C + 1
                 DISPLAY "S7=EXACT"
              WHEN OTHER
                 DISPLAY "S7=OTHER"
           END-EVALUATE
           EVALUATE BIG * BIG * ONE9
              WHEN 1
                 DISPLAY "S8=WHEN-1"
              WHEN BIG * BIG * ONE9
                 DISPLAY "S8=EXACT"
              WHEN OTHER
                 DISPLAY "S8=OTHER"
           END-EVALUATE
           EVALUATE A * B * C
              WHEN A * B * C - 1
                 DISPLAY "S9=MINUS-1"
              WHEN A * B * C + 1
                 DISPLAY "S9=PLUS-1"
              WHEN OTHER
                 DISPLAY "S9=OTHER"
           END-EVALUATE
           EVALUATE SM * SM * BIG * BIG
              WHEN 999999.999999998
                 DISPLAY "S10=ROUNDED"
              WHEN SM * SM * BIG * BIG
                 DISPLAY "S10=EXACT"
              WHEN OTHER
                 DISPLAY "S10=OTHER"
           END-EVALUATE
           STOP RUN.
