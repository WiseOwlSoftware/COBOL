      *>--- conformance-case
      *> id: g85_f14_05
      *> rules: [1985-GR-VI.6.27.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.4 GR17 - an overflow condition arises when (a)
      *> the POINTER value at initiation is less than 1 or greater than
      *> the size of identifier-1, or (b) all receiving areas have been
      *> acted upon while identifier-1 holds unexamined characters.
      *> §1985:VI.6.27.4 GR18: the operation is terminated and ON
      *> OVERFLOW runs; §1985:VI.6.27.4 GR20: otherwise NOT ON OVERFLOW
      *> runs. A1 P=0 and A2 P=7 (SRC is 6) overflow at initiation: R1
      *> keeps *** and P is unchanged, nothing examined
      *> (§1985:VI.6.27.4 GR15). A3 P=6 (equal to the size) is not
      *> overflow: F is examined, P ends 07. A4 P=1 examines all six
      *> (R1 truncates to ABC): truncation is no overflow. B1 AB,CD,EF
      *> into two receivers leaves EF unexamined: overflow. B2 AB,CD
      *> exhausts the sender: no overflow. B3 without DELIMITED BY
      *> takes three characters per receiver, so G is unexamined:
      *> overflow. Each leg fails on the wrong branch or a transfer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1405.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC6  PIC X(6) VALUE "ABCDEF".
       01  SRC8  PIC X(8) VALUE "AB,CD,EF".
       01  SRC5  PIC X(5) VALUE "AB,CD".
       01  SRC7  PIC X(7) VALUE "ABCDEFG".
       01  R1    PIC X(3).
       01  R2    PIC X(3).
       01  P     PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "*" TO R1.
           MOVE 0 TO P.
           UNSTRING SRC6 DELIMITED BY "," INTO R1 WITH POINTER P
               ON OVERFLOW DISPLAY "A1 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "A1 NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "A1 [" R1 "] " P.
           MOVE ALL "*" TO R1.
           MOVE 7 TO P.
           UNSTRING SRC6 DELIMITED BY "," INTO R1 WITH POINTER P
               ON OVERFLOW DISPLAY "A2 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "A2 NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "A2 [" R1 "] " P.
           MOVE ALL "*" TO R1.
           MOVE 6 TO P.
           UNSTRING SRC6 DELIMITED BY "," INTO R1 WITH POINTER P
               ON OVERFLOW DISPLAY "A3 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "A3 NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "A3 [" R1 "] " P.
           MOVE ALL "*" TO R1.
           MOVE 1 TO P.
           UNSTRING SRC6 DELIMITED BY "," INTO R1 WITH POINTER P
               ON OVERFLOW DISPLAY "A4 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "A4 NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "A4 [" R1 "] " P.
           UNSTRING SRC8 DELIMITED BY "," INTO R1 R2
               ON OVERFLOW DISPLAY "B1 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "B1 NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "B1 [" R1 "][" R2 "]".
           UNSTRING SRC5 DELIMITED BY "," INTO R1 R2
               ON OVERFLOW DISPLAY "B2 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "B2 NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "B2 [" R1 "][" R2 "]".
           UNSTRING SRC7 INTO R1 R2
               ON OVERFLOW DISPLAY "B3 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "B3 NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "B3 [" R1 "][" R2 "]".
           STOP RUN.
