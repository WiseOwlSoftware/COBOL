      *>--- conformance-case
      *> id: pb5186_negative_base_real_root_85
      *> rules: [1985-GR-VI.6.2.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5186. X3.23-1985 VI-6.2.3 GR5c: "If no real number
      *> exists as the result of the evaluation, the size error
      *> condition exists" - so a negative base raised to p/q, in
      *> lowest terms with q odd, is the real number -|b|**(p/q) for
      *> p odd and |b|**(p/q) for p even, and only an even q (no real
      *> root) is the size error. COBOL-2002 replaced the rule with
      *> the integer-exponent rule (its witness is
      *> pb5186_negative_base_integer_exponent_2002).
      *>   (-32) ** 0.2  = (-32) ** (1/5) = -2
      *>   (-32) ** 0.4  = (-32) ** (2/5) =  4
      *>   (-32) ** -0.2 = 1 / (-2)       = -0.5
      *>   (-4)  ** 0.5  : no real result, SIZE ERROR, R unchanged
      *>   ((-32) ** 0.2) * 3              = -6
      *>   (-32) ** 1.0  = -32 (an integer exponent written 1.0)
      *>   MAX (-32 -40) ** 0.6 = (-32) ** (3/5) = -8 (the exact arm)
      *>   IF (-32) ** 0.2 < 0 is true.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5186RR85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NB   PIC S99 VALUE -32.
       01  N40  PIC S99 VALUE -40.
       01  N4   PIC S9 VALUE -4.
       01  EX   PIC S9V9 VALUE 0.4.
       01  EN   PIC S9V9 VALUE -0.2.
       01  R    PIC S99V99 VALUE 77.
       01  RE   PIC -99.99.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R ROUNDED = NB ** 0.2
               ON SIZE ERROR DISPLAY "SIZE ERROR 1"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           COMPUTE R ROUNDED = NB ** EX
               ON SIZE ERROR DISPLAY "SIZE ERROR 2"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           COMPUTE R ROUNDED = NB ** EN
               ON SIZE ERROR DISPLAY "SIZE ERROR 3"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           MOVE 77 TO R.
           COMPUTE R ROUNDED = N4 ** 0.5
               ON SIZE ERROR DISPLAY "SIZE ERROR 4"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           COMPUTE R ROUNDED = (NB ** 0.2) * 3
               ON SIZE ERROR DISPLAY "SIZE ERROR 5"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           COMPUTE R ROUNDED = NB ** 1.0
               ON SIZE ERROR DISPLAY "SIZE ERROR 6"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           COMPUTE R ROUNDED = FUNCTION MAX (NB N40) ** 0.6
               ON SIZE ERROR DISPLAY "SIZE ERROR 7"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           IF NB ** 0.2 < 0 DISPLAY "NEGATIVE".
           STOP RUN.
