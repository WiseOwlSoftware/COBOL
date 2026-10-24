      *>--- conformance-case
      *> id: pb1910_overlap_in_place_move
      *> rules: [GR-14.6.10-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.6.10 1) - OVERLAPPING OPERANDS OF MOVE: AN UNDEFINED RESULT
      *> THAT WISEOWL COBOL DOCUMENTS AND PINS (kb/Work PB1910, PB1907;
      *> D-OVL2 in docs/CONFORMANCE.md section 3).
      *>
      *> 14.6.10 1): "When the data items are not described by the same data
      *>   description entry, the result of the statement is undefined."  OK
      *>   cite.py --check 14.6.10 -> 1).
      *> 4.4 2): "A COBOL run unit that allows these situations to happen is
      *>   a conforming run unit" - so every result below conforms, and the
      *>   expected output is WiseOwl COBOL's documented choice, not a value
      *>   the standard derives.  OK cite.py --check 4.4 -> 2).
      *>
      *> THE DOCUMENTED CHOICE (CLAUDE.md rule 1: the ISO text is silent, so
      *> GnuCOBOL 3.2.0 decides, MEASURED with a built cobc): a numeric
      *> DISPLAY or alphanumeric sender is converted into a numeric DISPLAY
      *> or PACKED-DECIMAL receiver IN PLACE - the receiver is cleared and
      *> then written one digit (one digit pair) at a time, each taken from
      *> the sender's bytes AS THE STORAGE THEN HOLDS THEM.  So:
      *>   PK-HEAD   the receiver over the sender's first bytes reads the
      *>             cleared bytes: 123456 gives 00456.
      *>   PK-TAIL   over the last bytes the second pair is formed from the
      *>             byte the first pair has just written: 23300, which is
      *>             neither the snapshot (23456) nor "clear, then convert"
      *>             (23000).
      *>   SMEAR-N   a receiver to the right of the sender's digits repeats
      *>             the first digit read: 9(4) over bytes 4-7 of 123456
      *>             gives 3333.
      *>   SMEAR-X   the same from an alphanumeric sender.
      *>   P-SIGN    a PACKED sender's digits are a snapshot but its sign is
      *>             read from its last byte after the receiver is stored.
      *>   PP-TAIL, PP-SEND, PP-PACK   a P position stores no byte, only the
      *>             scale: the library's offsets count the STORED digits, so
      *>             9(3)PP PACKED-DECIMAL over the tail of 123456 shows 23300,
      *>             9(4)PP (123400) into 9(5) PACKED-DECIMAL over bytes 2-4
      *>             shows 00000 and a 9(3)PP PACKED-DECIMAL sender holding
      *>             12300 into 9(4) over its bytes 2-5 gives 2300.
      *>   PP-OUT, PP-LOW   the sender's digit positions and the receiver's
      *>             share no decimal position (123456 into PP9(3) PACKED-
      *>             DECIMAL; .001234 into 9(5) PACKED-DECIMAL): libcob then
      *>             reads past the sender or writes past the receiver, which
      *>             no rule describes, so the snapshot stands: zero.
      *> It applies only where the overlap is a compile-time fact.
      *>
      *> WHY EACH LEG CAN FAIL: a compiler that snapshots every MOVE prints
      *> PK-HEAD=23456 and SMEAR-N=3456; one that clears the receiver and
      *> then converts prints PK-TAIL=23000 and SMEAR-N=0000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1910IP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1.
          05 AD PIC 9(6) VALUE 123456.
       01 A2 REDEFINES A1.
          05 AP PIC 9(5) PACKED-DECIMAL.
          05 FILLER PIC X(3).
       01 B1.
          05 BD PIC 9(6) VALUE 123456.
       01 B2 REDEFINES B1.
          05 FILLER PIC X(3).
          05 BP PIC 9(5) PACKED-DECIMAL.
       01 C1.
          05 CS6 PIC 9(6) VALUE 123456.
          05 FILLER PIC X(2) VALUE SPACES.
       01 C2 REDEFINES C1.
          05 FILLER PIC X(3).
          05 CN PIC 9(4).
          05 FILLER PIC X.
       01 D1.
          05 DX PIC X(6) VALUE "123456".
          05 FILLER PIC X(2) VALUE SPACES.
       01 D2 REDEFINES D1.
          05 FILLER PIC X(3).
          05 DN PIC 9(4).
          05 FILLER PIC X.
       01 E1.
          05 EX PIC X(6) VALUE "123456".
          05 FILLER PIC X(2) VALUE SPACES.
       01 E2 REDEFINES E1.
          05 FILLER PIC X(4).
          05 EN PIC 9(4).
       01 F1.
          05 FP PIC S9(7) PACKED-DECIMAL VALUE -1234567.
          05 FILLER PIC X VALUE SPACE.
       01 F2 REDEFINES F1.
          05 FS PIC S9(4) SIGN IS LEADING SEPARATE.
       01 G1.
          05 GD PIC 9(6) VALUE 123456.
          05 FILLER PIC X(8) VALUE SPACES.
       01 G2 REDEFINES G1.
          05 FILLER PIC X(3).
          05 GP PIC 9(3)PP PACKED-DECIMAL.
       01 H1.
          05 HD PIC 9(4)PP VALUE 123400.
          05 FILLER PIC X(8) VALUE SPACES.
       01 H2 REDEFINES H1.
          05 FILLER PIC X.
          05 HP PIC 9(5) PACKED-DECIMAL.
       01 I1.
          05 IP PIC 9(3)PP PACKED-DECIMAL VALUE 12300.
          05 FILLER PIC X(8) VALUE SPACES.
       01 I2 REDEFINES I1.
          05 FILLER PIC X.
          05 IN-D PIC 9(4).
       01 J1.
          05 JD PIC 9(6) VALUE 123456.
          05 FILLER PIC X(8) VALUE SPACES.
       01 J2 REDEFINES J1.
          05 FILLER PIC X(3).
          05 JP PIC PP9(3) PACKED-DECIMAL.
       01 K1.
          05 KD PIC PP9(4) VALUE .001234.
          05 FILLER PIC X(8) VALUE SPACES.
       01 K2 REDEFINES K1.
          05 FILLER PIC X.
          05 KP PIC 9(5) PACKED-DECIMAL.
       PROCEDURE DIVISION.
       MAIN.
           MOVE AD TO AP
           DISPLAY "PK-HEAD=" AP
           MOVE BD TO BP
           DISPLAY "PK-TAIL=" BP
           MOVE CS6 TO CN
           DISPLAY "SMEAR-N=" CN
           MOVE DX TO DN
           DISPLAY "SMEAR-X=" DN
           MOVE EX TO EN
           DISPLAY "SMEAR-X4=" EN
           MOVE FP TO FS
           DISPLAY "P-SIGN=" FS
           MOVE GD TO GP
           DISPLAY "PP-TAIL=" GP
           MOVE HD TO HP
           DISPLAY "PP-SEND=" HP
           MOVE IP TO IN-D
           DISPLAY "PP-PACK=" IN-D
           MOVE JD TO JP
           DISPLAY "PP-OUT=" JP
           MOVE KD TO KP
           DISPLAY "PP-LOW=" KP
           STOP RUN.
