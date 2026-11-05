      *>--- conformance-case
      *> id: pb4587_native_power_reduced_once
      *> rules: [DOC-A.1-123, DOC-A.1-179, GR-8.8.1.2-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4587 (docs/CONFORMANCE.md DOC-A.1-123): under NATIVE
      *> arithmetic a power past the 77-digit wide form is the EXACT
      *> power reduced ONCE, not a chain of products each reduced to 34
      *> digits. ISO 8.8.1.3: "Native arithmetic is an
      *> implementor-defined method of evaluating an arithmetic
      *> expression" (cite.py: OK 8.8.1.3) and 11.9.11.2 1): "the
      *> rounding rules that apply to intermediate data items are
      *> defined by the implementor" (OK), so CLAUDE.md rule 1 sends the
      *> choice to GnuCOBOL, whose cob_decimal_pow is an exact
      *> mpz_pow_ui at any size; 8.8.1.5.4 2) e)'s chain of rounded
      *> multiplications binds STANDARD-DECIMAL only. The receivers
      *> below hold at most 31 digit positions (13.18.40.3 14)), so a
      *> value reduced once is observably the exact power; the chain
      *> drifted 2 to 8 units in its 34th digit.
      *>
      *> EXPECTED VALUES, DERIVED (Python fractions, truncation unless
      *> ROUNDED, which is NEAREST-AWAY-FROM-ZERO by default,
      *> 14.7.4.3 2) and 4)):
      *>   L5/L5B  1.01 ** 119 into 9V9(30) = 3.267709796607589156313825958851
      *>           (the chain gave ...852), literal and data-item exponent
      *>   L6      1.01 ** 181 ROUNDED into 9V9(30) = 6.055759995109729203443854749876
      *>   L12     2.5 ** 100 / 2.5 ** 98 into 9V9(6) = 6.250000 (the chain 6.249999)
      *>   L7      3 ** 200 / 3 ** 199 into 9V9(6) = 3.000000
      *>   G14     1.09 ** 115 into 9(5)V9(26) = 20139.43410312256224348269189714
      *>   G15     1.08 ** 297 ROUNDED into 9(10)V9(21) = 8449974135.558113648519770897687
      *>   Q9      (A * B) ** 2 = (A * B) * (A * B): the square of a
      *>           40-digit product (79 digits) and the product of the
      *>           same two values are one value (EQ)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4587POWONCE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X25   PIC 9V9 VALUE 2.5.
       01 X3    PIC 9 VALUE 3.
       01 X101  PIC 9V99 VALUE 1.01.
       01 X109  PIC 9V99 VALUE 1.09.
       01 X108  PIC 9V99 VALUE 1.08.
       01 N98   PIC 999 VALUE 98.
       01 N100  PIC 999 VALUE 100.
       01 N115  PIC 999 VALUE 115.
       01 N119  PIC 999 VALUE 119.
       01 N199  PIC 999 VALUE 199.
       01 N200  PIC 999 VALUE 200.
       01 N297  PIC 999 VALUE 297.
       01 A     PIC 9(20) VALUE 85460336068819949348.
       01 B     PIC 9(20) VALUE 27482144350526720241.
       01 R30   PIC 9V9(30).
       01 R6    PIC 9V9(6).
       01 R26   PIC 9(5)V9(26).
       01 R21   PIC 9(10)V9(21).
       PROCEDURE DIVISION.
           COMPUTE R30 = 1.01 ** 119
           DISPLAY "L5  " R30
           COMPUTE R30 = X101 ** N119
           DISPLAY "L5B " R30
           COMPUTE R30 ROUNDED = 1.01 ** 181
           DISPLAY "L6  " R30
           COMPUTE R6 = 2.5 ** 100 / 2.5 ** 98
           DISPLAY "L12 " R6
           COMPUTE R6 = X25 ** N100 / X25 ** N98
           DISPLAY "L12B " R6
           COMPUTE R6 = X3 ** N200 / X3 ** N199
           DISPLAY "L7  " R6
           COMPUTE R26 = X109 ** N115
           DISPLAY "G14 " R26
           COMPUTE R21 ROUNDED = X108 ** N297
           DISPLAY "G15 " R21
           IF (A * B) ** 2 = (A * B) * (A * B)
               DISPLAY "Q9  EQ"
           ELSE
               DISPLAY "Q9  NE".
           STOP RUN.
       END PROGRAM PB4587POWONCE.
