*>--- conformance-case
*> id: pb3505_display_numeric_container_widths
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR1 (Annex A.1 item 56, docs/CONFORMANCE.md DOC-A.1-56; kb/Work PB3505, owner
      *> decision PB3467) under the default `--numeric-display decimal`: a BINARY-CAPACITY item
      *> (COMP-5, BINARY-CHAR / -SHORT / -LONG / -DOUBLE) holds any value of its CONTAINER
      *> (Annex A.1 item 208, docs/CONFORMANCE.md DOC-A.1-208: COMP-5 owns its full container range), past its PICTURE's digit count, so DISPLAY shows the
      *> container's digit positions - the decimal digits of the largest unsigned value of 1, 2, 4 and
      *> 8 bytes: 3, 5, 10 and 20, signed or unsigned - and never a value cut to its PICTURE
      *> (the digit image of `--numeric-display digits` cuts it; that is pinned by
      *> pb3505_display_comp5_beyond_picture_digits).
      *>
      *> EXPECTED VALUES from the container widths, not observed: BINARY-CHAR is one byte (3 digits),
      *> BINARY-SHORT two (5), BINARY-LONG four (10), BINARY-DOUBLE eight (20); PIC 9(3) COMP-5 is a
      *> two-byte container (5 digits) and holds 12345; PIC S9(2)V9 COMP-5 holds 300.0 as 3000 in its
      *> two bytes and shows its point at the scale. A signed item shows its sign, '+' when positive.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3505W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BC USAGE BINARY-CHAR VALUE -5.
       01 BCU USAGE BINARY-CHAR UNSIGNED VALUE 200.
       01 BS USAGE BINARY-SHORT VALUE -300.
       01 BSU USAGE BINARY-SHORT UNSIGNED VALUE 60000.
       01 BL USAGE BINARY-LONG VALUE -70000.
       01 BLU USAGE BINARY-LONG UNSIGNED VALUE 70000.
       01 BD USAGE BINARY-DOUBLE VALUE -5000000000.
       01 BDU USAGE BINARY-DOUBLE UNSIGNED VALUE 18446744073709551615.
       01 C5 PIC 9(3) COMP-5 VALUE 7.
       01 C5B PIC 9(3) COMP-5 VALUE 0.
       01 C5S PIC S9(2)V9 COMP-5 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BC=" BC "|"
           DISPLAY "BCU=" BCU "|"
           DISPLAY "BS=" BS "|"
           DISPLAY "BSU=" BSU "|"
           DISPLAY "BL=" BL "|"
           DISPLAY "BLU=" BLU "|"
           DISPLAY "BD=" BD "|"
           DISPLAY "BDU=" BDU "|"
           DISPLAY "C5=" C5 "|"
           MOVE 12345 TO C5B
           DISPLAY "C5B=" C5B "|"
           COMPUTE C5S = 300
           DISPLAY "C5S=" C5S "|"
           COMPUTE C5S = -300
           DISPLAY "C5S=" C5S "|"
           STOP RUN.
