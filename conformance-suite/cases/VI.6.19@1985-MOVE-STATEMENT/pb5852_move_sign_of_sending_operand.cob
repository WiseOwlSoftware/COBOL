      *>--- conformance-case
      *> id: pb5852_move_sign_of_sending_operand
      *> rules: [1985-GR-VI.6.19.4-4]
      *> accept: [85, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5852 - the sign of a MOVE whose result truncates to
      *> zero. §1985:VI.6.19.4 GR4b1 (cite.py: OK): "When a signed
      *> numeric item is the receiving item, the sign of the sending
      *> operand is placed in the receiving item", so at 85 a negative
      *> sender whose digits all truncate away stores a NEGATIVE zero.
      *> §2023:14.9.25.4 GR6d.2.a (cite.py: OK): "the sign of the
      *> numeric value shall be represented in the receiving operand",
      *> the value after truncation, zero: at 2023 the zero is positive
      *> (.out); the 85 lines are .85.out. DERIVATION, line by line:
      *>  LIT->L, S3->L: -100 into S99 SIGN LEADING SEPARATE keeps the
      *>   digits 00 and, at 85, the sender's minus: -00 (2023: +00).
      *>  S3->T: the trailing separate sign: 00- (2023: 00+).
      *>  S3->P: a plain S99 over-punches its last digit (the default
      *>   IBM convention, DOC-A.1-177): negative zero is 0} (2023: 0{),
      *>   shown through the REDEFINES X as characters.
      *>  S1->L: -1 is not truncated: -01 at both editions.
      *>  L->L4: L holds -00; moved on, the SENDING OPERAND is negative,
      *>   so L4 is -00 at 85 (the value is zero: +00 at 2023).
      *>  LOW->L3: -0.4 truncates on the right (GR4b alignment): -00 at
      *>   85, +00 at 2023.
      *>  S3->U: an unsigned receiver takes "the absolute value of the
      *>   sending operand" and no sign (GR4b2): 00 at both.
      *>  POS->L5: a positive sender truncated to zero: +00 at both.
      *>  INIT: INITIALIZE REPLACING moves -100 by "an implicit MOVE
      *>   statement" (§1985:VI.6.17.4 GR2b, cite.py: OK): -00 at 85.
      *>  FILE: a group holding -00 and 00- is written and read back as
      *>   characters (a group move, GR5): -0000- at 85, +0000+ at 2023;
      *>   FILE->L5 moves the read field on: -00 at 85.
      *>  A negative zero's VALUE is zero: "Zero is considered a unique
      *>   value regardless of the sign" (§1985:VI.6.3.1.1.1, cite.py:
      *>   OK), so L = 0 holds; the sign condition calls an operand
      *>   "negative if its value is less than zero" (§1985:VI.6.3.1.5,
      *>   cite.py: OK), so L is ZERO and neither NEGATIVE nor POSITIVE.
      *>  COMPUTE C = L stores "the value of the arithmetic expression"
      *>   (§1985:VI.6.8.4 GR2, cite.py: OK), zero, whose sign is
      *>   positive: +00; ADD 1 TO L adds 1 to zero: +01. Both editions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5852MV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "pb5852.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE.
       01  SREC        PIC X(6).
       WORKING-STORAGE SECTION.
       01  L           PIC S99 SIGN LEADING SEPARATE.
       01  T           PIC S99 SIGN TRAILING SEPARATE.
       01  P           PIC S99.
       01  X REDEFINES P PIC XX.
       01  S3          PIC S999 VALUE -100.
       01  S1          PIC S99 VALUE -1.
       01  L3          PIC S99 SIGN LEADING SEPARATE.
       01  L4          PIC S99 SIGN LEADING SEPARATE.
       01  L5          PIC S99 SIGN LEADING SEPARATE.
       01  U           PIC 99.
       01  G.
           05  G-L     PIC S99 SIGN LEADING SEPARATE.
           05  G-T     PIC S99 SIGN TRAILING SEPARATE.
       01  G2.
           05  G2-L    PIC S99 SIGN LEADING SEPARATE.
           05  G2-T    PIC S99 SIGN TRAILING SEPARATE.
       01  INI.
           05  INI-L   PIC S99 SIGN LEADING SEPARATE.
       01  C           PIC S99 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       P0.
           MOVE -100 TO L.
           DISPLAY "LIT->L  [" L "]".
           MOVE S3 TO L.
           DISPLAY "S3->L   [" L "]".
           MOVE S3 TO T.
           DISPLAY "S3->T   [" T "]".
           MOVE S3 TO P.
           DISPLAY "S3->P   [" X "]".
           MOVE S1 TO L.
           DISPLAY "S1->L   [" L "]".
           MOVE S3 TO L.
           MOVE L TO L4.
           DISPLAY "L->L4   [" L4 "]".
           MOVE -0.4 TO L3.
           DISPLAY "LOW->L3 [" L3 "]".
           MOVE S3 TO U.
           DISPLAY "S3->U   [" U "]".
           MOVE 100 TO L5.
           DISPLAY "POS->L5 [" L5 "]".
           INITIALIZE INI REPLACING NUMERIC DATA BY -100.
           DISPLAY "INIT    [" INI-L "]".
           MOVE S3 TO G-L G-T.
           OPEN OUTPUT SFILE.
           WRITE SREC FROM G.
           CLOSE SFILE.
           OPEN INPUT SFILE.
           READ SFILE INTO G2 AT END DISPLAY "NO RECORD".
           CLOSE SFILE.
           DISPLAY "FILE    [" G2 "]".
           MOVE G2-L TO L5.
           DISPLAY "FILE->L5[" L5 "]".
           IF L = 0 DISPLAY "L = 0" ELSE DISPLAY "L NOT = 0".
           IF L IS ZERO DISPLAY "L ZERO" ELSE DISPLAY "L NOT ZERO".
           IF L IS NEGATIVE DISPLAY "L NEGATIVE"
               ELSE DISPLAY "L NOT NEGATIVE".
           IF L IS POSITIVE DISPLAY "L POSITIVE"
               ELSE DISPLAY "L NOT POSITIVE".
           COMPUTE C = L.
           DISPLAY "COMPUTE [" C "]".
           ADD 1 TO L.
           DISPLAY "ADD 1   [" L "]".
           STOP RUN.
