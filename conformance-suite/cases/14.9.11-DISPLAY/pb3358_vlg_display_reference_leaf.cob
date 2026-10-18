*>--- conformance-case
*> id: pb3358_vlg_display_reference_leaf
*> rules: [GR-14.9.11.4-7, DOC-A.1-57, DOC-A.1-214]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3358 - DISPLAY and FUNCTION LENGTH of a STRONGLY-TYPED VARIABLE-LENGTH GROUP that holds an
      *> object-reference leaf, in every shape a variable-length group takes.
      *>
      *> ISO 1989:2023 14.9.11.4 GR7: "If identifier-1 references a variable-length group, the format in which its
      *> contents are displayed is defined by the implementor." The implementor's format is docs/CONFORMANCE.md
      *> DOC-A.1-57: the members' images concatenated in declaration order, each dynamic member at its CURRENT
      *> extent. 13.18.60.3 SR14 admits an object reference as an elementary item subordinate to a type declaration
      *> with the STRONG phrase, so a variable-length group can hold one; its 8 positions are the D-SLOT placeholder
      *> of DOC-A.1-214 - EIGHT SPACES, never the reference - exactly as in a fixed-length group (DOC-A.1-56).
      *> 15.50.4 rule 7: FUNCTION LENGTH of a variable-length group is the sum of its members' current extents, the
      *> reference counting its 8 positions (13.18.60.4 GR22 a), so LENGTH equals the displayed width.
      *> Before the fix every DISPLAY below compiled clean and aborted at run time with the Tier-C
      *> "pointer/object-class leaf" refusal.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION - NOT FROM A RUN (S8 = the 8-space placeholder):
      *>   G1  A "hello" + 3 refs (24) + N "5"                          width 5 + 24 + 1      = 30
      *>   G2  A "ab" + CNT "2" + two ODO occurrences (EA + S8)         width 2 + 1 + 2 x 10  = 23
      *>   G3  A "cd" + two dynamic-capacity occurrences (EA + S8)      width 2 + 2 x 10      = 22
      *>   G4  A "ef" + 2 refs (16)                                     width 2 + 16          = 18
      *>   G5  Q1 "z" + two fixed occurrences (own dynamic WA + S8)     width 1 + 9 + 10      = 20
      *>   G6  Q1 "z" + CNT6 "2" + two ODO occurrences of variable-length elements  1 + 1 + 9 + 10 = 21
      *>   G7  Q1 "w" + two dynamic-capacity occurrences of variable-length elements 1 + 9 + 10     = 20
      *>   GD  A "abc" + a nested FIXED strong group (EA "xy" + S8) + DN "07"   width 3 + 10 + 2    = 15
      *>   E5/E6/E7 one occurrence of G5/G6/G7 (itself a variable-length group)  10 / 9 / 10
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3358A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3358C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TE IS TYPEDEF STRONG.
          05 EA PIC X(2).
          05 ER USAGE OBJECT REFERENCE PB3358C.
       01 TW IS TYPEDEF STRONG.
          05 WA PIC X DYNAMIC LENGTH LIMIT IS 4.
          05 WR USAGE OBJECT REFERENCE PB3358C.
       01 T1 IS TYPEDEF STRONG.
          05 A PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 RT USAGE OBJECT REFERENCE PB3358C OCCURS 3.
          05 N PIC 9.
       01 G1 TYPE T1.
       01 T2 IS TYPEDEF STRONG.
          05 A PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 CNT PIC 9.
          05 EL TYPE TE OCCURS 1 TO 3 DEPENDING ON CNT.
       01 G2 TYPE T2.
       01 T3 IS TYPEDEF STRONG.
          05 A PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 DT TYPE TE OCCURS DYNAMIC CAPACITY IN CAP.
       01 G3 TYPE T3.
       01 T4 IS TYPEDEF STRONG.
          05 A PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 DR USAGE OBJECT REFERENCE PB3358C OCCURS 2.
       01 G4 TYPE T4.
       01 T5 IS TYPEDEF STRONG.
          05 Q1 PIC X.
          05 FT TYPE TW OCCURS 2.
       01 G5 TYPE T5.
       01 T6 IS TYPEDEF STRONG.
          05 Q1 PIC X.
          05 CNT6 PIC 9.
          05 OT TYPE TW OCCURS 1 TO 3 DEPENDING ON CNT6.
       01 G6 TYPE T6.
       01 T7 IS TYPEDEF STRONG.
          05 Q1 PIC X.
          05 DTW TYPE TW OCCURS DYNAMIC CAPACITY IN CAP7.
       01 G7 TYPE T7.
       01 TD IS TYPEDEF STRONG.
          05 DA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 DF TYPE TE.
          05 DN PIC 9(2).
       01 GD TYPE TD.
       01 WS-L PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "hello" TO A OF G1
           MOVE 5 TO N OF G1
           DISPLAY "G1=[" G1 "]"
           MOVE FUNCTION LENGTH (G1) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "ab" TO A OF G2
           MOVE 2 TO CNT OF G2
           MOVE "x1" TO EA OF EL OF G2 (1)
           MOVE "x2" TO EA OF EL OF G2 (2)
           DISPLAY "G2=[" G2 "]"
           MOVE FUNCTION LENGTH (G2) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "cd" TO A OF G3
           MOVE "y1" TO EA OF DT OF G3 (1)
           MOVE "y2" TO EA OF DT OF G3 (2)
           DISPLAY "G3=[" G3 "]"
           MOVE FUNCTION LENGTH (G3) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "ef" TO A OF G4
           DISPLAY "G4=[" G4 "]"
           MOVE FUNCTION LENGTH (G4) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "z" TO Q1 OF G5
           MOVE "p" TO WA OF FT OF G5 (1)
           MOVE "qq" TO WA OF FT OF G5 (2)
           DISPLAY "G5=[" G5 "]"
           MOVE FUNCTION LENGTH (G5) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "z" TO Q1 OF G6
           MOVE 2 TO CNT6 OF G6
           MOVE "p" TO WA OF OT OF G6 (1)
           MOVE "qq" TO WA OF OT OF G6 (2)
           DISPLAY "G6=[" G6 "]"
           MOVE FUNCTION LENGTH (G6) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "w" TO Q1 OF G7
           MOVE "m" TO WA OF DTW OF G7 (1)
           MOVE "nn" TO WA OF DTW OF G7 (2)
           DISPLAY "G7=[" G7 "]"
           MOVE FUNCTION LENGTH (G7) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "abc" TO DA OF GD
           MOVE "xy" TO EA OF DF OF GD
           MOVE 7 TO DN OF GD
           DISPLAY "GD=[" GD "]"
           MOVE FUNCTION BYTE-LENGTH (GD) TO WS-L
           DISPLAY "LEN " WS-L
           DISPLAY "E5=[" FT OF G5 (2) "]"
           MOVE FUNCTION LENGTH (FT OF G5 (2)) TO WS-L
           DISPLAY "LEN " WS-L
           DISPLAY "E6=[" OT OF G6 (1) "]"
           MOVE FUNCTION LENGTH (OT OF G6 (1)) TO WS-L
           DISPLAY "LEN " WS-L
           DISPLAY "E7=[" DTW OF G7 (2) "]"
           MOVE FUNCTION LENGTH (DTW OF G7 (2)) TO WS-L
           DISPLAY "LEN " WS-L
           STOP RUN.
       END PROGRAM PB3358A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3358C.
       OBJECT.
       END OBJECT.
       END CLASS PB3358C.
