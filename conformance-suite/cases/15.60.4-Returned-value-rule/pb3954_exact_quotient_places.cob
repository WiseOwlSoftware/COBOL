*>--- conformance-case
*> id: pb3954_exact_quotient_places
*> rules: [RV-15.60.4-1, RV-15.84.4-2]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3954 - AN EXACT QUOTIENT OR ROOT IS HELD AT THE PLACES THE VALUE HAS, NOT PADDED TO 34 DIGITS.
      *> 8.8.1.5.3: "Addition, subtraction, multiplication and division are performed as specified in ISO/IEC 60559:2020,
      *> 5.4.1", whose exact result takes the representation nearest the PREFERRED exponent (the dividend's less the
      *> divisor's; for the square root half the operand's, rounded down) and only an INEXACT result fills the 34 digits
      *> of the standard-decimal intermediate data item. 15.84.4 r2 gives SQRT the same definition. The compiler's
      *> division pre-scaled every quotient to 34-36 digits, so an EXACT one such as (1 + 2 + 3) / 3 was held as
      *> 2.000...0 at 33 places, and everything that reads the places a value is held at read 33 where the value has 0.
      *> The native quotient is the one such reader: the working scale of a quotient NESTED over an operand follows the
      *> operand's places (docs/NUMERIC_DESIGN.md, the written precision), so `FUNCTION MEAN (1 2 3) / 3 * 3` truncated its
      *> inner quotient at a finer scale than the literal spelling of the same value, `2 / 3 * 3`, and stored 2.00000000
      *> where the literal stores 1.99999999.
      *> WiseOwl COBOL's determination (owner: one reference, one value; PB3860) is that a function's returned value acts as
      *> the number it is, wherever it came from: MEAN of (1 2 3) IS 2, MEAN of (1 2) IS 1.5, MEAN of (1 1 4) IS 2. Every
      *> leg therefore compares the function spelling against its literal control and must find them equal (SAME), and
      *> prints both stores. The controls pin the numbers: the nested native quotient truncates at the working scale
      *> (PB3538), so 2 / 3 * 3 is 1.99999999 into 8 places and 1.5 / 7 * 7 is 1.49999999.
      *> A: MEAN (1 2 3) / 3 * 3 against 2 / 3 * 3.   B: MEAN (1 2) / 7 * 7 against 1.5 / 7 * 7.
      *> C: MEAN (1 1 4) / 3 * 3 against 2 / 3 * 3.   D: the chain MEAN (1 2 3) / K / K / K / K against 2 / K / K / K / K.
      *> E: the same A under ROUNDED MODE IS TRUNCATION, the COBOL-2014 phrase that gives this program its edition (14.7.4.2).
      *> cite.py --check 8.8.1.5.3 "performed as specified in ISO/IEC 60559:2020" -> OK 8.8.1.5.3
      *> cite.py --check 15.84.4 "rounded to 34 digits according to the rules for standard-decimal arithmetic" -> OK 15.84.4 2)
      *> cite.py --check 14.7.4.3 "ROUNDED MODE IS TRUNCATION had been specified" -> OK 14.7.4.3 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3954QUO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FNV   PIC S9(3)V9(8) SIGN LEADING SEPARATE.
       01 LITV   PIC S9(3)V9(8) SIGN LEADING SEPARATE.
       01 K    PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE FNV = FUNCTION MEAN(1 2 3) / 3 * 3.
           COMPUTE LITV = 2 / 3 * 3.
           PERFORM SHOW-A.
           COMPUTE FNV = FUNCTION MEAN(1 2) / 7 * 7.
           COMPUTE LITV = 1.5 / 7 * 7.
           PERFORM SHOW-B.
           COMPUTE FNV = FUNCTION MEAN(1 1 4) / 3 * 3.
           COMPUTE LITV = 2 / 3 * 3.
           PERFORM SHOW-C.
           COMPUTE FNV = FUNCTION MEAN(1 2 3) / K / K / K / K.
           COMPUTE LITV = 2 / K / K / K / K.
           PERFORM SHOW-D.
           COMPUTE FNV ROUNDED MODE IS TRUNCATION = FUNCTION MEAN(1 2 3) / 3 * 3.
           COMPUTE LITV ROUNDED MODE IS TRUNCATION = 2 / 3 * 3.
           PERFORM SHOW-E.
           STOP RUN.
       SHOW-A.
           DISPLAY "A F=" FNV " L=" LITV WITH NO ADVANCING.
           IF FNV = LITV DISPLAY " SAME" ELSE DISPLAY " DIFFER" END-IF.
       SHOW-B.
           DISPLAY "B F=" FNV " L=" LITV WITH NO ADVANCING.
           IF FNV = LITV DISPLAY " SAME" ELSE DISPLAY " DIFFER" END-IF.
       SHOW-C.
           DISPLAY "C F=" FNV " L=" LITV WITH NO ADVANCING.
           IF FNV = LITV DISPLAY " SAME" ELSE DISPLAY " DIFFER" END-IF.
       SHOW-D.
           DISPLAY "D F=" FNV " L=" LITV WITH NO ADVANCING.
           IF FNV = LITV DISPLAY " SAME" ELSE DISPLAY " DIFFER" END-IF.
       SHOW-E.
           DISPLAY "E F=" FNV " L=" LITV WITH NO ADVANCING.
           IF FNV = LITV DISPLAY " SAME" ELSE DISPLAY " DIFFER" END-IF.
