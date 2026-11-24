*>--- conformance-case
*> id: pb6017_dyn_table_external_2023
*> rules: [GR-14.6.13.2-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6017 - ISO 14.6.13.2 rule 6: "When the internal format of a dynamic-capacity
      *> table, as defined by the implementor, is not correctly formed or does not agree with the
      *> corresponding OCCURS clause an EC-DATA-INCOMPATIBLE exception condition is set to exist."
      *> A dynamic-capacity table in an EXTERNAL record (8.5.1.9.1 3)) is the one table read through
      *> a description other than the one that built it (13.18.22.4 GR1): PB6017GA builds GT with
      *> 2-character elements and capacity 2.
      *> At 2023 EC-EXTERNAL-FORMAT-CONFLICT is checked too (13.18.22.4 GR6, "the records shall define the same number of bytes"):
      *> a record holding a dynamic-capacity table defines no fixed number of bytes, so that rule does not reach the
      *> disagreement and the CALLs complete; the table's own rule above is the one that applies. Were FORMAT-CONFLICT raised at the
      *> CALL, PB6017GA (no declarative) would end the run and B, C and D would never print.
      *> B: PB6017GB describes GT with 3-character elements -> HANDLED, the MOVE does not complete.
      *> C: PB6017GC describes GT with FROM 3, above the current capacity 2 -> HANDLED.
      *> D: PB6017GD describes GT as PB6017GA does -> no condition, W [CD  ].
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6017GA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB6017XG EXTERNAL.
          05 GH PIC X(2).
          05 GT PIC X(2) OCCURS DYNAMIC CAPACITY IN GC.
       PROCEDURE DIVISION.
           MOVE "AB" TO GT(1)
           MOVE "CD" TO GT(2)
           CALL "PB6017GB"
           CALL "PB6017GC"
           CALL "PB6017GD"
           DISPLAY "MAIN-AFTER " GC
           STOP RUN.
       END PROGRAM PB6017GA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6017GB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 PB6017XG EXTERNAL.
          05 GH PIC X(2).
          05 GT PIC X(3) OCCURS DYNAMIC CAPACITY IN GC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DI SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       DI-1.
           DISPLAY "B HANDLED " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-B.
           MOVE "...." TO W
           MOVE GT(1) TO W
           DISPLAY "B [" W "]"
           GOBACK.
       END PROGRAM PB6017GB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6017GC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 PB6017XG EXTERNAL.
          05 GH PIC X(2).
          05 GT PIC X(2) OCCURS DYNAMIC CAPACITY IN GC FROM 3.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DI SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       DI-1.
           DISPLAY "C HANDLED " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-C.
           MOVE "...." TO W
           MOVE GT(2) TO W
           DISPLAY "C [" W "]"
           GOBACK.
       END PROGRAM PB6017GC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6017GD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 PB6017XG EXTERNAL.
          05 GH PIC X(2).
          05 GT PIC X(2) OCCURS DYNAMIC CAPACITY IN GC.
       PROCEDURE DIVISION.
           MOVE "...." TO W
           MOVE GT(2) TO W
           DISPLAY "D [" W "]"
           GOBACK.
       END PROGRAM PB6017GD.
