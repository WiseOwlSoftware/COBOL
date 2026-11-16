      *>--- conformance-case
      *> id: g2002-pic-cd-p-scaling-legal-forms
      *> rules: [2002-SR-13.16.38.2-1, 2002-SR-13.16.38.2-5, 2002-SR-13.16.38.2-12, 2002-SR-13.16.38.2-13, 2002-SR-13.16.38.2-14, 2002-SR-13.16.38.2-15]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Every item below is a record description entry written after
      *> an OUTPUT CD entry, an ordinary data description entry there
      *> (§2002:A.1.1.2: "may be implicitly redefined by user-specified
      *> record description entries"; "are explained in 8.5.1.2,
      *> Levels, and in 13.14, Data description entry"), so the PICTURE
      *> syntax rules reach it. The communication facility is obsolete
      *> at 2002 (COBOL0903 warning); the warning does not fail the case.
      *> Each item writes a form the rules leave legal; the program must
      *> compile and run (the refusals are the sibling cases
      *> g2002-pic-cd-sr1/sr12/sr13/sr14/sr15-*).
      *> §2002:13.16.38.2 SR1 - PICTURE only at the elementary level: GA
      *>   is a group with no PICTURE; X1..X4 are elementary with one.
      *>   LENGTH(GA) = 3+3+2+2 = 10 (§2002:15.39.3 RV3, §2002:13.16.38.3
      *>   GR4). Fails if the group is taken as elementary or refused.
      *> §2002:13.16.38.2 SR5 - "PIC is an abbreviation for PICTURE":
      *>   X1 PIC 9(3) and X2 PICTURE 9(3), X3 PIC IS X(2) and X4
      *>   PICTURE IS X(2) are the same descriptions; MOVE 12 gives 012
      *>   in both (zero fill, §2002:14.8.24.3 GR4b), so GA = 012012ABAB.
      *> §2002:13.16.38.2 SR12 - a continuous string of 'P's at the
      *>   rightmost (P1 S99PP, P3 99PPV) or leftmost (P2 SVPP99, P4 PP9)
      *>   digit positions. §2002:13.16.38.3 GR13: 'P' is not counted in
      *>   the size of the item, 'V' is not, and 'S' is counted only with
      *>   SIGN ... SEPARATE: LENGTH P1=2 P2=2 P3=2 P4=1. The algebraic
      *>   value assumes zero in each 'P' position (used in a numeric
      *>   comparison and a move from an item with 'P'): P1 holds -1200,
      *>   P2 -0.0012 (V then PP then 99: the 9s are the 10**-3 and
      *>   10**-4 positions); P3 moved to 9(6).9(6) edits to
      *>   004500.000000 and P4 to 000000.007000 (§2002:13.16.38.4
      *>   GR4 special insertion).
      *>   Fails if a 'P' is stored or its scaling is ignored.
      *> §2002:13.16.38.2 SR13 - no item with 'P' carries '.'.
      *> §2002:13.16.38.2 SR14 - 'S' first (S99PP, SVPP99); the sign is
      *>   operational: P1 < 0 and P2 < 0 hold.
      *> §2002:13.16.38.2 SR15 - 'V' immediately precedes the first 'P'
      *>   (SVPP99) or follows the last 'P' (99PPV).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2P2OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WE  PIC 9(6).9(6).
       01  WL1 PIC 99.
       01  WL2 PIC 99.
       01  WL3 PIC 99.
       01  WL4 PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 GA.
              10 X1 PIC 9(3).
              10 X2 PICTURE 9(3).
              10 X3 PIC IS X(2).
              10 X4 PICTURE IS X(2).
           05 P1 PIC S99PP.
           05 P2 PIC SVPP99.
           05 P3 PIC 99PPV.
           05 P4 PIC PP9.
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH(GA) TO WL1
           DISPLAY "SR1 LEN GA=" WL1
           MOVE 12 TO X1 X2
           MOVE "AB" TO X3 X4
           DISPLAY "SR5 GA=[" GA "]"
           IF X1 = X2 AND X1 = 12
               DISPLAY "SR5 X1=X2=12"
           ELSE
               DISPLAY "SR5 X1/X2 WRONG"
           END-IF
           MOVE FUNCTION LENGTH(P1) TO WL1
           MOVE FUNCTION LENGTH(P2) TO WL2
           MOVE FUNCTION LENGTH(P3) TO WL3
           MOVE FUNCTION LENGTH(P4) TO WL4
           DISPLAY "LEN P1=" WL1 " P2=" WL2 " P3=" WL3 " P4=" WL4
           MOVE -1200 TO P1
           MOVE -0.0012 TO P2
           MOVE 4500 TO P3
           MOVE 0.007 TO P4
           IF P1 = -1200 AND P1 < 0
               DISPLAY "P1=-1200"
           ELSE
               DISPLAY "P1 WRONG"
           END-IF
           IF P2 = -0.0012 AND P2 < 0
               DISPLAY "P2=-0.0012"
           ELSE
               DISPLAY "P2 WRONG"
           END-IF
           IF P3 = 4500
               DISPLAY "P3=4500"
           ELSE
               DISPLAY "P3 WRONG"
           END-IF
           IF P4 = 0.007
               DISPLAY "P4=0.007"
           ELSE
               DISPLAY "P4 WRONG"
           END-IF
           MOVE P3 TO WE
           DISPLAY "P3 WE=" WE
           MOVE P4 TO WE
           DISPLAY "P4 WE=" WE
           STOP RUN.
