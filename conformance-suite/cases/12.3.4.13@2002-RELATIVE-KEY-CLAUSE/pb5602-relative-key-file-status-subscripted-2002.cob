*>--- conformance-case
*> id: pb5602-relative-key-file-status-subscripted-2002
*> rules: [2002-SR-12.3.4.13.2-1, 2002-SR-12.3.4.13.2-3, 2002-SR-12.3.4.7.2-1, 2002-GR-14.8.29.3-22,
*>    2002-GR-14.8.33.3-20, 2002-SR-14.8.37.2-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - at COBOL-2002 the RELATIVE KEY and FILE STATUS clauses take a subscripted data-name.
      *> §2002:12.3.4.13.2 SR1 ("Data-name-1 may be qualified.") and §2002:12.3.4.7.2 SR1 say only that the
      *> operand may be qualified, and §2002:5.1.3 lists "User-defined word, including qualification and
      *> subscription if needed" as what a data-name-n operand is; a table element is referenced by its
      *> subscripts (§2002:8.4.1.2.2 SR5). §2014 added "shall not be subscripted". The subscripts are integer
      *> literals (docs/conformance, D-CLAUSE-SUBSCRIPT).
      *>
      *> The relative key is RK(2), the file status FS(1): the other occurrences must stay untouched.
      *> §2002:14.8.29.3 GR22: "For a relative file, if the RELATIVE KEY clause is specified for file-name-1,
      *> the execution of a READ statement moves the relative record number of the record made available
      *> to the relative key data item according to the rules for the MOVE statement."
      *> cite.py: OK  §2002:14.8.29.3 GR22
      *>
      *> Derived: two records are written at relative record numbers 7 and 9 (RK(2) names the slot of
      *> each WRITE of a file in dynamic access). A sequential READ NEXT makes record 7 available and
      *> moves 7 to RK(2) (PIC 9(2): "07"); the I-O status "00" goes to FS(1). RK(1) and RK(3) were set
      *> to zero and FS(2) to "ZZ" beforehand, and a status or key that landed in the wrong occurrence
      *> would show there. The second READ NEXT makes record 9 available. A random READ with RK(2) = 7
      *> reads the record at slot 7. A random READ at 8 finds no record: invalid key, status "23". START KEY IS
      *> GREATER THAN RK(2) with RK(2) = 8 positions on the first record whose relative record number is greater
      *> (§2002:14.8.37.2 SR5: for a relative file data-name-1 shall be the data item of the RELATIVE KEY clause, here
      *> the occurrence RK(2)), so the next READ NEXT makes record 9 available and moves 9 to RK(2).
      *> §2002:14.8.33.3 GR20: for a file in dynamic access "the operating environment logically replaces the record
      *> identified by the relative key data item": with RK(2) = 9 a REWRITE of "YYY" replaces record 9 ("00"), a
      *> READ at 9 then returns "YYY" and a READ at 7 still returns "AAA" (the other occurrences of RK stay zero).
      *> cite.py: OK  §2002:14.8.33.3 GR20
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-relative-key-subscripted-2002.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK(2)
               FILE STATUS IS FS(1).
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(3).
       WORKING-STORAGE SECTION.
       01 RKT.
          05 RK PIC 9(2) OCCURS 3.
       01 FST.
          05 FS PIC XX OCCURS 2.
       PROCEDURE DIVISION.
           MOVE "ZZ" TO FS(2)
           MOVE 0 TO RK(1) RK(3)
           OPEN OUTPUT F1
           MOVE 7 TO RK(2)
           MOVE "AAA" TO R1
           WRITE R1
           MOVE 9 TO RK(2)
           MOVE "BBB" TO R1
           WRITE R1
           CLOSE F1
           OPEN INPUT F1
           READ F1 NEXT
           DISPLAY "FS=" FS(1) " FS2=" FS(2) " RK=" RK(1) RK(2) RK(3) " R=" R1
           READ F1 NEXT
           DISPLAY "FS=" FS(1) " RK=" RK(1) RK(2) RK(3) " R=" R1
           MOVE 7 TO RK(2)
           READ F1
           DISPLAY "FS=" FS(1) " RK=" RK(1) RK(2) RK(3) " R=" R1
           MOVE 8 TO RK(2)
           READ F1
           DISPLAY "FS=" FS(1) " FS2=" FS(2)
           MOVE 8 TO RK(2)
           START F1 KEY IS GREATER THAN RK(2)
           DISPLAY "S FS=" FS(1)
           READ F1 NEXT
           DISPLAY "FS=" FS(1) " RK=" RK(1) RK(2) RK(3) " R=" R1
           CLOSE F1
           OPEN I-O F1
           MOVE 9 TO RK(2)
           MOVE "YYY" TO R1
           REWRITE R1
           DISPLAY "RW FS=" FS(1)
           MOVE 9 TO RK(2)
           READ F1
           DISPLAY "FS=" FS(1) " R=" R1
           MOVE 7 TO RK(2)
           READ F1
           DISPLAY "FS=" FS(1) " R=" R1
           CLOSE F1
           STOP RUN.
