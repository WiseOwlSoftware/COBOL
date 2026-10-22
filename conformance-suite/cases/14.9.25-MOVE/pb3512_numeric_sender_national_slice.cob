      *>--- conformance-case
      *> id: pb3512_numeric_sender_national_slice
      *> rules: [GR-14.9.25.4-6, GR-8.4.3.3.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3512, the NATIONAL receiver. A reference-modified
      *> national item is an elementary item of class and category
      *> national (ISO §8.4.3.3.4 GR6, base case):
      *>   cite.py --check 8.4.3.3.4 "The unique data item has the same
      *>   class, category, and usage as that defined for identifier-1"
      *>   -> OK  §8.4.3.3.4 6)  (General rules)
      *> so a MOVE into it is §14.9.25.4 GR6's elementary move, as a
      *> MOVE into the whole national item is:
      *>   cite.py --check 14.9.25.4 "Any necessary conversion from
      *>   alphanumeric character to national character representation
      *>   shall be performed, before any alignment"
      *>   -> OK  §14.9.25.4 6)  (General rules)
      *>   cite.py --check 14.9.25.4 "If the sending operand is
      *>   described as being signed numeric, the operational sign is
      *>   not moved"  -> OK  §14.9.25.4 6)  (General rules)
      *> Usage national is new in ISO 2002, so the test starts there.
      *> The device form of a national item is the implementor's
      *> (§14.9.11.4 GR1), so each result is compared with the national
      *> literal the rules derive and the relation's truth is shown.
      *> EXPECTED OUTPUT, derived line by line (N6 starts all '*'):
      *>   N  S9(4) -2468 into N6(1:5): the digits only, space filled,
      *>      N6 = N"2468 *": T. (Before the fix the sign moved.)
      *>   M  "AB" into N6(2:3), converted to national and space
      *>      filled: N6 = N"2AB  *": T.
      *>   L  SIGN LEADING SEPARATE -1357 into N6(2:4): the sign
      *>      character is not moved, the size is four: N"21357*": T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3512RMNAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SN PIC S9(4) VALUE -2468.
       01 L4 PIC S9(4) SIGN LEADING SEPARATE VALUE -1357.
       01 N6 PIC N(6) VALUE ALL N"*".
       PROCEDURE DIVISION.
           MOVE SN TO N6(1:5)
           IF N6 = N"2468 *" DISPLAY "N T" ELSE DISPLAY "N F" END-IF
           MOVE "AB" TO N6(2:3)
           IF N6 = N"2AB  *" DISPLAY "M T" ELSE DISPLAY "M F" END-IF
           MOVE L4 TO N6(2:4)
           IF N6 = N"21357*" DISPLAY "L T" ELSE DISPLAY "L F" END-IF
           STOP RUN.
