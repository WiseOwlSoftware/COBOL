      *>--- conformance-case
      *> id: set-index-outside-table-below-2002
      *> rules: [2002-GR-14.8.35.3-2]
      *> source-format: fixed
      *>---
      *> The >>TURN directive is a COBOL-2002 introduction, so
      *> COBOL-85 refuses the program that the 2002 golden
      *> set-index-outside-table-checked accepts (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETIXTBLB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 PIC X OCCURS 10 INDEXED BY IX1.
       01 T2.
          05 E2 PIC X OCCURS 5 INDEXED BY IX2.
       01 N PIC 99.
       01 NX PIC XX.
       01 V PIC 99 VALUE 11.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D1P.
           DISPLAY "EC".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       M SECTION.
       M1.
           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           SET IX1 TO 3.
           SET N TO IX1. MOVE N TO NX. DISPLAY "A=" NX.
           SET IX1 TO V.
           SET N TO IX1. MOVE N TO NX. DISPLAY "B=" NX.
           SET IX1 TO 10.
           SET N TO IX1. MOVE N TO NX. DISPLAY "C=" NX.
           SET IX1 UP BY 1.
           SET N TO IX1. MOVE N TO NX. DISPLAY "D=" NX.
           SET IX1 DOWN BY 9.
           SET N TO IX1. MOVE N TO NX. DISPLAY "E=" NX.
           SET IX1 DOWN BY 1.
           SET N TO IX1. MOVE N TO NX. DISPLAY "F=" NX.
           SET IX1 TO 0.
           SET N TO IX1. MOVE N TO NX. DISPLAY "G=" NX.
           SET IX1 IX2 TO 4.
           SET N TO IX1. MOVE N TO NX. DISPLAY "H=" NX.
           SET N TO IX2. MOVE N TO NX. DISPLAY "I=" NX.
           SET IX1 TO 8.
           SET IX2 TO IX1.
           SET N TO IX2. MOVE N TO NX. DISPLAY "J=" NX.
           STOP RUN.
