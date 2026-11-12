      *>--- conformance-case
      *> id: pb5162_ord_numeric_edited_85
      *> rules: [1989-AR-A.2.31.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5162 - the 1989 ORD function asks for CLASS
      *> alphanumeric, which holds the numeric edited category.
      *>
      *> §1989:A.2.31.3 AR1 (cite.py --check): "Argument-1 must be
      *> one character in length and must be class alphabetic or
      *> alphanumeric"; §1985:IV.4.3.3: "The alphanumeric class
      *> includes the categories of alphanumeric edited, numeric
      *> edited, and alphanumeric (without editing)". A one-position
      *> PIC Z or PIC * item is numeric edited, so class
      *> alphanumeric, and is an ORD argument; §1989:A.2.31.4: the
      *> value is its ordinal position in the program collating
      *> sequence, here the native one (the code of the character
      *> plus 1, as ORD ("A") is 66). Z1 holds "7" (56), Z2 the
      *> check-protect "*" of a zero (43) and Z3 the space a
      *> zero-suppressed zero leaves (33). The program fails to
      *> compile if the 2002 CATEGORY rule (§2023:15.70.3 AR1;
      *> pb658-ord-numeric-edited-argument) is applied at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5162A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9(3).
       01 Z1 PIC Z.
       01 Z2 PIC *.
       01 Z3 PIC Z.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 7 TO Z1.
           MOVE 0 TO Z2.
           MOVE 0 TO Z3.
           COMPUTE N = FUNCTION ORD (Z1).
           DISPLAY N.
           COMPUTE N = FUNCTION ORD (Z2).
           DISPLAY N.
           COMPUTE N = FUNCTION ORD (Z3).
           DISPLAY N.
           STOP RUN.
