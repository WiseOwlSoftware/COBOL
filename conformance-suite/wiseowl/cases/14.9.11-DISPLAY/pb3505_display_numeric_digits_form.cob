*>--- conformance-case
*> id: pb3505_display_numeric_digits_form
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [85, 2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR1 (Annex A.1 item 56, docs/CONFORMANCE.md DOC-A.1-56; kb/Work PB3505, owner
      *> decision PB3467) under the NON-DEFAULT `--numeric-display digits`, set for this test in
      *> wiseowl/options.json: DISPLAY shows a numeric item's DIGIT IMAGE, the digits as stored with
      *> no decimal point (it is implied) and an unseparated sign over-punched onto a digit by the
      *> default `--sign-encoding ibm`: positive 0-9 are "{ABCDEFGHI" and negative 0-9 "}JKLMNOPQR",
      *> the sign taking the LAST digit position (the FIRST under SIGN LEADING). The declarations are
      *> the default-form test's (pb3505_display_numeric_decimal_form) so the two outputs read side by
      *> side, which is what makes the OPTION, rather than the program, the thing under test.
      *>
      *> EXPECTED VALUES are derived from the determination and the declarations, not observed:
      *> 12.5 in 9(5)V99 is the seven digits 0001250; +12.5 and -12.5 in S9(5)V99 end in the table
      *> position of their last digit 0 ("{" and "}"); +5 and -5 in S9(3) are 00 and the table
      *> position 5 ("E" and "N"); SLEAD fuses onto the FIRST digit; SLS and STS carry the separate
      *> character '+'/'-' (GR6 b) of the SIGN clause, where the program put it; a COMP or COMP-3
      *> item has no zoned overpunch, so its signed image is a leading '-' only when negative; a P
      *> scaling shows only the stored digits (1200 in 9(3)PP is stored as 012).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3505G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U1 PIC 9(5)V99 VALUE 12.5.
       01 S1P PIC S9(5)V99 VALUE 12.5.
       01 S1N PIC S9(5)V99 VALUE -12.5.
       01 I1P PIC S9(3) VALUE 5.
       01 I1N PIC S9(3) VALUE -5.
       01 I1Z PIC S9(3) VALUE 0.
       01 U3 PIC 9(3) VALUE 7.
       01 SLEAD PIC S9(3)V99 SIGN LEADING VALUE -1.5.
       01 SLS PIC S9(3)V99 SIGN LEADING SEPARATE VALUE -1.5.
       01 STS PIC S9(3)V99 SIGN TRAILING SEPARATE VALUE -1.5.
       01 STSP PIC S9(3)V99 SIGN TRAILING SEPARATE VALUE 1.5.
       01 C1 PIC 9(3)V99 COMP VALUE 1.5.
       01 C1S PIC S9(3)V99 COMP VALUE -1.5.
       01 C3 PIC 9(3)V99 COMP-3 VALUE 1.5.
       01 C3S PIC S9(3)V99 COMP-3 VALUE -1.5.
       01 PP1 PIC 9(3)PP VALUE 1200.
       01 PP3 PIC PP9(3) VALUE 0.00123.
       01 V1 PIC V9(3) VALUE 0.5.
       01 V2 PIC 9V9(3) VALUE 0.5.
       01 ED PIC ZZ9.99.
       01 G.
          05 GA PIC 9(3)V99 VALUE 1.5.
          05 GB PIC S9(3) VALUE -5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 12.5 TO ED
           DISPLAY "U1=" U1 "|"
           DISPLAY "S1P=" S1P "|"
           DISPLAY "S1N=" S1N "|"
           DISPLAY "I1P=" I1P "|"
           DISPLAY "I1N=" I1N "|"
           DISPLAY "I1Z=" I1Z "|"
           DISPLAY "U3=" U3 "|"
           DISPLAY "SLEAD=" SLEAD "|"
           DISPLAY "SLS=" SLS "|"
           DISPLAY "STS=" STS "|"
           DISPLAY "STSP=" STSP "|"
           DISPLAY "C1=" C1 "|"
           DISPLAY "C1S=" C1S "|"
           DISPLAY "C3=" C3 "|"
           DISPLAY "C3S=" C3S "|"
           DISPLAY "PP1=" PP1 "|"
           DISPLAY "PP3=" PP3 "|"
           DISPLAY "V1=" V1 "|"
           DISPLAY "V2=" V2 "|"
           DISPLAY "ED=" ED "|"
           DISPLAY "G=" G "|"
           DISPLAY "GA=" GA " GB=" GB "|"
           DISPLAY "LIT=" 12.50 " " -5 " " 0.5 " " ZERO "|"
           DISPLAY U1 NO ADVANCING
           DISPLAY S1N
           STOP RUN.
