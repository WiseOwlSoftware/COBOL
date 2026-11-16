      *>--- conformance-case
      *> id: g2div02_rounded_each_format
      *> rules: [2002-FMT-14.8.11.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.11.1 - the five DIVIDE formats, each written with
      *> the bare ROUNDED word on a receiver ("DIVIDE INTO identifier-2
      *> ROUNDED", "GIVING identifier-3 ROUNDED REMAINDER identifier-4")
      *> and with ON SIZE ERROR, NOT ON SIZE ERROR and END-DIVIDE.
      *> Native arithmetic (no ARITHMETIC clause). §2002:14.6.3: without
      *> ROUNDED "truncation is relative to the size provided for the
      *> resultant identifier"; with it "the absolute value of the
      *> resultant identifier is increased by one in the low-order
      *> position whenever the most significant digit of the excess is
      *> greater than or equal to five". §2002:14.8.11.3 GR2: "The
      *> dividend is identifier-2 or literal-2. The divisor is
      *> identifier-1 or literal-1."
      *>  F1 (into): DIVIDE 2 INTO A ROUNDED B, A=7 B=9 PIC 99: 3.5
      *>     rounds to 4, 4.5 truncates to 4; no size error, so NOT ON
      *>     SIZE ERROR runs (§2002:14.6.4)       -> F1 OK 04 04
      *>  F2 (into-giving): 10 / 4 = 2.5 GIVING Q ROUNDED QT
      *>                                          -> F2 OK 03 02
      *>  F3 (by-giving): 10 BY 4 GIVING QT Q ROUNDED (ROUNDED binds to
      *>     the receiver it follows)             -> F3 OK 02 03
      *>  F4 (into-remainder): 11 / 4 = 2.75, Q ROUNDED = 3; GR6c the
      *>     subsidiary quotient is "derived from the quotient by
      *>     truncation of digits at the least significant end" = 2, and
      *>     GR7 R = 11 - 2 * 4 = 3               -> F4 OK 03 03
      *>  F5 (by-remainder): 19 / 5 = 3.8, Q ROUNDED = 4, subsidiary 3,
      *>     R = 19 - 3 * 5 = 4                   -> F5 OK 04 04
      *>  F3R: 19 BY 2 GIVING S ROUNDED, S PIC 9 = 9: 9.5 rounds to 10;
      *>     §2002:14.6.4 size error case 3 is judged "after radix point
      *>     alignment and any rounding specified by the ROUNDED
      *>     phrase", and rule 2 leaves S unchanged -> F3R SE 9
      *>  F4R: 19 / 2 GIVING S ROUNDED REMAINDER R, R = 77: the quotient
      *>     store raises the size error, S stays 9, and GR6c develops
      *>     the remainder only "If the size error condition is not
      *>     raised", so R stays 77                -> F4R SE 9 77
      *>  F1Z: DIVIDE 0 INTO S: §2002:14.6.4 case 2 "if the divisor in
      *>     a divide operation or in a DIVIDE statement is zero"; rule 1
      *>     "the values of all of the resultant data items remain
      *>     unchanged"                           -> F1Z SE 9
      *> Why each leg can fail: ROUNDED applied to every receiver or to
      *> none (F1-F3 would print 04 05 / 03 04, 03 03, 03 03), the
      *> remainder taken from the rounded quotient (F4 R = 11 - 12),
      *> the size test made before rounding (F3R NSE 9), or the
      *> remainder stored after a quotient size error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIV02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC 99 VALUE 7.
       01 B  PIC 99 VALUE 9.
       01 D4 PIC 9  VALUE 4.
       01 Q  PIC 99 VALUE 0.
       01 QT PIC 99 VALUE 0.
       01 R  PIC 99 VALUE 0.
       01 S  PIC 9  VALUE 9.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 2 INTO A ROUNDED B
               ON SIZE ERROR DISPLAY "F1 SE"
               NOT ON SIZE ERROR DISPLAY "F1 OK " A " " B
           END-DIVIDE.
           DIVIDE D4 INTO 10 GIVING Q ROUNDED QT
               ON SIZE ERROR DISPLAY "F2 SE"
               NOT ON SIZE ERROR DISPLAY "F2 OK " Q " " QT
           END-DIVIDE.
           DIVIDE 10 BY D4 GIVING QT Q ROUNDED
               NOT ON SIZE ERROR DISPLAY "F3 OK " QT " " Q
           END-DIVIDE.
           DIVIDE D4 INTO 11 GIVING Q ROUNDED REMAINDER R
               ON SIZE ERROR DISPLAY "F4 SE"
               NOT ON SIZE ERROR DISPLAY "F4 OK " Q " " R
           END-DIVIDE.
           DIVIDE 19 BY 5 GIVING Q ROUNDED REMAINDER R
               ON SIZE ERROR DISPLAY "F5 SE"
               NOT ON SIZE ERROR DISPLAY "F5 OK " Q " " R
           END-DIVIDE.
           DIVIDE 19 BY 2 GIVING S ROUNDED
               ON SIZE ERROR DISPLAY "F3R SE " S
               NOT ON SIZE ERROR DISPLAY "F3R NSE " S
           END-DIVIDE.
           MOVE 77 TO R.
           DIVIDE 2 INTO 19 GIVING S ROUNDED REMAINDER R
               ON SIZE ERROR DISPLAY "F4R SE " S " " R
               NOT ON SIZE ERROR DISPLAY "F4R NSE " S " " R
           END-DIVIDE.
           DIVIDE 0 INTO S
               ON SIZE ERROR DISPLAY "F1Z SE " S
               NOT ON SIZE ERROR DISPLAY "F1Z NSE " S
           END-DIVIDE.
           STOP RUN.
       END PROGRAM G2DIV02.
