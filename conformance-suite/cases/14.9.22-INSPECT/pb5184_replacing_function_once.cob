      *>--- conformance-case
      *> id: pb5184_replacing_function_once
      *> rules: [GR-14.9.22.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5184 - a function-identifier in the REPLACING half of a format 3 INSPECT is evaluated once, as the first
      *> operation, before the TALLYING half stores its counter.
      *>
      *> §2023:14.9.22.4 GR6 (checked with scripts/spec/cite.py --check): "Item identification for any identifier is done
      *> only once as the first operation in the execution of the INSPECT statement" (§1985:VI.6.18.4 GR5, checked: "If
      *> any identifier is subscripted or is a function-identifier, the subscript or function-identifier is evaluated only
      *> once as the first operation in the execution of the INSPECT statement").  §2023:14.6.4 step 2 puts function
      *> evaluation, and step 4 subscript evaluation, inside item identification.
      *>
      *> L1: D4 = "XAXB", I = 1, LIT = "abb".  The pattern FUNCTION UPPER-CASE (LIT (I)) is identified first with I = 1:
      *>     LIT (1) = "a", so the pattern is "A".  TALLYING I FOR ALL "X" adds 2: I = 3.  REPLACING ALL "A" BY "Z"
      *>     turns the A of XAXB into Z: "XZXB".  (Evaluated after the tally, LIT (3) = "b" gives the pattern "B": "XAXZ".)
      *> L2: the same for the REPLACEMENT operand: D4 = "XAXB", I = 1.  REPLACING ALL "X" BY FUNCTION UPPER-CASE (LIT (I))
      *>     identifies LIT (1) = "a" first, so the replacement is "A": "AAAB", with I = 3 from the tally.  (Evaluated after
      *>     the tally the replacement would be "B": "BABB".)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5184FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D4  PIC X(4).
       01  I   PIC 9.
       01  LIT-TABLE.
           05  LIT PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "abb" TO LIT-TABLE.
           MOVE "XAXB" TO D4.
           MOVE 1 TO I.
           INSPECT D4 TALLYING I FOR ALL "X"
               REPLACING ALL FUNCTION UPPER-CASE (LIT (I)) BY "Z".
           DISPLAY "PATTERN " I " " D4.
           MOVE "XAXB" TO D4.
           MOVE 1 TO I.
           INSPECT D4 TALLYING I FOR ALL "X"
               REPLACING ALL "X" BY FUNCTION UPPER-CASE (LIT (I)).
           DISPLAY "REPLACEMENT " I " " D4.
           STOP RUN.
