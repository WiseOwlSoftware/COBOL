      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_2-5
      *> rules: [1985-GR-VI.6.3.1.2-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5122 - §1985:VI.6.3.1.2 GR5 (class-name-1) and GR2 (ALPHABETIC) over a numeric
      *> edited item. 1985 bars only an item described as numeric: "The ALPHABETIC test cannot be
      *> used with an item whose data description describes the item as numeric"; a numeric edited
      *> item is of the alphanumeric class (§1985:IV.4.3.3), so NE (PIC ZZZ) is tested by content:
      *> zero edits to three spaces (alphabetic, 21 T), 7 to "  7" (not alphabetic, 22 F; not
      *> all vowels, 23 T). The other lines are the slice-04 draft's (kb/Work PB4995), each derived
      *> from GR1 to GR5 and the zero-length group paragraph.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5122CN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS VOWEL IS "A" "E" "I" "O" "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X5 PIC X(5).
       01  SS PIC S99 SIGN IS LEADING SEPARATE CHARACTER.
       01  SS-X REDEFINES SS PIC XXX.
       01  US PIC 99.
       01  US-X REDEFINES US PIC XX.
       01  NE PIC ZZZ.
       01  CNT PIC 9 VALUE 0.
       01  ZG.
           05  ZT PIC X OCCURS 0 TO 5 TIMES DEPENDING ON CNT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "12345" TO X5.
           IF X5 IS NUMERIC DISPLAY "01 T" ELSE DISPLAY "01 F".
           MOVE "12 45" TO X5.
           IF X5 IS NUMERIC DISPLAY "02 T" ELSE DISPLAY "02 F".
           MOVE "+1234" TO X5.
           IF X5 IS NUMERIC DISPLAY "03 T" ELSE DISPLAY "03 F".
           IF X5 IS NOT NUMERIC DISPLAY "04 T" ELSE DISPLAY "04 F".
           MOVE "+12" TO SS-X.
           IF SS IS NUMERIC DISPLAY "05 T" ELSE DISPLAY "05 F".
           MOVE "-12" TO SS-X.
           IF SS IS NUMERIC DISPLAY "06 T" ELSE DISPLAY "06 F".
           MOVE " 12" TO SS-X.
           IF SS IS NUMERIC DISPLAY "07 T" ELSE DISPLAY "07 F".
           MOVE "012" TO SS-X.
           IF SS IS NUMERIC DISPLAY "08 T" ELSE DISPLAY "08 F".
           MOVE "12" TO US-X.
           IF US IS NUMERIC DISPLAY "09 T" ELSE DISPLAY "09 F".
           MOVE "+2" TO US-X.
           IF US IS NUMERIC DISPLAY "10 T" ELSE DISPLAY "10 F".
           MOVE "Ab Cd" TO X5.
           IF X5 IS ALPHABETIC DISPLAY "11 T" ELSE DISPLAY "11 F".
           IF X5 IS ALPHABETIC-LOWER DISPLAY "12 T"
               ELSE DISPLAY "12 F".
           IF X5 IS ALPHABETIC-UPPER DISPLAY "13 T"
               ELSE DISPLAY "13 F".
           MOVE "ab cd" TO X5.
           IF X5 IS ALPHABETIC-LOWER DISPLAY "14 T"
               ELSE DISPLAY "14 F".
           MOVE "AB CD" TO X5.
           IF X5 IS ALPHABETIC-UPPER DISPLAY "15 T"
               ELSE DISPLAY "15 F".
           MOVE "AB1CD" TO X5.
           IF X5 IS ALPHABETIC DISPLAY "16 T" ELSE DISPLAY "16 F".
           IF X5 IS NOT ALPHABETIC DISPLAY "17 T" ELSE DISPLAY "17 F".
           MOVE "AEIOU" TO X5.
           IF X5 IS VOWEL DISPLAY "18 T" ELSE DISPLAY "18 F".
           MOVE "AE OU" TO X5.
           IF X5 IS VOWEL DISPLAY "19 T" ELSE DISPLAY "19 F".
           IF X5 IS NOT VOWEL DISPLAY "20 T" ELSE DISPLAY "20 F".
           MOVE ZERO TO NE.
           IF NE IS ALPHABETIC DISPLAY "21 T" ELSE DISPLAY "21 F".
           MOVE 7 TO NE.
           IF NE IS ALPHABETIC DISPLAY "22 T" ELSE DISPLAY "22 F".
           IF NE IS NOT VOWEL DISPLAY "23 T" ELSE DISPLAY "23 F".
           MOVE 0 TO CNT.
           IF ZG IS ALPHABETIC DISPLAY "24 T" ELSE DISPLAY "24 F".
           IF ZG IS NOT ALPHABETIC DISPLAY "25 T" ELSE DISPLAY "25 F".
           IF ZG IS NUMERIC DISPLAY "26 T" ELSE DISPLAY "26 F".
           IF ZG IS NOT NUMERIC DISPLAY "27 T" ELSE DISPLAY "27 F".
           IF ZG IS VOWEL DISPLAY "28 T" ELSE DISPLAY "28 F".
           STOP RUN.
