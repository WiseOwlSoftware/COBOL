      *>--- conformance-case
      *> id: g2-replace-debugging-line-matching-no-mode
      *> rules: [2002-SR-7.1.3.2-10, 2002-GR-7.1.3.3-8,
      *>    2002-GR-7.1.3.3-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> No WITH DEBUGGING MODE. §2002:6.2.7 "After all COPY and
      *> REPLACE statements have been processed, a debugging line has
      *> the characteristics of a comment line if the WITH DEBUGGING
      *> MODE clause is not specified": while REPLACE is processed a
      *> D line is text, not a comment.
      *> LEG 1 - §2002:7.1.3.2 SR10 "Pseudo-text-1 and pseudo-text-2
      *> may contain debugging lines": pseudo-text-1 "L1" "L2" ends on
      *> a D line. §2002:7.1.3.3 GR8c.6 "A debugging indicator shall be
      *> treated as if it did not appear in the line. Text words
      *> within a debugging line participate in the matching", so
      *> pseudo-text-1 is the two words "L1" "L2" and matches the
      *> source "L1" "L2": prints R1Z. A pseudo-text-1 that dropped
      *> the D line would print R1L2Z; refusing the D line fails the
      *> compile.
      *> LEG 2 - §2002:7.1.3.3 GR11 "If the text being replaced begins
      *> on a debugging line, the resultant text is on a debugging
      *> line": the source "L3" (D line) "L4" (normal line) matches
      *> (GR8c6) and begins on a D line, so "R3" "R4" land on a
      *> debugging line, a comment after REPLACE (6.2.7): prints AZ.
      *> No match prints AL4Z; a result placed on a normal line
      *> prints AR3R4Z.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPL06.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           REPLACE =="L1"
      D    "L2"== BY =="R1"==
                   =="L3" "L4"== BY =="R3"
                   "R4"==.
           DISPLAY "L1" "L2" "Z".
           DISPLAY "A"
      D    "L3"
           "L4"
           "Z".
           STOP RUN.
