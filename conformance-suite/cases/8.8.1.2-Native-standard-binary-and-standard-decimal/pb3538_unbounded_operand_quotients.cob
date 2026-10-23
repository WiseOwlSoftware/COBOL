      *>--- conformance-case
      *> id: pb3538_unbounded_operand_quotients
      *> rules: [GR-8.8.1.2-3, GR-14.7.4.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538 branch review finding R1-1: a nested quotient
      *> whose dividend is a function value has no digit bound of its
      *> own, so the compiler forms it past the Int128 lane; the LANE
      *> must not change the VALUE. 8.8.1.2 3) evaluates left to right,
      *> a nested quotient is truncated at the documented native
      *> precision (CONFORMANCE.md DOC-A.1-123: the receiver's or the
      *> operands' fraction digits, whichever is more, plus 14), and
      *> 14.7.4.3 2) truncates into the receiver. So each FUNCTION
      *> spelling equals the data-item spelling beside it:
      *>   ITEMQ / INTQ  2 / 3 = 0.666666666666666666 (scale 18) * 3
      *>                 = 1.999999999999999998, stored 1.9999;
      *>   MODQ   MOD (8 7) = 1: 1 / 3 * 3 stored 0.9999;
      *>   ITEMQ0 / INTQ0 the same into PIC 9(4) (scale 14): 1;
      *>   PRODQ  INTEGER (2) * 8 = 16: 16 / 3 * 3 stored 15.9999;
      *>   DEEPQ  8 / 2 / 2 / 2 / 2 * 2 = 1;
      *>   ITEMREL / INTREL  2 / 3 * 3 = 1.99999999999999 < 2;
      *>   SUMITEM / SUMINT / SUMMIX  10 ** 17 less 10 ** 17 / 3 * 3
      *>          (scale 32) is 10 ** -32, stored 0 in V9(18).
      *> Why each leg can fail: the SDIDI's round-to-odd quotient made
      *> INTQ 2.0000, INTQ0 2, PRODQ 16.0000 and INTREL NOT LESS,
      *> while the item legs kept the truncation; a scale-32 quotient
      *> of BIG (50 digits) passed the Int128 lane on any spelling, and
      *> a sum of a wide value and a function value took the SDIDI too,
      *> so each SUM leg kept a round-to-odd tail (0.00000000000000001).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538UNQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9 VALUE 8.
       01 B    PIC 9 VALUE 2.
       01 T    PIC 9 VALUE 3.
       01 R    PIC 9(4)V9(4) VALUE 0.
       01 RI   PIC 9(4) VALUE 0.
       01 BIG  PIC 9(18) VALUE 100000000000000000.
       01 RX   PIC 9V9(18) VALUE 0.
       01 EX   PIC 9.9(18).
       01 E8   PIC Z(3)9.9(4).
       01 E4   PIC Z(3)9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = B / T * T.
           MOVE R TO E8.
           DISPLAY "ITEMQ " E8.
           COMPUTE R = FUNCTION INTEGER (B) / T * T.
           MOVE R TO E8.
           DISPLAY "INTQ " E8.
           COMPUTE R = FUNCTION MOD (A 7) / T * T.
           MOVE R TO E8.
           DISPLAY "MODQ " E8.
           COMPUTE RI = B / T * T.
           MOVE RI TO E4.
           DISPLAY "ITEMQ0 " E4.
           COMPUTE RI = FUNCTION INTEGER (B) / T * T.
           MOVE RI TO E4.
           DISPLAY "INTQ0 " E4.
           COMPUTE R = FUNCTION INTEGER (B) * A / T * T.
           MOVE R TO E8.
           DISPLAY "PRODQ " E8.
           COMPUTE R = FUNCTION INTEGER (A) / B / B / B / B * B.
           MOVE R TO E8.
           DISPLAY "DEEPQ " E8.
           IF B / T * T < 2
              DISPLAY "ITEMREL LESS"
           ELSE
              DISPLAY "ITEMREL NOT LESS"
           END-IF.
           IF FUNCTION INTEGER (B) / T * T < 2
              DISPLAY "INTREL LESS"
           ELSE
              DISPLAY "INTREL NOT LESS"
           END-IF.
           COMPUTE RX = BIG - BIG / T * T.
           MOVE RX TO EX.
           DISPLAY "SUMITEM " EX.
           COMPUTE RX = FUNCTION INTEGER (BIG)
               - FUNCTION INTEGER (BIG) / T * T.
           MOVE RX TO EX.
           DISPLAY "SUMINT " EX.
           COMPUTE RX = FUNCTION INTEGER (BIG) - BIG / T * T.
           MOVE RX TO EX.
           DISPLAY "SUMMIX " EX.
           STOP RUN.
