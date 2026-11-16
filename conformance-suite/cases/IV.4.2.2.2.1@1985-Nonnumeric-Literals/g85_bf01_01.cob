      *>--- conformance-case
      *> id: g85_bf01_01
      *> rules: [1985-GR-IV.4.2.2.2.1.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.1.3 GR3 - a nonnumeric literal is of category
      *> alphanumeric, so a relation between a numeric integer item and
      *> a nonnumeric literal is a nonnumeric comparison
      *> (§1985:VI.6.3.1.1.2): the numeric item is treated as moved to
      *> an alphanumeric item of its own size and compared character by
      *> character, the shorter operand space-extended on the right.
      *> Each CAT-n leg compares against a nonnumeric literal and each
      *> NUM-n leg against the numeric literal of the same digits; a
      *> compiler that took the quoted literal as numeric would print
      *> the NUM answer on the CAT line, so every CAT line can fail.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0101.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N2               PIC 99    VALUE 10.
       01  WS-N3               PIC 999   VALUE 5.
       01  WS-A4               PIC X(4)  VALUE "AB".
       PROCEDURE DIVISION.
       P-MAIN.
      * CAT-1: "10" against "9 ": '1' precedes '9', so LESS.
           IF WS-N2 < "9"
               DISPLAY "CAT-1 LT"
           ELSE
               DISPLAY "CAT-1 GE".
      * NUM-1: algebraic 10 against 9, so NOT LESS.
           IF WS-N2 < 9
               DISPLAY "NUM-1 LT"
           ELSE
               DISPLAY "NUM-1 GE".
      * CAT-2: "005" against "5  ": '0' differs from '5', so NOT EQUAL.
           IF WS-N3 = "5"
               DISPLAY "CAT-2 EQ"
           ELSE
               DISPLAY "CAT-2 NE".
      * NUM-2: algebraic 5 against 5, so EQUAL.
           IF WS-N3 = 5
               DISPLAY "NUM-2 EQ"
           ELSE
               DISPLAY "NUM-2 NE".
      * CAT-3: "005" against "005", so EQUAL.
           IF WS-N3 = "005"
               DISPLAY "CAT-3 EQ"
           ELSE
               DISPLAY "CAT-3 NE".
      * CAT-4: an alphanumeric item "AB  " against "AB": the literal is
      * space-extended, so EQUAL.
           IF WS-A4 = "AB"
               DISPLAY "CAT-4 EQ"
           ELSE
               DISPLAY "CAT-4 NE".
           STOP RUN.
