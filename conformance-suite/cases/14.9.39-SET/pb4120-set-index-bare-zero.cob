      *>--- conformance-case
      *> id: pb4120-set-index-bare-zero
      *> rules: [FMT-14.9.39.2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4120 (and PB4172) - ISO 14.9.39.2 Format 1 sets an
      *> index-name TO arithmetic-expression-1 and Format 2 UP BY or
      *> DOWN BY arithmetic-expression-2, and 8.8.1.1: "An arithmetic
      *> expression may be an identifier referencing a numeric data item,
      *> a numeric literal, the figurative constant ZERO (ZEROS,
      *> ZEROES)". (The earlier editions print integer-1 there, and
      *> 8.3.3.6.3 SR1 a) admits ZERO where the literal is restricted to
      *> a numeric one.) So each bare ZERO below is the value zero:
      *> SET IX TO 3, UP BY ZERO and DOWN BY ZEROS leave IX on the third
      *> occurrence (C); SET IX TO ZEROES, then UP BY 2, puts it on the
      *> second (B; the occurrence number 0 lies inside the range OCCURS
      *> 13.18.38.4 GR2 requires). Fails if a bare ZERO is refused
      *> where no neighbouring operator shows the arithmetic expression,
      *> or if it is read as anything but zero.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4120SZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB VALUE "ABCD".
          05 TE PIC X OCCURS 4 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET IX TO 3.
           SET IX UP BY ZERO.
           SET IX DOWN BY ZEROS.
           DISPLAY TE (IX).
           SET IX TO ZEROES.
           SET IX UP BY 2.
           DISPLAY TE (IX).
           STOP RUN.
