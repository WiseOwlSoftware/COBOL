*>--- conformance-case
*> id: pb3505_display_numeric_decimal_form
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [85, 2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR1 - "Any conversion of data required between literal-1 or the data item
      *> referenced by identifier-1 and the device is defined by the implementor" (Annex A.1 item 56,
      *> docs/CONFORMANCE.md DOC-A.1-56; kb/Work PB3505, owner decision PB3467).
      *>
      *> THE DETERMINATION PINNED HERE: under the default `--numeric-display decimal`, DISPLAY shows a
      *> numeric item as a reader writes its value: the digit positions the PICTURE declares, the
      *> decimal point at the scale's place, the zeros a P scaling stands for, and a SIGNED item's
      *> sign as a leading + or - character (a SIGN TRAILING SEPARATE item shows it trailing, where
      *> the program put it). The alternative, the digit image, is pinned by
      *> wiseowl/cases/14.9.11-DISPLAY/pb3505_display_numeric_digits_form.
      *> The latitude is the same at every edition, so every edition's suite accepts this program.
      *>
      *> EXPECTED VALUES are derived from the determination and the declarations, not observed:
      *> each line names its item, and the value is the PICTURE's positions filled with the VALUE.
      *> Three things keep the form from being a coincidence of one item: the SCALE (U1, V1, V2, PP1,
      *> PP3), the SIGN in each of its five presentations (S1P/S1N overpunch, SLEAD, SLS, STS, the
      *> COMP and COMP-3 usages that have no zoned overpunch at all), and the operands the form must
      *> NOT reach: the edited item ED, the group G (its storage image, §14.9.25.4 GR4's characters)
      *> and the literals, which are shown as written (DOC-A.1-56, kb/Work PB1643).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3505D.
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
