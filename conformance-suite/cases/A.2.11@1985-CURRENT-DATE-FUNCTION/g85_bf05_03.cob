      *>--- conformance-case
      *> id: g85_bf05_03
      *> rules: [1989-FMT-A.2.11.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1989:A.2.11.2 - the format FUNCTION CURRENT-DATE (no argument
      *    list), accepted at 85 as a MOVE sender and a reference-
      *    modified operand. The value is the clock, so only its SHAPE
      *    is pinned, from
      * §1989:A.2.11.1 "returns a 21-character alphanumeric value" and
      *    the table of §1989:A.2.11.3 RV1 ("Four numeric digits of the
      *    year ..."):
      * L21 a 30-character receiver holds 21 characters then spaces;
      * DIG positions 1-16 and 18-21 are digits; S17 position 17 is -, +
      *    or 0;
      * MON 01-12, DAY 01-31, HMS hours 00-23, minutes and seconds
      *    00-59;
      * RM4 FUNCTION CURRENT-DATE (1:4) is the 4-digit year alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0503.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W30             PIC X(30).
       01  CDT             PIC X(21).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION CURRENT-DATE TO W30.
           IF W30 (22:9) = SPACES AND W30 (21:1) NOT = SPACE
               DISPLAY "L21=Y"
           ELSE
               DISPLAY "L21=N [" W30 "]".
           MOVE FUNCTION CURRENT-DATE TO CDT.
           IF CDT (1:16) IS NUMERIC AND CDT (18:4) IS NUMERIC
               DISPLAY "DIG=Y"
           ELSE
               DISPLAY "DIG=N [" CDT "]".
           IF CDT (17:1) = "-" OR "+" OR "0"
               DISPLAY "S17=Y"
           ELSE
               DISPLAY "S17=N [" CDT "]".
           IF CDT (5:2) NOT < "01" AND CDT (5:2) NOT > "12"
               DISPLAY "MON=Y"
           ELSE
               DISPLAY "MON=N [" CDT "]".
           IF CDT (7:2) NOT < "01" AND CDT (7:2) NOT > "31"
               DISPLAY "DAY=Y"
           ELSE
               DISPLAY "DAY=N [" CDT "]".
           IF CDT (9:2) NOT > "23" AND CDT (11:2) NOT > "59"
                   AND CDT (13:2) NOT > "59"
               DISPLAY "HMS=Y"
           ELSE
               DISPLAY "HMS=N [" CDT "]".
           MOVE FUNCTION CURRENT-DATE (1:4) TO W30.
           IF W30 (1:4) IS NUMERIC AND W30 (5:26) = SPACES
               DISPLAY "RM4=Y"
           ELSE
               DISPLAY "RM4=N [" W30 "]".
           STOP RUN.
