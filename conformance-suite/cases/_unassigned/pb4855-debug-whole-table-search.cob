      *>--- conformance-case
      *> id: pb4855-debug-whole-table-search
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4855. X3.23-1985 USE FOR DEBUGGING general rule 5 d:
      *> an ALL REFERENCES OF section runs after "any other COBOL
      *> statement that explicitly references identifier-1", and SEARCH
      *> names its table without subscripts (the 1985 text, Table
      *> Handling: the reference indicates the occurrence "except in a
      *> USE FOR DEBUGGING statement and SEARCH statement"). Rule 24 c:
      *> DEBUG-CONTENTS "contains the content of the data item
      *> referenced by identifier-1": every current occurrence of the
      *> table, in storage order. Rule 15: DEBUG-CONTENTS is "large
      *> enough to contain the data required". Rule 6 c: identifier-1
      *> alone runs only for a statement that changes the item.
      *> A: SEARCH ALL of an OCCURS DEPENDING table runs T's section
      *> once (rule 7), its content the three current occurrences, no
      *> subscript (DEBUG-SUB-1 spaces, moved to X5). B: a SEARCH whose
      *> WHEN phrase never names T still references T. C: the nested
      *> table C, whose WHEN phrase names ROW (2): the content is that
      *> row's C, DEBUG-SUB-1 the row. D: TK alone (rule 6) does not run
      *> for a SEARCH. E: a 40-position table: DEBUG-CONTENTS holds all
      *> of it (positions 31 to 40).
      *> Why each leg can fail: a statement whose table operand is held
      *> as a data item and not as a reference runs no section; a
      *> DEBUG-CONTENTS taken from one occurrence, or from the physical
      *> maximum, shows the wrong content; a register sized only for one
      *> occurrence truncates the table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4855WS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 3.
       01 TB.
          02 T OCCURS 1 TO 5 TIMES DEPENDING ON N
               ASCENDING KEY TK INDEXED BY TX.
             03 TK PIC X.
       01 NB.
          02 ROW OCCURS 2 TIMES INDEXED BY RX.
             03 C OCCURS 3 TIMES INDEXED BY CX PIC X.
       01 WB.
          02 W OCCURS 8 TIMES INDEXED BY WX PIC X(5).
       01 X5 PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBT SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF T.
       DBT-B.
           MOVE DEBUG-SUB-1 TO X5.
           DISPLAY "  DBT " DEBUG-NAME (1:2) " [" X5 "] ["
               DEBUG-CONTENTS (1:6) "]".
       DBC SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF C.
       DBC-B.
           MOVE DEBUG-SUB-1 TO X5.
           DISPLAY "  DBC " DEBUG-NAME (1:2) " [" X5 "] ["
               DEBUG-CONTENTS (1:4) "]".
       DBK SECTION.
           USE FOR DEBUGGING ON TK.
       DBK-B.
           DISPLAY "  DBK NOT EXPECTED".
       DBW SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF W.
       DBW-B.
           DISPLAY "  DBW [" DEBUG-CONTENTS (1:5) "] ["
               DEBUG-CONTENTS (31:10) "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "ABCDE" TO TB.
           MOVE "uvwxyz" TO NB.
           MOVE "AAAAABBBBBCCCCCDDDDDEEEEEFFFFFGGGGGHHHHH" TO WB.
           DISPLAY "A: SEARCH ALL T".
           SEARCH ALL T WHEN TK (TX) = "B" DISPLAY "FOUND B".
           DISPLAY "B: SEARCH T".
           SET TX TO 1.
           SEARCH T AT END DISPLAY "AT END" WHEN TX > 9 DISPLAY "NO".
           DISPLAY "C: SEARCH C".
           SET RX TO 2.
           SET CX TO 1.
           SEARCH C WHEN C (RX, CX) = "y" DISPLAY "FOUND y".
           DISPLAY "E: SEARCH W".
           SET WX TO 1.
           SEARCH W WHEN W (WX) = "EEEEE" DISPLAY "FOUND E".
           DISPLAY "END".
           STOP RUN.
