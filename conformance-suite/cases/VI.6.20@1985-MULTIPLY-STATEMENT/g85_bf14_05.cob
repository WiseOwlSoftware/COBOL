      *>--- conformance-case
      *> id: g85_bf14_05
      *> rules: [1985-GR-VI.6.20.4-1, 1985-GR-VI.6.20.4-2, 1985-GR-VI.6.20.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.20.4 GR1 - format 1: identifier-1 is stored in a
      *> temporary that multiplies each identifier-2 left to right. L1
      *> (09 12 15) fails if A is re-read after it becomes 09 (36, 45).
      *> §1985:VI.6.20.4 GR2 - format 2: the product is stored in each
      *> identifier-3. L2 fails if a receiver is skipped; L3 if a numeric
      *> edited receiver is not filled; L4 if V, an operand and a receiver,
      *> is stored before the product reaches X.
      *> §1985:VI.6.20.4 GR3 - the ROUNDED, ON SIZE ERROR, multiple-result
      *> and scope paragraphs apply to MULTIPLY (§1985:VI.6.4.1,
      *> §1985:VI.6.4.2, §1985:VI.6.4.6, scope of statements IV-40).
      *> L5 fails without rounding (4); L6/L7 if the size
      *> error on W is missed, W is changed, or P (no size error) is not
      *> stored; L8 if NOT ON SIZE ERROR is not taken; L9 if END-MULTIPLY
      *> does not end the conditional MULTIPLY inside the IF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1405.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 99 VALUE 3.
       01 B PIC 99 VALUE 4.
       01 C PIC 99 VALUE 5.
       01 X PIC 99 VALUE 0.
       01 Y PIC 99 VALUE 0.
       01 ED PIC ZZ9.
       01 V PIC 9 VALUE 3.
       01 V2 PIC 9 VALUE 3.
       01 P PIC 99 VALUE 5.
       01 W PIC 99 VALUE 50.
       PROCEDURE DIVISION.
       P1.
           MULTIPLY A BY A B C.
           DISPLAY "L1 " A " " B " " C.
           MULTIPLY 6 BY 7 GIVING X Y.
           DISPLAY "L2 " X " " Y.
           MULTIPLY 3 BY 4 GIVING ED.
           DISPLAY "L3 " ED.
           MULTIPLY V BY 3 GIVING V X.
           DISPLAY "L4 " V " " X.
           MULTIPLY 1.5 BY V2 ROUNDED.
           DISPLAY "L5 " V2.
           MULTIPLY 10 BY P W
               ON SIZE ERROR DISPLAY "L6 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L6 NO SIZE ERROR"
           END-MULTIPLY.
           DISPLAY "L7 " P " " W.
           MULTIPLY 2 BY 4 GIVING X
               ON SIZE ERROR DISPLAY "L8 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L8 NO SIZE ERROR"
           END-MULTIPLY.
           IF X = 8
               MULTIPLY 2 BY X
                   ON SIZE ERROR DISPLAY "L9 SIZE ERROR"
               END-MULTIPLY
               DISPLAY "L9 " X
           END-IF.
           STOP RUN.
