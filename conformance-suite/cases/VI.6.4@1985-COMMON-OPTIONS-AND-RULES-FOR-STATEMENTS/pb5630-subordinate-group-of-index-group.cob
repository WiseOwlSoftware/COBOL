      *>--- conformance-case
      *> id: pb5630-subordinate-group-of-index-group
      *> rules: [1985-GR-VI.6.4.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.3 GR3: "The description of D1 and D2 must not contain
      *> level-number 66, 77, or 88 or the USAGE IS INDEX clause." The USAGE
      *> clause written at a group level "applies to each elementary item in
      *> the group" (§1985:VI.5.14.4 GR1), not to a subordinate group: the
      *> entry of SUB below has no USAGE clause, so SUB is a legal operand of
      *> CORRESPONDING at 85 (kb/Work PB5630). No pair corresponds, so each
      *> statement transfers nothing and the program displays OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5630.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GI USAGE IS INDEX.
           05  SUB.
               10  Y1.
       01  G2.
           05  Z1 PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           ADD CORRESPONDING SUB OF GI TO G2.
           MOVE CORRESPONDING SUB OF GI TO G2.
           SUBTRACT CORRESPONDING G2 FROM SUB OF GI.
           DISPLAY "OK".
           STOP RUN.
