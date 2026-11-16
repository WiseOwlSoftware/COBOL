      *>--- conformance-case
      *> id: g2mul03_rounded_each_format
      *> rules: [2002-FMT-14.8.25.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.25.1 - both MULTIPLY formats, each written with the
      *> bare ROUNDED word on a receiver ("MULTIPLY BY identifier-2
      *> ROUNDED", "literal-2 GIVING identifier-3 ROUNDED") and with ON
      *> SIZE ERROR, NOT ON SIZE ERROR and END-MULTIPLY. Native
      *> arithmetic. §2002:14.6.3: without ROUNDED "truncation is
      *> relative to the size provided for the resultant identifier";
      *> with it the value "is increased by one in the low-order
      *> position whenever the most significant digit of the excess is
      *> greater than or equal to five".
      *>  M1 (by): MULTIPLY 1.5 BY A ROUNDED B, A=7 B=9 PIC 99:
      *>     §2002:14.8.25.3 GR1 stores each product in identifier-2:
      *>     10.5 rounds to 11, 13.5 truncates to 13; no size error so
      *>     NOT ON SIZE ERROR runs (§2002:14.6.4) -> M1 OK 11 13
      *>  M2 (by-giving): MULTIPLY 2.5 BY 3 GIVING Q ROUNDED QT: GR2
      *>     stores 7.5 in each identifier-3      -> M2 OK 08 07
      *>  M2R: MULTIPLY 1.9 BY 5 GIVING S ROUNDED, S PIC 9 = 9: 9.5
      *>     rounds to 10; §2002:14.6.4 judges the size error "after
      *>     radix point alignment and any rounding specified by the
      *>     ROUNDED phrase", and leaves S unchanged -> M2R SE 9
      *>  M1S: MULTIPLY 5 BY S: 45 exceeds PIC 9, S unchanged
      *>                                          -> M1S SE 9
      *> Why each leg can fail: ROUNDED applied to every receiver or to
      *> none (M1 11 14 / 10 13, M2 08 08 / 07 07), or the size test
      *> made before rounding (M2R NSE 9).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MUL03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC 99 VALUE 7.
       01 B  PIC 99 VALUE 9.
       01 Q  PIC 99 VALUE 0.
       01 QT PIC 99 VALUE 0.
       01 S  PIC 9  VALUE 9.
       PROCEDURE DIVISION.
       MAIN.
           MULTIPLY 1.5 BY A ROUNDED B
               ON SIZE ERROR DISPLAY "M1 SE"
               NOT ON SIZE ERROR DISPLAY "M1 OK " A " " B
           END-MULTIPLY.
           MULTIPLY 2.5 BY 3 GIVING Q ROUNDED QT
               ON SIZE ERROR DISPLAY "M2 SE"
               NOT ON SIZE ERROR DISPLAY "M2 OK " Q " " QT
           END-MULTIPLY.
           MULTIPLY 1.9 BY 5 GIVING S ROUNDED
               ON SIZE ERROR DISPLAY "M2R SE " S
               NOT ON SIZE ERROR DISPLAY "M2R NSE " S
           END-MULTIPLY.
           MULTIPLY 5 BY S
               ON SIZE ERROR DISPLAY "M1S SE " S
               NOT ON SIZE ERROR DISPLAY "M1S NSE " S
           END-MULTIPLY.
           STOP RUN.
       END PROGRAM G2MUL03.
