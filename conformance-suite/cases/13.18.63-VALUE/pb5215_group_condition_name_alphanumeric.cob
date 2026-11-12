      *>--- conformance-case
      *> id: pb5215_group_condition_name_alphanumeric
      *> rules: [SR-13.18.63.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5215 - the legal side. An alphanumeric group item has class
      *> and category alphanumeric (§2023:8.5.2.1), and a condition-name takes its conditional
      *> variable's characteristics (§2023:13.18.63.4 GR19; §1985:VI.5.15.5 GR1),
      *> so §2023:13.18.63.3 SR4 sentence 1 ("literals in the VALUE clause shall
      *> be alphanumeric literals", cite.py --check OK) admits the nonnumeric
      *> literals below on the group G of four characters, a THRU range among
      *> them. DERIVATION: after SET G-AB TO TRUE the group holds "AB  "
      *> (aligned left, space filled), so G-AB is true, G-RNG (VALUE "AA"
      *> THRU "AZ", alphanumeric compare of "AB  " against each end after
      *> space extension) is true and G-CD is false.
      *> Fails if: a nonnumeric literal on a group's condition-name is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5215OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           88 G-AB VALUE "AB".
           88 G-CD VALUE "CD".
           88 G-RNG VALUE "AA" THRU "AZ".
           05 A1 PIC X(2).
           05 A2 PIC X(2).
       PROCEDURE DIVISION.
       P1.
           SET G-AB TO TRUE.
           IF G-AB DISPLAY "G-AB TRUE" ELSE DISPLAY "G-AB FALSE".
           IF G-CD DISPLAY "G-CD TRUE" ELSE DISPLAY "G-CD FALSE".
           IF G-RNG DISPLAY "G-RNG TRUE" ELSE DISPLAY "G-RNG FALSE".
           DISPLAY "G [" G "]".
           STOP RUN.
