      *>--- conformance-case
      *> id: g2mov01_cd_move_operands
      *> rules: [2002-SR-14.8.24.2-3, 2002-SR-14.8.24.2-4,
      *>    2002-SR-14.8.24.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> MOVE format 1 whose operands are communication-section items
      *> (an input CD's SYMBOLIC QUEUE and TEXT LENGTH fields, and
      *> items of the records that implicitly redefine the CD areas,
      *> §2002:A.1.2.2 SR5, SR9). The facility draws COBOL0903.
      *> §2002:14.8.24.2 SR3 - literal-1 and identifier-1 are sending
      *>   operands: their content is moved (§2002:14.8.24.3 GR1) and
      *>   they are left unchanged. Lines 2, 3, 4, 6 send a CD field
      *>   or CD record item and redisplay it; lines 1, 3, 4, 5, 7
      *>   send literals. Fails if a CD sending operand is refused,
      *>   altered, or read from the wrong positions.
      *> §2002:14.8.24.2 SR4 - identifier-2 is a receiving operand:
      *>   SQ (X(12), §2002:A.1.2.3 GR2 a), TL (9(4), GR2 h), IA
      *>   (A(5)), IN4 (9(4)), OB, OB2 (1(4)) each receive; line 7
      *>   moves to a CD field and a WS item in one statement. SQ
      *>   pads to 12 (QUEUE-A + 5 spaces; XY + 10 spaces), 42 into
      *>   TL is 0042. Fails if a CD receiving operand is refused or
      *>   stored unaligned.
      *> §2002:14.8.24.2 SR7 - ZERO (boolean characters '0') and
      *>   ALL B"1" may go to the boolean record items of an output
      *>   CD: OB=0000, OB2=1111 (§2002:14.8.24.3 GR5 Table 18: ZERO
      *>   to a boolean receiver is boolean). Fails if the legal forms
      *>   are refused; the illegal SPACE is g2mov02.
      *> Record layouts keep every used item apart: INREC positions
      *>   13-17 IA, 18-21 IN4 (SQ is 1-12, TL 75-78); OUTREC 11-18.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A  PIC X(12).
       01 W-5  PIC X(5).
       01 W-B  PIC 1(4).
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL.
       01 INREC.
          05 FILLER PIC X(12).
          05 IA     PIC A(5).
          05 IN4    PIC 9(4).
          05 FILLER PIC X(66).
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OUTREC.
          05 FILLER PIC X(10).
          05 OB     PIC 1(4).
          05 OB2    PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "QUEUE-A" TO SQ.
           DISPLAY "1 SQ=[" SQ "]".
           MOVE SQ TO W-A.
           DISPLAY "2 W-A=[" W-A "] SQ=[" SQ "]".
           MOVE "ABCDE" TO IA.
           MOVE IA TO W-5.
           DISPLAY "3 IA=[" IA "] W-5=[" W-5 "]".
           MOVE 42 TO TL.
           MOVE TL TO IN4.
           DISPLAY "4 TL=" TL " IN4=" IN4.
           MOVE ZERO TO OB.
           MOVE ALL B"1" TO OB2.
           DISPLAY "5 OB=" OB " OB2=" OB2.
           MOVE OB2 TO W-B.
           DISPLAY "6 W-B=" W-B " OB2=" OB2.
           MOVE "XY" TO SQ W-A.
           DISPLAY "7 SQ=[" SQ "] W-A=[" W-A "]".
           STOP RUN.
