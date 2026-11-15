      *>--- conformance-case
      *> id: pb5484_no_phrase_size_error_continues
      *> rules: [1985-GR-VI.6.4.2-3, 1985-GR-VI.6.4.2-1,
      *>     1985-GR-VI.6.4.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5484. §1985:VI.6.4.2 GR3: "If the ON SIZE ERROR
      *> phrase is not specified and a size error condition exists
      *> after the execution of the arithmetic operations specified by
      *> an arithmetic statement, the values of the affected resultant
      *> identifiers are undefined. The values of resultant
      *> identifiers for which no size error condition exists are the
      *> same as they would have been if the size error condition had
      *> not resulted for any of the resultant identifiers. After
      *> completion of the arithmetic operations, control is
      *> transferred to the end of the arithmetic statement". A zero
      *> divisor (GR2: "Division by zero always terminates the
      *> arithmetic operation and always causes a size error
      *> condition") and an exponentiation-rule violation (GR1) are
      *> size error conditions like any other, and §1985:B.2.2 item
      *> 45 says what follows: "the program will continue and the
      *> value of any affected resultant identifier is undefined".
      *> Its explanation (paragraph 2.2B item 8) is this program's
      *> STATEMENT-1 and STATEMENT-2: DIGIT2 is defined as 10 after
      *> STATEMENT-1, and the program continues after both.
      *>
      *> Only DEFINED values are displayed: an affected resultant's
      *> value is undefined, so it is never shown. Each leg fails if
      *> the size error ends the run unit (every line after it is
      *> missing; before kb/Work PB5484 the zero divisor and the
      *> exponentiation violation ended it, EC-SIZE-...  fatal, as the
      *> 2002+ fatal exception condition does), if control leaves the
      *> statement for anything but its end (the sentence's next
      *> statement, the IF's next statement, the next paragraph), or
      *> if an unaffected resultant is not stored (DIGIT2 = 10).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5484A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DIGIT1      PIC 9  VALUE 9.
       01  DIGIT2      PIC 99 VALUE 9.
       01  VALUE-ZERO  PIC 9  VALUE 0.
       01  NEG4        PIC S9 VALUE -4.
       01  HALF        PIC 9V9 VALUE 0.5.
       01  R1          PIC S99V99 VALUE 0.
       01  R2          PIC 99 VALUE 0.
       01  FLAG        PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       STATEMENT-1.
           ADD +1 TO DIGIT1 DIGIT2.
           DISPLAY "DIGIT2 " DIGIT2.
       STATEMENT-2.
           DIVIDE VALUE-ZERO INTO DIGIT1 DIGIT2.
           DISPLAY "AFTER STATEMENT-2".
       ZERO-BASE.
           COMPUTE R1 = VALUE-ZERO ** 0 DISPLAY "SAME SENTENCE".
       NO-REAL-RESULT.
           IF FLAG = 1
               COMPUTE R1 = NEG4 ** HALF
               DISPLAY "INSIDE THE IF".
       NESTED-QUOTIENT.
           COMPUTE R1 = 1 + 5 / VALUE-ZERO.
           DISPLAY "AFTER NESTED QUOTIENT".
       DIVIDE-REMAINDER.
           DIVIDE 7 BY VALUE-ZERO GIVING R1 REMAINDER R2.
           DISPLAY "AFTER REMAINDER".
       NEXT-STATEMENT.
           MOVE 0 TO R2.
           ADD 3 TO R2.
           DISPLAY "R2 " R2.
           STOP RUN.
