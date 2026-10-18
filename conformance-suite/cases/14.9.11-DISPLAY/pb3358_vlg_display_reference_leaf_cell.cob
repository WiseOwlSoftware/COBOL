*>--- conformance-case
*> id: pb3358_vlg_display_reference_leaf_cell
*> rules: [GR-14.9.11.4-7, DOC-A.1-57, DOC-A.1-214]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3358 - the CELL-BACKED twin of pb3358_vlg_display_reference_leaf: DISPLAY and FUNCTION LENGTH of
      *> a strongly-typed VARIABLE-LENGTH GROUP that lives in a cell (an EXTERNAL record, an ADDRESS-OF-taken record)
      *> and holds an object-reference leaf.
      *>
      *> ISO 1989:2023 14.9.11.4 GR7: "If identifier-1 references a variable-length group, the format in which its
      *> contents are displayed is defined by the implementor." DOC-A.1-57: the cell composes the same format a
      *> declared group does, and the reference's 8 positions of the cell's fixed run are the D-SLOT placeholder of
      *> DOC-A.1-214 - EIGHT SPACES (S8) - the characters a declared group shows (DOC-A.1-56). The strong type of an
      *> EXTERNAL record is itself external (13.18.22.3 SR5; a COBOL-2023 feature), so its table elements are described inline.
      *> Before the fix each DISPLAY compiled clean and aborted at run time with the Tier-C "pointer/object-class
      *> leaf" refusal.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION - NOT FROM A RUN:
      *>   X1  EXTERNAL  A "hello" + 3 refs (24) + N "5"                       width 5 + 24 + 1      = 30
      *>   X2  EXTERNAL  A "ab" + CNT "2" + two ODO occurrences (EA + S8)      width 2 + 1 + 2 x 10  = 23
      *>   X3  EXTERNAL  A "cd" + two dynamic-capacity occurrences (EA + S8)   width 2 + 2 x 10      = 22
      *>   X5  EXTERNAL  Q1 "z" + two fixed occurrences (own dynamic WA + S8)  width 1 + 9 + 10      = 20
      *>   X6  EXTERNAL  Q1 "z" + CNT6 "2" + two ODO occurrences of variable-length elements  1 + 1 + 9 + 10 = 21
      *>   X7  EXTERNAL  Q1 "w" + two dynamic-capacity occurrences of variable-length elements 1 + 9 + 10     = 20
      *>   AG  the ADDRESS OF a declared group   A "adr" + one ref (S8) + N "05"                  3 + 8 + 2 = 13
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3358B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3358D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF STRONG EXTERNAL.
          05 A PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 RT USAGE OBJECT REFERENCE PB3358D OCCURS 3.
          05 N PIC 9.
       01 X1 TYPE T1 EXTERNAL.
       01 T2 IS TYPEDEF STRONG EXTERNAL.
          05 A PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 CNT PIC 9.
          05 EL OCCURS 1 TO 3 DEPENDING ON CNT.
             10 EA PIC X(2).
             10 ER USAGE OBJECT REFERENCE PB3358D.
       01 X2 TYPE T2 EXTERNAL.
       01 T3 IS TYPEDEF STRONG EXTERNAL.
          05 A PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 DT OCCURS DYNAMIC CAPACITY IN CAP.
             10 EA PIC X(2).
             10 ER USAGE OBJECT REFERENCE PB3358D.
       01 X3 TYPE T3 EXTERNAL.
       01 T5 IS TYPEDEF STRONG EXTERNAL.
          05 Q1 PIC X.
          05 FT OCCURS 2.
             10 WA PIC X DYNAMIC LENGTH LIMIT IS 4.
             10 WR USAGE OBJECT REFERENCE PB3358D.
       01 X5 TYPE T5 EXTERNAL.
       01 T6 IS TYPEDEF STRONG EXTERNAL.
          05 Q1 PIC X.
          05 CNT6 PIC 9.
          05 OT OCCURS 1 TO 3 DEPENDING ON CNT6.
             10 WA PIC X DYNAMIC LENGTH LIMIT IS 4.
             10 WR USAGE OBJECT REFERENCE PB3358D.
       01 X6 TYPE T6 EXTERNAL.
       01 T7 IS TYPEDEF STRONG EXTERNAL.
          05 Q1 PIC X.
          05 DTW OCCURS DYNAMIC CAPACITY IN CAP7.
             10 WA PIC X DYNAMIC LENGTH LIMIT IS 4.
             10 WR USAGE OBJECT REFERENCE PB3358D.
       01 X7 TYPE T7 EXTERNAL.
       01 TG IS TYPEDEF STRONG.
          05 GA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 GR USAGE OBJECT REFERENCE PB3358D.
          05 GN PIC 9(2).
       01 TPT IS TYPEDEF USAGE POINTER TO TG.
       01 PA TYPE TPT.
       01 AG TYPE TG.
       01 WS-L PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "hello" TO A OF X1
           MOVE 5 TO N OF X1
           DISPLAY "X1=[" X1 "]"
           MOVE FUNCTION LENGTH (X1) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "ab" TO A OF X2
           MOVE 2 TO CNT OF X2
           MOVE "x1" TO EA OF EL OF X2 (1)
           MOVE "x2" TO EA OF EL OF X2 (2)
           DISPLAY "X2=[" X2 "]"
           MOVE FUNCTION LENGTH (X2) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "cd" TO A OF X3
           MOVE "y1" TO EA OF DT OF X3 (1)
           MOVE "y2" TO EA OF DT OF X3 (2)
           DISPLAY "X3=[" X3 "]"
           MOVE FUNCTION LENGTH (X3) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "z" TO Q1 OF X5
           MOVE "p" TO WA OF FT OF X5 (1)
           MOVE "qq" TO WA OF FT OF X5 (2)
           DISPLAY "X5=[" X5 "]"
           MOVE FUNCTION LENGTH (X5) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "z" TO Q1 OF X6
           MOVE 2 TO CNT6 OF X6
           MOVE "p" TO WA OF OT OF X6 (1)
           MOVE "qq" TO WA OF OT OF X6 (2)
           DISPLAY "X6=[" X6 "]"
           MOVE FUNCTION LENGTH (X6) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "w" TO Q1 OF X7
           MOVE "m" TO WA OF DTW OF X7 (1)
           MOVE "nn" TO WA OF DTW OF X7 (2)
           DISPLAY "X7=[" X7 "]"
           MOVE FUNCTION LENGTH (X7) TO WS-L
           DISPLAY "LEN " WS-L
           SET PA TO ADDRESS OF AG
           MOVE "adr" TO GA OF AG
           MOVE 5 TO GN OF AG
           DISPLAY "AG=[" AG "]"
           MOVE FUNCTION BYTE-LENGTH (AG) TO WS-L
           DISPLAY "LEN " WS-L
           STOP RUN.
       END PROGRAM PB3358B.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3358D.
       OBJECT.
       END OBJECT.
       END CLASS PB3358D.
