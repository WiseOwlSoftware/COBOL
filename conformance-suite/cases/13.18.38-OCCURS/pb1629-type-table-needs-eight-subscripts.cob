*>--- conformance-case
*> id: pb1629-type-table-needs-eight-subscripts
*> rules: [SR-13.18.38.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1629 - ISO 13.18.38.3 SR10: an OCCURS clause may be
      *>   subordinate to another "as long as the number of subscripts
      *>   required does not exceed seven"; the TYPE clause's effect is
      *>   "as though the data description identified by type-name-1 had
      *>   been coded in place" (13.18.57.4 GR1). TT holds ONE table, which
      *>   is legal in its declaration, and the subject G sits under seven
      *>   tables, so the copied table T is the EIGHTH dimension. The
      *>   composed entry is refused, at the entry that wrote the TYPE
      *>   clause. Control: pb1629_type_table_at_seven_subscripts_is_legal.
      *> cite.py --check 13.18.38.3 "as long as the number of subscripts
      *>   required does not exceed seven" -> OK  13.18.38.3 10)
      *> cite.py --check 13.18.57.4 "as though the data description
      *>   identified by type-name-1 had been coded in" -> OK  13.18.57.4 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1629A.
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
                08 A7 OCCURS 2.
                 09 G TYPE TT.
       PROCEDURE DIVISION.
           STOP RUN.
