      *>--- conformance-case
      *> id: g85_f27_04
      *> rules: [1985-GR-XI.4.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR2 - KEY data-names in decreasing significance
      *> left to right "without regard to how they are divided into KEY
      *> phrases"; GR2a ASCENDING lowest to highest, GR2b DESCENDING
      *> highest to lowest, by the relation-condition comparison (K2 is
      *> a signed numeric key, compared algebraically: -05 < 00 < 03).
      *> The same six records (K1, K2, ID) are sorted five ways and the
      *> IDs printed in return order. S1 (one KEY phrase, K1 K2) and S2
      *> (two KEY phrases) must agree (GR2's "without regard"); S3 mixes
      *> directions (GR2a and GR2b); S4 makes K2 the major key by
      *> listing it first (the leftmost data-name is the major key); S5
      *> is DESCENDING on both. Every K1/K2 pair is distinct, so GR4's
      *> undefined tie order is never consulted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2704.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g85f2704s.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SF-REC.
           05  K1        PIC X.
           05  K2        PIC S99.
           05  ID-X      PIC X.
       WORKING-STORAGE SECTION.
       01  INIT-DATA.
           05  FILLER    PIC X(4) VALUE "B031".
           05  FILLER    PIC X(4) VALUE "A052".
           05  FILLER    PIC X(4) VALUE "B053".
           05  FILLER    PIC X(4) VALUE "A034".
           05  FILLER    PIC X(4) VALUE "B005".
           05  FILLER    PIC X(4) VALUE "A006".
       01  INIT-TAB REDEFINES INIT-DATA.
           05  INIT-ENT  OCCURS 6 TIMES.
               10  I-K1  PIC X.
               10  I-K2  PIC 99.
               10  I-ID  PIC X.
       01  SIGNS         PIC X(6) VALUE "+--+++".
       01  SIGN-TAB REDEFINES SIGNS.
           05  I-SIGN    PIC X OCCURS 6 TIMES.
       01  IX            PIC 9.
       01  OX            PIC 9.
       01  EOF-SW        PIC X.
       01  RESULT        PIC X(6).
       01  RES-TAB REDEFINES RESULT.
           05  R-ID      PIC X OCCURS 6 TIMES.
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SF ON ASCENDING KEY K1 K2
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "S1 " RESULT.
           SORT SF ON ASCENDING KEY K1 ON ASCENDING KEY K2
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "S2 " RESULT.
           SORT SF ON ASCENDING KEY K1 ON DESCENDING KEY K2
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "S3 " RESULT.
           SORT SF ON DESCENDING KEY K2 ON ASCENDING KEY K1
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "S4 " RESULT.
           SORT SF ON DESCENDING KEY K1 K2
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "S5 " RESULT.
           STOP RUN.
       IN-P.
           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 6
               MOVE I-K1 (IX) TO K1
               IF I-SIGN (IX) = "-"
                   COMPUTE K2 = 0 - I-K2 (IX)
               ELSE
                   MOVE I-K2 (IX) TO K2
               END-IF
               MOVE I-ID (IX) TO ID-X
               RELEASE SF-REC
           END-PERFORM.
       OUT-P.
           MOVE SPACES TO RESULT.
           MOVE 0 TO OX.
           MOVE "N" TO EOF-SW.
           PERFORM UNTIL EOF-SW = "Y"
               RETURN SF
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END
                       ADD 1 TO OX
                       MOVE ID-X TO R-ID (OX)
               END-RETURN
           END-PERFORM.
