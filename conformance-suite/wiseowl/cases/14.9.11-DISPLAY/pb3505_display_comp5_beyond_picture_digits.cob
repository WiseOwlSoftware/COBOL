*>--- conformance-case
*> id: pb3505_display_comp5_beyond_picture_digits
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR1 (Annex A.1 item 56; kb/Work PB3505, owner decision PB3467, which moves
      *> kb/Work R13's DISPLAY of a BinaryCapacity item to the option's other value) under
      *> `--numeric-display digits`, set for this test in wiseowl/options.json: DISPLAY shows the
      *> PICTURE-DIGIT image of a COMP-5 item holding a value beyond its PICTURE, the value modulo
      *> 10^digits, as before the option existed. The default form shows the whole value
      *> (pb3505_display_numeric_container_widths); the full value stays reachable here through the
      *> spec-fixed MOVE (§14.9.25.4 GR6 a: PIC 9(20) holds all twenty digits).
      *>
      *> EXPECTED: 18446744073709551615 is twenty digits; the item's PICTURE 9(18) keeps the low
      *> eighteen, 446744073709551615; the MOVE to PIC 9(20) keeps the value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3505R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9(18) COMP-5.
       01 BIG PIC 9(20).
       PROCEDURE DIVISION.
           MOVE 18446744073709551615 TO W
           DISPLAY W
           MOVE W TO BIG
           DISPLAY BIG.
           STOP RUN.
