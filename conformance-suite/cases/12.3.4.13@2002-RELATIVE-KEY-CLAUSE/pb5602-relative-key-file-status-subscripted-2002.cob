*>--- conformance-case
*> id: pb5602-relative-key-file-status-subscripted-2002
*> rules: [2002-SR-12.3.4.13.2-1, 2002-SR-12.3.4.13.2-3, 2002-SR-12.3.4.7.2-1, 2002-GR-14.8.29.3-22]
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
      *> reads the record at slot 7. A random READ at 8 finds no record: invalid key, status "23".
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
           CLOSE F1
           STOP RUN.
