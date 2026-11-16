      *>--- conformance-case
      *> id: g85_bf42_01
      *> rules: [1989-FMT-A.2.32.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.32.2 - FUNCTION ORD-MAX ( { argument-1 } ... ): the
      *> repeated argument-1 in each permitted spelling. Expected values
      *> follow from §1989:A.2.32.4 RV1 (position of the greatest) and RV3
      *> (leftmost on a tie); argument forms from §1989:A.2.2 (identifiers,
      *> arithmetic expressions, literals; a table with an ALL subscript
      *> for a repeated argument).
      *>   ORD-MAX (3 9 1)          -> 9 is 2nd           -> 002
      *>   ORD-MAX (4)              -> one argument       -> 001
      *>   ORD-MAX (7, 7, 2)        -> tie, leftmost      -> 001
      *>   ORD-MAX (A B 2.5 A + 1)  -> 5 -7 2.5 6         -> 004
      *>   ORD-MAX (E (ALL))        -> 10 -20 30 30       -> 003
      *>   ORD-MAX (X "AB")         -> "AB  " = "AB" tie -> 001
      *> A leg fails if a count of arguments, a separator, an argument form
      *> or the ALL expansion is refused or mis-positioned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4201.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A                    PIC 9(3)  VALUE 5.
       01  WS-B                    PIC S9(3) VALUE -7.
       01  WS-X                    PIC X(4)  VALUE "AB".
       01  WS-TAB.
           05  WS-E                PIC S9(3) OCCURS 4 TIMES.
       01  WS-N                    PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 10 TO WS-E (1).
           MOVE -20 TO WS-E (2).
           MOVE 30 TO WS-E (3).
           MOVE 30 TO WS-E (4).
           COMPUTE WS-N = FUNCTION ORD-MAX (3 9 1).
           DISPLAY WS-N.
           COMPUTE WS-N = FUNCTION ORD-MAX (4).
           DISPLAY WS-N.
           COMPUTE WS-N = FUNCTION ORD-MAX (7, 7, 2).
           DISPLAY WS-N.
           COMPUTE WS-N = FUNCTION ORD-MAX (WS-A WS-B 2.5 WS-A + 1).
           DISPLAY WS-N.
           COMPUTE WS-N = FUNCTION ORD-MAX (WS-E (ALL)).
           DISPLAY WS-N.
           COMPUTE WS-N = FUNCTION ORD-MAX (WS-X "AB").
           DISPLAY WS-N.
           STOP RUN.
