*>--- conformance-case
*> id: comp5_display_beyond_picture
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R13 - OWNER DECISION 2026-08-08: follow GnuCOBOL on DISPLAY of a BinaryCapacity item
      *> holding a beyond-PICTURE value (the vendors are split; 14.9.11.4 GR1 makes the conversion
      *> implementor-defined - CONFORMANCE.md section 7 item 56). R13 took GnuCOBOL's rendering to be
      *> the PICTURE-DIGIT IMAGE (the value mod 10^digits, 446744073709551615 here); kb/Work PB3505
      *> (owner decision PB3467, 2026-10-21) measured GnuCOBOL 3.2.0 on 2026-10-22 and it prints the
      *> WHOLE value, the container's twenty digits: so the default `--numeric-display decimal` shows
      *> 18446744073709551615, and the cut is the `digits` form's (wiseowl/cases/14.9.11-DISPLAY/
      *> pb3505_display_comp5_beyond_picture_digits). The value is also reachable through the
      *> spec-fixed MOVE path (14.9.25.4 GR6a: PIC 9(20) holds all twenty digits), the second line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R13PIN.
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
