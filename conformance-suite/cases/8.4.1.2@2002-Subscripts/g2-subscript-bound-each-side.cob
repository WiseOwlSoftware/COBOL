      *>--- conformance-case
      *> id: g2-subscript-bound-each-side
      *> rules: [2002-GR-8.4.1.2.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.2.3 GR2 - "The value of a subscript shall be a
      *> positive integer. ... If the value of the subscript is not a
      *> positive integer or is less than one or greater than the highest
      *> permissible occurrence number, the EC-BOUND-SUBSCRIPT exception
      *> condition is set to exist." T has OCCURS 3: the permissible
      *> occurrence numbers are 1, 2 and 3.
      *> Checking is enabled by >>TURN ... CHECKING ON (§2002:7.2.19.3
      *> GR4; the default is OFF, GR1). The declarative records the
      *> exception-name (§2002:15.28.2 RV1, left-justified, space
      *> padded, so the comparison to the literal is exact) and RESUME AT
      *> NEXT STATEMENT continues at the DISPLAY (§2002:14.8.31.3 GR2).
      *>   B0  IDX = 0    less than one          -> "B0 Y"
      *>   B-  IDX = -1   not positive           -> "B- Y"
      *>   B1  IDX = 1    lowest occurrence      -> "B1 N 11"
      *>   B3  IDX = 3    highest occurrence     -> "B3 N 33"
      *>   B4  IDX = 4    greater than OCCURS 3  -> "B4 Y"
      *>   BF  J / 2 with J = 3, i.e. 1.5: not an integer
      *>       (also §2002:8.4.1.2.3 GR1b)       -> "BF Y"
      *> Each leg can fail: an unraised condition prints N, a raise on a
      *> legal subscript prints Y or loses the element value, and a
      *> truncated 1.5 would read T (1) silently.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUB11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05 T PIC 9(2) OCCURS 3 TIMES.
       01  IDX PIC S9(2) VALUE 0.
       01  J   PIC 9 VALUE 3.
       01  R   PIC 9(2) VALUE 0.
       01  HIT PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       BOUND-HANDLER SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       BOUND-PARA.
           IF FUNCTION EXCEPTION-STATUS = "EC-BOUND-SUBSCRIPT"
               MOVE "Y" TO HIT
           ELSE
               MOVE "?" TO HIT
           END-IF.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           MOVE "112233" TO G.
           MOVE 0 TO IDX.
           MOVE "N" TO HIT.
           MOVE T (IDX) TO R.
           DISPLAY "B0 " HIT.
           MOVE -1 TO IDX.
           MOVE "N" TO HIT.
           MOVE T (IDX) TO R.
           DISPLAY "B- " HIT.
           MOVE 1 TO IDX.
           MOVE "N" TO HIT.
           MOVE T (IDX) TO R.
           DISPLAY "B1 " HIT " " R.
           MOVE 3 TO IDX.
           MOVE "N" TO HIT.
           MOVE T (IDX) TO R.
           DISPLAY "B3 " HIT " " R.
           MOVE 4 TO IDX.
           MOVE "N" TO HIT.
           MOVE T (IDX) TO R.
           DISPLAY "B4 " HIT.
           MOVE "N" TO HIT.
           MOVE T (J / 2) TO R.
           DISPLAY "BF " HIT.
           STOP RUN.
