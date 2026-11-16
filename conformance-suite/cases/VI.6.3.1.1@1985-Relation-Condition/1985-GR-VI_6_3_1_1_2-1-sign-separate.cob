      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_1_2-1-sign-separate
      *> rules: [1985-GR-VI.6.3.1.1.2-1, 1985-SR-IV.4.2.2.2.3-L3.2]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5569. §1985:VI.6.3.1.1.2 GR1: the numeric operand
      *> "is treated as though it were moved to an elementary
      *> alphanumeric data item of the same size as the numeric data
      *> item (in terms of standard data format characters)". The S of
      *> a SIGN SEPARATE item "is counted in determining the size of
      *> the item" (§1985:VI.5.12.4 GR6), so the temporary has a
      *> position for the sign the move does not send.
      *> N1 is PIC S99PP SIGN TRAILING SEPARATE, size 3: the move sends
      *> the digit positions 1200 (P positions counted, §1985:VI.6.19.4
      *> GR4) into three characters, "120", the image MOVE N1 TO X3
      *> stores, so N1 = X3 is true before and after that MOVE.
      *> S4 and S6 are PIC S9(3) SIGN LEADING and TRAILING SEPARATE
      *> holding 121, size 4: the temporary is "121 ". S4 = "121" is
      *> true (the shorter operand is extended by spaces) and
      *> S4 = "1212" false. ALL "12" opposite them is "repeated
      *> character by character on the right until the size of the
      *> resultant string is greater than or equal to the number of
      *> character positions in the associated data item"
      *> (§1985:IV.4.2.2.2.3 GRL2.1): four positions, "1212", which is
      *> not "121 ", so both relations are false, in either order.
      *> U3 is PIC 9(3) holding 121 (size 3, no sign position):
      *> ALL "12" is "121" and the relation is true.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REL2SS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N1  PIC S99PP SIGN TRAILING SEPARATE VALUE -1200.
       01  X3  PIC X(3) VALUE "120".
       01  S4  PIC S9(3) SIGN LEADING SEPARATE VALUE 121.
       01  S6  PIC S9(3) SIGN TRAILING SEPARATE VALUE 121.
       01  U3  PIC 9(3) VALUE 121.
       PROCEDURE DIVISION.
       P1.
           IF N1 = X3 DISPLAY "N1 = X3 T" ELSE DISPLAY "N1 = X3 F".
           IF N1 = "12" DISPLAY "N1 = 12 T" ELSE DISPLAY "N1 = 12 F".
           MOVE N1 TO X3.
           DISPLAY "MOVE N1 TO X3 [" X3 "]".
           IF N1 = X3 DISPLAY "N1 = X3 T" ELSE DISPLAY "N1 = X3 F".
           IF S4 = "121" DISPLAY "S4 = 121 T" ELSE DISPLAY "S4 = 121 F".
           IF S4 = "1212" DISPLAY "S4 = 1212 T"
               ELSE DISPLAY "S4 = 1212 F".
           IF S4 = ALL "12" DISPLAY "S4 = ALL T"
               ELSE DISPLAY "S4 = ALL F".
           IF S6 = ALL "12" DISPLAY "S6 = ALL T"
               ELSE DISPLAY "S6 = ALL F".
           IF ALL "12" = S6 DISPLAY "ALL = S6 T"
               ELSE DISPLAY "ALL = S6 F".
           IF U3 = ALL "12" DISPLAY "U3 = ALL T"
               ELSE DISPLAY "U3 = ALL F".
           EVALUATE S4 WHEN "121 " DISPLAY "EVALUATE S4 121 T"
               WHEN OTHER DISPLAY "EVALUATE S4 121 F" END-EVALUATE.
           STOP RUN.
