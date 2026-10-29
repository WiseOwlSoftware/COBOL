      *>--- conformance-case
      *> id: pb3974_expression_subject_past_carrier_fraction
      *> rules: [GR-14.9.13.4-3, GR-14.9.13.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3974 (branch review round 1, R1-1) - AN ARITHMETIC-EXPRESSION SELECTION
      *> SUBJECT WHOSE DIGIT BOUND PASSES THE 38-DIGIT SCALED CARRIER IS HELD WITH THE
      *> INTEGER DIGITS ITS VALUE NEEDS.  ISO 14.9.13.4 GR3 c): "Any selection subject
      *> specified by arithmetic-expression-1 is assigned a numeric value according to the
      *> rules for evaluating an arithmetic expression", and GR4 a) 6. compares that value
      *> with each selection object.  The bound of a product is the sum of its operands'
      *> integer digits and of their scales, an upper bound and not the value; the hold is
      *> a superset of the S9(21)V9(17) item it replaced on both axes, so the value is
      *> kept whenever that item kept it.
      *>
      *> S1  P * Q, P and Q PIC 9V9(19) holding 3 and 4, is bounded by 2 integer + 38
      *>     fraction digits.  The value is 12: WHEN 2 is false (a hold that cut the
      *>     integer part to keep all 38 fraction digits held 2 and took it), WHEN P * Q
      *>     is true.
      *> S2  CC * DD, both PIC 9(10)V9(15) holding 12345 and 10000, is bounded by 20 + 30
      *>     digits.  The value is 123450000: WHEN 23450000 (the value cut to 8 integer
      *>     digits) is false, WHEN CC * DD is true.
      *> S3  The same P * Q read by a range object: 12 is in 2 THRU P * Q and not in
      *>     13 THRU 20.
      *> S4  AA * BB, both PIC 9(15)V9(12) holding 1.000000000001 and 1, is bounded by
      *>     30 + 24 digits, so the hold is the S9(21)V9(17) item itself: the fraction
      *>     floor is that item's 17 digits (branch review round 2, R2-1, where a floor of
      *>     9 held S9(29)V9(9)).  The value is 1.000000000001: WHEN 1 (the value cut to
      *>     9 fraction digits) is false, WHEN 1.000000000001 is true.
      *> S5  EE * FF, the same 30 + 24 bound holding 0.000000000001 and 0.00001.  The
      *>     value is 0.00000000000000001, its 17th fraction digit the floor's last:
      *>     WHEN 0 (any shorter fraction) is false, WHEN EE * FF is true.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3974PF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P    PIC 9V9(19) VALUE 3.
       01 Q    PIC 9V9(19) VALUE 4.
       01 CC   PIC 9(10)V9(15) VALUE 12345.
       01 DD   PIC 9(10)V9(15) VALUE 10000.
       01 AA   PIC 9(15)V9(12) VALUE 1.000000000001.
       01 BB   PIC 9(15)V9(12) VALUE 1.
       01 EE   PIC 9(15)V9(12) VALUE 0.000000000001.
       01 FF   PIC 9(15)V9(12) VALUE 0.00001.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE P * Q
              WHEN 2
                 DISPLAY "S1=CUT-2"
              WHEN P * Q
                 DISPLAY "S1=EXACT"
              WHEN OTHER
                 DISPLAY "S1=OTHER"
           END-EVALUATE
           EVALUATE CC * DD
              WHEN 23450000
                 DISPLAY "S2=CUT"
              WHEN CC * DD
                 DISPLAY "S2=EXACT"
              WHEN OTHER
                 DISPLAY "S2=OTHER"
           END-EVALUATE
           EVALUATE P * Q
              WHEN 13 THRU 20
                 DISPLAY "S3=HIGH"
              WHEN 2 THRU P * Q
                 DISPLAY "S3=EXACT"
              WHEN OTHER
                 DISPLAY "S3=OTHER"
           END-EVALUATE
           EVALUATE AA * BB
              WHEN 1
                 DISPLAY "S4=CUT-TO-1"
              WHEN 1.000000000001
                 DISPLAY "S4=EXACT"
              WHEN OTHER
                 DISPLAY "S4=OTHER"
           END-EVALUATE
           EVALUATE EE * FF
              WHEN 0
                 DISPLAY "S5=CUT-TO-0"
              WHEN EE * FF
                 DISPLAY "S5=EXACT"
              WHEN OTHER
                 DISPLAY "S5=OTHER"
           END-EVALUATE
           STOP RUN.
