      *>--- conformance-case
      *> id: pb5132-numeric-embedded-sign-with-group-1985
      *> rules: [GR-8.8.4.2.1-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5132 item 4 - a signed numeric item with an EMBEDDED
      *> (overpunched) sign opposite a GROUP; pb5106-numeric-with-group-
      *> 1985 pins the SEPARATE-sign items.
      *>
      *> 1985 6.3.1.1.2 GR2 (python scripts/spec/cite.py --check
      *> "§1985:VI.6.3.1.1.2 GR2" "moved to a group item of the same
      *> size"): "If the nonnumeric operand is a group item, the numeric
      *> operand is treated as though it were moved to a group item of
      *> the same size as the numeric data item ..., and the content of
      *> this group item were then compared to the nonnumeric operand."
      *> A move to a group has no conversion (§1985:VI.6.19.4 GR3 item
      *> 5), so the group holds the numeric item's own storage, the
      *> overpunched sign included. GS and GL below REDEFINE SE and SL,
      *> so they hold exactly those bytes and the comparison is EQUAL
      *> at 85 whatever representation the implementor gives the
      *> embedded sign.
      *>
      *> From 2002 (§2002:8.8.4.1.1) a group is treated as an
      *> elementary alphanumeric item, so the numeric operand is moved
      *> by the elementary rule, which drops the sign
      *> (§2023:14.9.25.4 GR6a): -5 becomes "05". The group still holds
      *> the overpunched bytes, whose last or first character is the
      *> digit with the negative sign carried in it, not the plain digit
      *> "5", so the comparison is NOT EQUAL.
      *>
      *> Why each leg can fail: E1 is a trailing embedded sign with the
      *> group as the object, E2 a leading embedded sign with the group
      *> as the subject; each is true only when the item's own bytes are
      *> compared (85). E3 is the unsigned control: P holds 5 and its
      *> redefining group PG holds the same bytes, EQUAL at every
      *> edition, so the difference above comes from the sign alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5132-EMB-GROUP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SE  PIC S99 VALUE -5.
       01  GS REDEFINES SE.
           05  GS1 PIC XX.
       01  SL  PIC S99 SIGN IS LEADING VALUE -5.
       01  GL REDEFINES SL.
           05  GL1 PIC XX.
       01  P   PIC 99 VALUE 5.
       01  PG REDEFINES P.
           05  PG1 PIC XX.
       PROCEDURE DIVISION.
       P1.
           IF SE = GS DISPLAY "E1 EQUAL" ELSE DISPLAY "E1 NOT EQUAL".
           IF GL = SL DISPLAY "E2 EQUAL" ELSE DISPLAY "E2 NOT EQUAL".
           IF P = PG DISPLAY "E3 EQUAL" ELSE DISPLAY "E3 NOT EQUAL".
           STOP RUN.
