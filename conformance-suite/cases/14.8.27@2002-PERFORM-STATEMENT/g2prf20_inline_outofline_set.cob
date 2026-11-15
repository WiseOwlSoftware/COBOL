      *>--- conformance-case
      *> id: g2prf20_inline_outofline_set
      *> rules: [2002-GR-14.8.27.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.27.3 GR2 - "An inline PERFORM statement and an
      *>   out-of-line PERFORM statement function identically"; the
      *>   out-of-line set runs "beginning with the first statement of
      *>   procedure-name-1 and ending with the last statement of
      *>   procedure-name-2, or, if procedure-name-2 is not specified,
      *>   the last statement of procedure-name-1"; the inline set is
      *>   "all statements contained within the PERFORM statement".
      *> Each paragraph appends its letter (or the VARYING value) to T
      *>   and P is the next free position; each line shows T(1:P - 1).
      *> DERIVATION of every .out line:
      *>   ONE [A]       PERFORM PA: PA's statements only (no PB).
      *>   THRU [ABC]    PERFORM PA THRU PC: first statement of PA to
      *>                 the last of PC, so PB between them runs too.
      *>   THRU2 [BCBC]  PERFORM PB THRU PC 2 TIMES (GR7): the set
      *>                 PB..PC twice.
      *>   INL2 [BCBC]   the same two statements written inline with
      *>                 2 TIMES: identical to THRU2 (GR2).
      *>   SECT [XY]     PERFORM SX: a section's set is its paragraphs
      *>                 X1 and X2, ending with the last statement of
      *>                 SX - not continuing into PA after it.
      *>   OOLV [123] K=4 and INLV [123] K=4: the same VARYING
      *>                 phrase out-of-line (PV) and inline give the
      *>                 same set executions and the same final K
      *>                 (§2002:14.8.27.3 GR9 d).
      *> Each leg fails if a THRU range drops a paragraph in between,
      *>   a basic PERFORM falls through into the next paragraph, a
      *>   section PERFORM runs on past its last paragraph, or the
      *>   inline and out-of-line forms differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRF20.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(12).
       01 P PIC 99.
       01 K PIC 9.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN-P.
           MOVE SPACES TO T MOVE 1 TO P
           PERFORM PA
           DISPLAY "ONE [" T(1:P - 1) "]"
           MOVE SPACES TO T MOVE 1 TO P
           PERFORM PA THRU PC
           DISPLAY "THRU [" T(1:P - 1) "]"
           MOVE SPACES TO T MOVE 1 TO P
           PERFORM PB THRU PC 2 TIMES
           DISPLAY "THRU2 [" T(1:P - 1) "]"
           MOVE SPACES TO T MOVE 1 TO P
           PERFORM 2 TIMES
               MOVE "B" TO T(P:1)
               ADD 1 TO P
               MOVE "C" TO T(P:1)
               ADD 1 TO P
           END-PERFORM
           DISPLAY "INL2 [" T(1:P - 1) "]"
           MOVE SPACES TO T MOVE 1 TO P
           PERFORM SX
           DISPLAY "SECT [" T(1:P - 1) "]"
           MOVE SPACES TO T MOVE 1 TO P
           PERFORM PV VARYING K FROM 1 BY 1 UNTIL K > 3
           DISPLAY "OOLV [" T(1:P - 1) "] K=" K
           MOVE SPACES TO T MOVE 1 TO P
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 3
               MOVE K TO T(P:1)
               ADD 1 TO P
           END-PERFORM
           DISPLAY "INLV [" T(1:P - 1) "] K=" K
           STOP RUN.
       PV.
           MOVE K TO T(P:1)
           ADD 1 TO P.
       SX SECTION.
       X1.
           MOVE "X" TO T(P:1)
           ADD 1 TO P.
       X2.
           MOVE "Y" TO T(P:1)
           ADD 1 TO P.
       SA SECTION.
       PA.
           MOVE "A" TO T(P:1)
           ADD 1 TO P.
       PB.
           MOVE "B" TO T(P:1)
           ADD 1 TO P.
       PC.
           MOVE "C" TO T(P:1)
           ADD 1 TO P.
