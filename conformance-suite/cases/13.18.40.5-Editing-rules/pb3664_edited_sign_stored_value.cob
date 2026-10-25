      *>--- conformance-case
      *> id: pb3664_edited_sign_stored_value
      *> rules: [GR-13.18.40.5-5, GR-13.18.40.5-6, GR-14.9.25.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3664.
      * kb/Work PB3664 - the editing sign follows the value the
      * receiver ENDS UP HOLDING, never the sending value.
      * 13.18.40.5 rule 5: "Table 8, Results of fixed insertion
      * editing, shows the character(s) produced by an editing sign
      * control symbol, depending on the value of the data item" -
      * Table 8's first column is "Positive or zero value". 14.9.25.4
      * rule 6 takes alignment and truncation from 14.6.8.2 rule 4,
      * "zero fill or truncation on either end as required", so a
      * sending -0.001 moved into an item with two fraction digits
      * stores ZERO, and 8.8.4.2.4 says "Zero is considered a unique
      * value regardless of the sign".
      *
      *   S  PIC S9V999 VALUE -0.001, moved to each edited item:
      *        -9.99  -> " 0.00"    +9.99 -> "+0.00"
      *        9.99CR -> "0.00  "   9.99DB -> "0.00  "
      *        --9.99 -> "  0.00"   ++9.99 -> " +0.00"
      *   T  PIC S9(3) VALUE -100 into two integer digits holds 00:
      *        -99 -> " 00"   99- -> "00 "   99CR -> "00  "
      *   U  PIC S9V999 VALUE -0.012 keeps its sign (value -0.01):
      *        -9.99 -> "-0.01"   9.99CR -> "0.01CR"
      *   V  the same value through a signed numeric (S9V99) gives
      *        the same image as the direct move.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S            PIC S9V999 VALUE -0.001.
       01  T            PIC S9(3)  VALUE -100.
       01  U            PIC S9V999 VALUE -0.012.
       01  N            PIC S9V99.
       01  E1           PIC -9.99.
       01  E2           PIC +9.99.
       01  E3           PIC 9.99CR.
       01  E4           PIC 9.99DB.
       01  E5           PIC --9.99.
       01  E6           PIC ++9.99.
       01  E7           PIC -99.
       01  E8           PIC 99-.
       01  E9           PIC 99CR.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE S TO E1 E2 E3 E4 E5 E6
           DISPLAY "S [" E1 "][" E2 "][" E3 "][" E4 "][" E5 "][" E6 "]"
           MOVE T TO E7 E8 E9
           DISPLAY "T [" E7 "][" E8 "][" E9 "]"
           MOVE U TO E1 E3
           DISPLAY "U [" E1 "][" E3 "]"
           MOVE S TO N
           MOVE N TO E1
           DISPLAY "V [" E1 "]"
           MOVE S TO E1
           DISPLAY "V [" E1 "]"
           STOP RUN.
