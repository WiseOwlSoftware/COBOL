      *>--- conformance-case
      *> id: g85_bf03_03
      *> rules: [1985-GR-VI.4.5.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.4 GR7 - HIGH-VALUE and LOW-VALUE written as
      *> literals in the SPECIAL-NAMES paragraph are the characters with
      *> the highest and lowest positions in the NATIVE collating
      *> sequence. AL lists "B" HIGH-VALUE "A" LOW-VALUE, so native
      *> ordinal 65536 (NHI, §1985:VI.4.5.4 GR8) takes position 2 and
      *> native ordinal 1 (NLO) position 4 of the program collating
      *> sequence; ORD and the comparisons fail if either figurative
      *> constant resolved to any other character.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0303.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. WISEOWL
           PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "B" HIGH-VALUE "A" LOW-VALUE
           SYMBOLIC CHARACTERS NHI NLO ARE 65536 1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  XH PIC X.
       01  XL PIC X.
       01  XA PIC X VALUE "A".
       01  N5 PIC 9(5).
       PROCEDURE DIVISION.
       P1.
           MOVE NHI TO XH.
           MOVE NLO TO XL.
           COMPUTE N5 = FUNCTION ORD(XH).
           DISPLAY "ORD-NHI=" N5.
           COMPUTE N5 = FUNCTION ORD(XL).
           DISPLAY "ORD-NLO=" N5.
           COMPUTE N5 = FUNCTION ORD(XA).
           DISPLAY "ORD-A=" N5.
           IF XH < XA
               DISPLAY "NHI-LT-A"
           ELSE
               DISPLAY "NHI-NOT-LT-A".
           IF XL > XA
               DISPLAY "NLO-GT-A"
           ELSE
               DISPLAY "NLO-NOT-GT-A".
           DISPLAY "LV=" LOW-VALUE.
           STOP RUN.
