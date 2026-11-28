*>--- conformance-case
*> id: pb6304_inline_invocation_ref_mod_in_range
*> rules: [GR-8.4.3.1.4-1, SR-8.4.3.3.3-3, GR-8.4.3.3.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6304 - a reference-modified inline method invocation obeys the rules a reference-modified data
      *> item obeys; this is the legal edge of them, the control of the two refusals beside it
      *> (pb6304-inline-invocation-ref-mod-twice, pb6304-inline-invocation-ref-mod-out-of-range).
      *> 8.4.3.1.4 GR1g: a reference modifier applies to the identifier on its left, here the invocation, whose value
      *> is the 8.4.3.4.4 GR1c temporary with the description of GETNAME's RETURNING item, PIC X(8). 8.4.3.3.3 SR3:
      *> "Identifier-1 shall not be a reference-modification format identifier." (cite.py OK) - one modifier is
      *> legal. 8.4.3.3.4 GR5b: leftmost-position names a position of the item (1 to 8), GR5c: "The sum of
      *> leftmost-position and length minus the value one shall be less than or equal to the number of positions in
      *> the data item referenced by identifier-1" (cite.py OK).
      *> Expected values, DERIVED: GETNAME returns "ACCOUNT" in PIC X(8), so the item is ACCOUNT and a space.
      *>   A  (1:3)  positions 1 to 3, the first position                        -> ACC
      *>   B  (6:3)  positions 6 to 8, the last position is the 8th (6 + 3 - 1)  -> "NT" and a space
      *>   C  (8:1)  the last position alone                                     -> a space
      *> WHY EACH LEG FAILS. A screen that counted the item as 7 positions (the literal's length) refuses B and C;
      *> one that refuses a start equal to the size refuses C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6304LEGAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB6304ACC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB6304ACC.
       01 W3 PIC X(3).
       01 W1 PIC X.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB6304ACC "NEW" RETURNING A1.
           MOVE A1 :: "GETNAME" (1:3) TO W3.
           DISPLAY "A=" W3 "|".
           MOVE A1 :: "GETNAME" (6:3) TO W3.
           DISPLAY "B=" W3 "|".
           MOVE A1 :: "GETNAME" (8:1) TO W1.
           DISPLAY "C=" W1 "|".
           STOP RUN.
       END PROGRAM PB6304LEGAL.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB6304ACC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB6304ACC.
