      *>--- conformance-case
      *> id: g2_relop_format1_simple
      *> rules: [2002-SR-8.7.5.2-2, 2002-SR-8.7.5.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.7.5.2 SR2 - "Format 1 specifies simple relational
      *> operators." The 2002 Format 1 (§2002:8.7.5.1, PDF page 141)
      *> is IS [NOT] GREATER THAN / > / LESS THAN / < / EQUAL TO / =,
      *> so the six NOT forms are SIMPLE operators at 2002 (NOT makes
      *> one operator, §2002:8.7.5.2 SR1); Format 2 (>=, <=, ...) is
      *> extended (§2002:8.7.5.2 SR3).
      *> §2002:8.7.5.2 SR5 - "< is an abbreviation for LESS THAN", in
      *> IS < and in IS NOT < alike.
      *> cite.py --check: OK §2002:8.7.5.2 SR1, SR2, SR3, SR5;
      *>   OK §2002:8.8.4.2.4.3 GR1, GR2a, GR2b.
      *> A = 5 is compared with X = 3, 5, 7; each line shows T/F at
      *> X = 3, 5, 7. Rows 1-12 are the twelve Format 1 operators:
      *>   GREATER THAN / >        5>X          -> TFF
      *>   LESS THAN / <           5<X          -> FFT
      *>   EQUAL TO / =            5=X          -> FTF
      *>   NOT GREATER THAN/NOT >  not 5>X      -> FTT
      *>   NOT LESS THAN / NOT <   not 5<X      -> TTF
      *>   NOT EQUAL TO / NOT =    not 5=X      -> TFT
      *> Rows 13-14: < and NOT < without IS (SR5)     -> FFT, TTF.
      *> Rows 15-18 are what SR2 decides: a NOT form is a simple
      *> operator, so in an abbreviated combined relation condition
      *> NOT followed by it is part of the operator (§2002:8.8.4.2.4.3
      *> GR2b) and the WHOLE negated operator is the one carried to the
      *> next object (GR1). Each leg answers otherwise if the NOT were
      *> read as a logical NOT (GR2c) or only the bare operator carried:
      *>  15 A > X AND NOT < 9 OR X
      *>     = ((A > X) AND (A NOT < 9)) OR (A NOT < X)  -> TTF
      *>     (logical NOT: ((A>X) AND NOT (A<9)) OR (A<X) -> FFT)
      *>  16 A NOT EQUAL X OR 5 = (A NOT = X) OR (A NOT = 5) -> TFT
      *>     (NOT logical: NOT (A = X) OR (A = 5)          -> TTT)
      *>  17 A NOT > X AND 9 = (A NOT > X) AND (A NOT > 9) -> FTT
      *>     (only > carried: (A NOT > X) AND (A > 9)     -> FFF)
      *>  18 A = X OR NOT LESS THAN 6 OR X
      *>     = (A = X) OR (A NOT < 6) OR (A NOT < X)      -> TTF
      *>     (logical NOT: (A=X) OR NOT (A<6) OR (A<X)    -> FTT)
      *> NOT before an EXTENDED operator (>=, <=) is outside the
      *> abbreviated format (§2002:8.8.4.2.4.1, rendered PDF page 158:
      *> NOT | [NOT] simple-relational-operator | extended-...), so
      *> only the Format 1 operators take the NOT here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A                       PIC 9 VALUE 5.
       01  X                       PIC 9.
       01  I                       PIC 9.
       01  RT.
           05  RROW OCCURS 18.
               10  RC              PIC X OCCURS 3.
       PROCEDURE DIVISION.
       M-1.
           PERFORM EVAL-ALL VARYING I FROM 1 BY 1 UNTIL I > 3
           DISPLAY "01 IS GREATER THAN         " RROW(1)
           DISPLAY "02 IS >                    " RROW(2)
           DISPLAY "03 IS LESS THAN            " RROW(3)
           DISPLAY "04 IS <                    " RROW(4)
           DISPLAY "05 IS EQUAL TO             " RROW(5)
           DISPLAY "06 IS =                    " RROW(6)
           DISPLAY "07 IS NOT GREATER THAN     " RROW(7)
           DISPLAY "08 IS NOT >                " RROW(8)
           DISPLAY "09 IS NOT LESS THAN        " RROW(9)
           DISPLAY "10 IS NOT <                " RROW(10)
           DISPLAY "11 IS NOT EQUAL TO         " RROW(11)
           DISPLAY "12 IS NOT =                " RROW(12)
           DISPLAY "13 <                       " RROW(13)
           DISPLAY "14 NOT <                   " RROW(14)
           DISPLAY "15 > X AND NOT < 9 OR X    " RROW(15)
           DISPLAY "16 NOT EQUAL X OR 5        " RROW(16)
           DISPLAY "17 NOT > X AND 9           " RROW(17)
           DISPLAY "18 = X OR NOT LESS 6 OR X  " RROW(18)
           STOP RUN.
       EVAL-ALL.
           COMPUTE X = 2 * I + 1
           IF A IS GREATER THAN X MOVE "T" TO RC(1, I)
               ELSE MOVE "F" TO RC(1, I) END-IF
           IF A IS > X MOVE "T" TO RC(2, I)
               ELSE MOVE "F" TO RC(2, I) END-IF
           IF A IS LESS THAN X MOVE "T" TO RC(3, I)
               ELSE MOVE "F" TO RC(3, I) END-IF
           IF A IS < X MOVE "T" TO RC(4, I)
               ELSE MOVE "F" TO RC(4, I) END-IF
           IF A IS EQUAL TO X MOVE "T" TO RC(5, I)
               ELSE MOVE "F" TO RC(5, I) END-IF
           IF A IS = X MOVE "T" TO RC(6, I)
               ELSE MOVE "F" TO RC(6, I) END-IF
           IF A IS NOT GREATER THAN X MOVE "T" TO RC(7, I)
               ELSE MOVE "F" TO RC(7, I) END-IF
           IF A IS NOT > X MOVE "T" TO RC(8, I)
               ELSE MOVE "F" TO RC(8, I) END-IF
           IF A IS NOT LESS THAN X MOVE "T" TO RC(9, I)
               ELSE MOVE "F" TO RC(9, I) END-IF
           IF A IS NOT < X MOVE "T" TO RC(10, I)
               ELSE MOVE "F" TO RC(10, I) END-IF
           IF A IS NOT EQUAL TO X MOVE "T" TO RC(11, I)
               ELSE MOVE "F" TO RC(11, I) END-IF
           IF A IS NOT = X MOVE "T" TO RC(12, I)
               ELSE MOVE "F" TO RC(12, I) END-IF
           IF A < X MOVE "T" TO RC(13, I)
               ELSE MOVE "F" TO RC(13, I) END-IF
           IF A NOT < X MOVE "T" TO RC(14, I)
               ELSE MOVE "F" TO RC(14, I) END-IF
           IF A > X AND NOT < 9 OR X MOVE "T" TO RC(15, I)
               ELSE MOVE "F" TO RC(15, I) END-IF
           IF A NOT EQUAL X OR 5 MOVE "T" TO RC(16, I)
               ELSE MOVE "F" TO RC(16, I) END-IF
           IF A NOT > X AND 9 MOVE "T" TO RC(17, I)
               ELSE MOVE "F" TO RC(17, I) END-IF
           IF A = X OR NOT LESS THAN 6 OR X MOVE "T" TO RC(18, I)
               ELSE MOVE "F" TO RC(18, I) END-IF.
