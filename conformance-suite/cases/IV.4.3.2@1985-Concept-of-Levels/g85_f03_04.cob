      *>--- conformance-case
      *> id: g85_f03_04
      *> rules: [1985-GR-IV.4.3.2.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.2.1 GR3 - the special level-numbers: 66 regroups
      *> items by RENAMES (line 1), 77 describes a noncontiguous item
      *> that is not subdivided (line 2), 88 names a condition on values
      *> of a conditional variable (lines 3-4). Fails if any of the three
      *> entries is refused, if the RENAMES range is wrong, or if a
      *> condition-name tests the wrong value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0304.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  N77 PIC 9(3) VALUE 42.
       77  SW PIC 9 VALUE 1.
           88  SW-ON VALUE 1.
           88  SW-OFF VALUE 0 2 THRU 9.
       01  REC.
           05  F-A PIC X VALUE "1".
           05  F-B PIC X VALUE "2".
           05  F-C PIC X VALUE "3".
       66  AB RENAMES F-A THRU F-B.
       PROCEDURE DIVISION.
       P1.
           DISPLAY AB.
           DISPLAY N77.
           IF SW-ON
               DISPLAY "ON"
           ELSE
               DISPLAY "NOT ON"
           END-IF.
           MOVE 5 TO SW.
           IF SW-OFF
               DISPLAY "OFF"
           ELSE
               DISPLAY "NOT OFF"
           END-IF.
           STOP RUN.
