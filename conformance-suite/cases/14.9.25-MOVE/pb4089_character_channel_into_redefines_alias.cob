*>--- conformance-case
*> id: pb4089_character_channel_into_redefines_alias
*> rules: [GR-14.9.25.4-4, GR-14.9.22.4-7]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4089 review round 1 (R1-1) - a CHARACTER CHANNEL into the REDEFINING item of a pair of numeric items
      *>   reaches the storage both items name. The redefining item owns no storage of its own.
      *> cite.py --check 13.18.44.1 "The REDEFINES clause allows the same computer storage area to be described
      *>   by different data description entries." -> OK  13.18.44.1
      *> cite.py --check 14.9.25.4 "treated exactly as if it were an alphanumeric to alphanumeric elementary
      *>   move" -> OK  14.9.25.4 4)
      *> cite.py --check 14.9.22.4 "each properly matched occurrence of literal-1 is tallied (format 1) or
      *>   replaced by literal-3 (format 2)" -> OK  14.9.22.4 7)
      *> Derivation (each pair is read through a reference modification, which is an alphanumeric item over the
      *>   item's character positions, so no numeric reading intervenes):
      *>   A MOVE GR TO G2: GR is a group holding the characters WXYZ, so the move is an alphanumeric to
      *>     alphanumeric elementary move (GR4) with no conversion of data: G2 receives the characters WXYZ
      *>     whatever they are, and G1, which names the same storage area, reads WXYZ.
      *>   B INSPECT H2 REPLACING ALL "2" BY "X": H1 holds the characters 1234 and H2 names the same storage
      *>     area, so the one occurrence of "2" (position 2) is replaced (GR7, format 2): 1X34 through both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4089C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GR.
          05 GR-A PIC X(2) VALUE "WX".
          05 GR-B PIC X(2) VALUE "YZ".
       01 G1 PIC 9(4) VALUE 1234.
       01 G2 REDEFINES G1 PIC 9(4).
       01 H1 PIC 9(4) VALUE 1234.
       01 H2 REDEFINES H1 PIC 9(4).
       PROCEDURE DIVISION.
           MOVE GR TO G2
           DISPLAY "A[" G1(1:4) "/" G2(1:4) "]"
           INSPECT H2 REPLACING ALL "2" BY "X"
           DISPLAY "B[" H1(1:4) "/" H2(1:4) "]"
           STOP RUN.
