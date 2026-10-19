*>--- conformance-case
*> id: pb1629_type_table_at_seven_subscripts_is_legal
*> rules: [SR-13.18.38.3-10]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1629 - ISO 13.18.38.3 SR10: tables may nest "as long as
      *>   the number of subscripts required does not exceed seven". The
      *>   subject G of the TYPE clause sits under SIX tables and TT holds
      *>   one, so the copied table T is the SEVENTH dimension - legal (the
      *>   control for pb1629-type-table-needs-eight-subscripts).
      *> cite.py --check 13.18.38.3 "as long as the number of subscripts
      *>   required does not exceed seven" -> OK  13.18.38.3 10)
      *> Derivation: the program compiles; MOVE "Z" TO the (1 2 1 2 1 2 1)
      *>   occurrence of T, and reading it back gives Z.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1629B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF.
          02 T PIC X OCCURS 2.
       01 R.
          02 A1 OCCURS 2.
           03 A2 OCCURS 2.
            04 A3 OCCURS 2.
             05 A4 OCCURS 2.
              06 A5 OCCURS 2.
               07 A6 OCCURS 2.
                08 G TYPE TT.
       PROCEDURE DIVISION.
           MOVE "Z" TO T (1 2 1 2 1 2 1)
           DISPLAY "T=" T (1 2 1 2 1 2 1)
           STOP RUN.
