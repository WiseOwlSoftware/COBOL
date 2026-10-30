      *>--- conformance-case
      *> id: pb1908_string_function_order
      *> rules: [GR-14.9.49.4-11, GR-14.9.43.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB1908 review R1-1/R1-2 -- a STRING's FUNCTION operand is
      *> identified in statement order whether or not its argument
      *> can share storage with identifier-3.
      *> ISO 14.6.4 7): "the identifiers within a statement are
      *> evaluated in left to right order as the first operation of
      *> the execution of that statement"; 14.9.49.4 GR11 selects
      *> the declarative by the first exception condition in that
      *> order. Every leg fails on BOTH sides: the sending side with
      *> EC-BOUND-REF-MOD (L = 50 into an 8-position item) and the
      *> receiving side with EC-BOUND-SUBSCRIPT (I = 9 into OCCURS
      *> 3), so the declarative that runs names the side the
      *> statement evaluates first: the sender, every time.
      *> 14.9.43.4 GR10 (Annex A.2 item 60) leaves a sender sharing
      *> identifier-3's storage undefined; it does not reach a
      *> first sending (no transfer has stored before it), a
      *> LINKAGE argument the caller keeps disjoint (F2, F3, F5) or
      *> a function over working storage (F1, F4), so none of them
      *> may change the order.
      *> F1 UPPER-CASE(WX(1:L))    working storage argument
      *> F2 UPPER-CASE(LK-X(1:L))  LINKAGE argument (may share)
      *> F3 LK-X(1:L)              LINKAGE field, no function
      *> F4 UPPER-CASE(WX)(1:L)    reference-modified result
      *> F5 DELIMITED BY UPPER-CASE(LK-X(1:L))   a delimiter
      *> V  in range, the functions give the same values: T(2) is
      *>    ABC and T(3) is xABC (a later sending reads LK-X at
      *>    its transfer, unchanged by the earlier ones).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908FO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC X(8) VALUE "abcdefgh".
       PROCEDURE DIVISION.
           CALL "PB1908FOS" USING X
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908FOS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T PIC X(4) OCCURS 3 TIMES.
       01 WX PIC X(8) VALUE "abcdefgh".
       01 I  PIC 99 VALUE 9.
       01 L  PIC 99 VALUE 50.
       LINKAGE SECTION.
       01 LK-X PIC X(8).
       PROCEDURE DIVISION USING LK-X.
       DECLARATIVES.
       D-SUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D-SUB-P.
           DISPLAY "  EC-BOUND-SUBSCRIPT"
           RESUME AT NEXT STATEMENT.
       D-REF SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       D-REF-P.
           DISPLAY "  EC-BOUND-REF-MOD"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-BOUND CHECKING ON
           DISPLAY "F1 STRING UPPER-CASE(WX(1:L)) INTO T(I)"
           STRING FUNCTION UPPER-CASE(WX(1:L)) DELIMITED BY SIZE
               INTO T(I)
           DISPLAY "F2 STRING UPPER-CASE(LK-X(1:L)) INTO T(I)"
           STRING FUNCTION UPPER-CASE(LK-X(1:L)) DELIMITED BY SIZE
               INTO T(I)
           DISPLAY "F3 STRING LK-X(1:L) INTO T(I)"
           STRING LK-X(1:L) DELIMITED BY SIZE INTO T(I)
           DISPLAY "F4 STRING UPPER-CASE(WX)(1:L) INTO T(I)"
           STRING FUNCTION UPPER-CASE(WX)(1:L) DELIMITED BY SIZE
               INTO T(I)
           DISPLAY "F5 STRING LK-X DELIMITED BY UPPER-CASE(LK-X(1:L))"
           STRING LK-X DELIMITED BY FUNCTION UPPER-CASE(LK-X(1:L))
               INTO T(I)
           MOVE SPACES TO TB
           MOVE 2 TO I
           MOVE 3 TO L
           STRING FUNCTION UPPER-CASE(LK-X(1:L)) DELIMITED BY SIZE
               INTO T(I)
           STRING "x" DELIMITED BY SIZE
                  FUNCTION UPPER-CASE(LK-X(1:L)) DELIMITED BY SIZE
               INTO T(3)
           DISPLAY "V [" T(2) "] [" T(3) "]"
           GOBACK.
       END PROGRAM PB1908FOS.
       END PROGRAM PB1908FO.
