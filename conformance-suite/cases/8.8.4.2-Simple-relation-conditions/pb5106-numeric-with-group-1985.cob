      *>--- conformance-case
      *> id: pb5106-numeric-with-group-1985
      *> rules: [GR-8.8.4.2.1-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> A numeric operand compared with a GROUP operand.
      *>
      *> 1985 6.3.1.1.2 GR2 (python scripts/spec/cite.py --check
      *> "§1985:VI.6.3.1.1.2 GR2" "moved to a group item of the same
      *> size"): "If the nonnumeric operand is a group item, the numeric
      *> operand is treated as though it were moved to a group item of
      *> the same size as the numeric data item ..., and the content of
      *> this group item were then compared to the nonnumeric operand."
      *> A move to a group has no conversion (§1985:VI.6.19.4 GR3 item
      *> 5), so a SEPARATE sign character, part of the item, is
      *> compared: SN = -5 is the three characters "-05" and ST = -5 is
      *> "05-".
      *>
      *> From 2002 (§2002:8.8.4.1.1) a group is treated as an
      *> elementary alphanumeric item, so the numeric operand is moved
      *> by the elementary rule, which drops the sign
      *> (§2023:14.9.25.4 GR6a): SN is "05", and "05" is compared
      *> with "-05" after the shorter is padded with a space.
      *>
      *> Why each leg can fail: U1 and U2 are the unsigned control; U3
      *> and U5 (a group as subject or object) and U6 (SIGN TRAILING)
      *> are true only when the sign character is compared at 85, false
      *> elsewhere; U4 is the reverse.  U7 is the EVALUATE range, which
      *> lowers to the same relation pair (§2023:14.7.8): the group ends
      *> LO and HI hold "-05", so SN is within them only at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5106-NUM-GROUP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  G1  PIC X VALUE "0".
           05  G2  PIC X VALUE "5".
       01  N   PIC 99 VALUE 5.
       01  SN  PIC S99 SIGN IS LEADING SEPARATE CHARACTER
               VALUE -5.
       01  ST  PIC S99 SIGN IS TRAILING SEPARATE CHARACTER
               VALUE -5.
       01  H.
           05  H1  PIC X(3) VALUE "-05".
       01  H2.
           05  H21 PIC X(3) VALUE "-05".
       01  LO.
           05  LO1 PIC X(3) VALUE "-05".
       01  HI.
           05  HI1 PIC X(3) VALUE "-05".
       01  T.
           05  T1  PIC X(3) VALUE "05-".
       PROCEDURE DIVISION.
       P1.
           IF N = G DISPLAY "U1 EQUAL" ELSE DISPLAY "U1 NOT EQUAL".
           MOVE " 5" TO G.
           IF N = G DISPLAY "U2 EQUAL" ELSE DISPLAY "U2 NOT EQUAL".
           IF SN = H DISPLAY "U3 EQUAL" ELSE DISPLAY "U3 NOT EQUAL".
           MOVE "05" TO H.
           IF SN = H DISPLAY "U4 EQUAL" ELSE DISPLAY "U4 NOT EQUAL".
           IF H2 = SN DISPLAY "U5 EQUAL" ELSE DISPLAY "U5 NOT EQUAL".
           IF ST = T DISPLAY "U6 EQUAL" ELSE DISPLAY "U6 NOT EQUAL".
           EVALUATE SN
               WHEN LO THRU HI DISPLAY "U7 IN"
               WHEN OTHER DISPLAY "U7 OUT"
           END-EVALUATE.
           STOP RUN.
