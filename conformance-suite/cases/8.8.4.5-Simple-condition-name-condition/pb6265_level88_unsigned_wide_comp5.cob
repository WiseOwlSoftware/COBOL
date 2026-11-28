      *>--- conformance-case
      *> id: pb6265_level88_unsigned_wide_comp5
      *> rules: [GR-8.8.4.5.3-1, GR-8.8.4.5.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6265 - a condition-name test over an unsigned
      *> 16-byte COMP-5 conditional variable whose VALUE has 19 or
      *> more digits is the SAME comparison as the relation of that
      *> variable with the VALUE.
      *> §2023:8.8.4.5.3 GR2: "The rules for comparing a conditional
      *> variable with a condition-name value are the same as those
      *> specified for relation conditions."
      *> §2023:8.8.4.2.4: "For operands whose class is numeric, a
      *> comparison is made with respect to the algebraic value of
      *> the operands regardless of the manner in which their usage
      *> is described."
      *> The value 10 to the 21st has 22 digits, past the 18 that a
      *> 64-bit carrier holds, so the variable is read as an
      *> unsigned 128-bit value and the condition-name VALUE as a
      *> signed one; the test must compare by algebraic value, not
      *> by a raw operator between the two carriers.
      *> §2023:8.8.4.5.3 GR1/GR2 with a THRU range: the values and
      *> range ends are inclusive ("including the end values").
      *> Expected values: U = 10**21. U-EQ = U: TRUE. U-OTHER =
      *> 10**21 + 1: FALSE. U-RNG 5 THRU 2*10**21 holds U: TRUE.
      *> U-ABOVE (10**21 + 1) THRU 2*10**21 excludes U: FALSE.
      *> U-LIST 1 or 10**21: TRUE. U-ZERO (ZERO is the numeric
      *> value 0, §2023:8.3.3.6.4 GR4): FALSE. SS = 12345678901234567890.25:
      *> SS-A equal: TRUE. SS-B from .26 up: FALSE. SS-C .01 THRU
      *> the value, inclusive: TRUE. SS-Z: FALSE.
      *> After SET U-OTHER TO TRUE (§2023:14.9.39.4 GR6) U holds
      *> 10**21 + 1: U-OTHER TRUE, U-EQ FALSE, U-ABOVE TRUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6265LV88.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U    PIC 9(25) COMP-5 VALUE 1000000000000000000000.
          88 U-EQ    VALUE 1000000000000000000000.
          88 U-OTHER VALUE 1000000000000000000001.
          88 U-RNG   VALUE 5 THRU 2000000000000000000000.
          88 U-ABOVE VALUE 1000000000000000000001 THRU
                           2000000000000000000000.
          88 U-LIST  VALUE 1, 1000000000000000000000.
          88 U-ZERO  VALUE ZERO.
       01 SS   PIC 9(20)V99 COMP-5 VALUE 12345678901234567890.25.
          88 SS-A    VALUE 12345678901234567890.25.
          88 SS-B    VALUE 12345678901234567890.26 THRU
                           99999999999999999999.99.
          88 SS-C    VALUE 0.01 THRU 12345678901234567890.25.
          88 SS-Z    VALUE ZERO.
       PROCEDURE DIVISION.
           IF U-EQ
               DISPLAY "U-EQ TRUE"
           ELSE
               DISPLAY "U-EQ FALSE"
           END-IF
           IF U-OTHER
               DISPLAY "U-OTHER TRUE"
           ELSE
               DISPLAY "U-OTHER FALSE"
           END-IF
           IF U-RNG
               DISPLAY "U-RNG TRUE"
           ELSE
               DISPLAY "U-RNG FALSE"
           END-IF
           IF U-ABOVE
               DISPLAY "U-ABOVE TRUE"
           ELSE
               DISPLAY "U-ABOVE FALSE"
           END-IF
           IF U-LIST
               DISPLAY "U-LIST TRUE"
           ELSE
               DISPLAY "U-LIST FALSE"
           END-IF
           IF U-ZERO
               DISPLAY "U-ZERO TRUE"
           ELSE
               DISPLAY "U-ZERO FALSE"
           END-IF
           IF NOT U-EQ
               DISPLAY "NOT-U-EQ TRUE"
           ELSE
               DISPLAY "NOT-U-EQ FALSE"
           END-IF
           IF SS-A
               DISPLAY "SS-A TRUE"
           ELSE
               DISPLAY "SS-A FALSE"
           END-IF
           IF SS-B
               DISPLAY "SS-B TRUE"
           ELSE
               DISPLAY "SS-B FALSE"
           END-IF
           IF SS-C
               DISPLAY "SS-C TRUE"
           ELSE
               DISPLAY "SS-C FALSE"
           END-IF
           IF SS-Z
               DISPLAY "SS-Z TRUE"
           ELSE
               DISPLAY "SS-Z FALSE"
           END-IF
           SET U-OTHER TO TRUE
           IF U-OTHER
               DISPLAY "SET U-OTHER TRUE"
           ELSE
               DISPLAY "SET U-OTHER FALSE"
           END-IF
           IF U-EQ
               DISPLAY "SET U-EQ TRUE"
           ELSE
               DISPLAY "SET U-EQ FALSE"
           END-IF
           IF U-ABOVE
               DISPLAY "SET U-ABOVE TRUE"
           ELSE
               DISPLAY "SET U-ABOVE FALSE"
           END-IF
           STOP RUN.
