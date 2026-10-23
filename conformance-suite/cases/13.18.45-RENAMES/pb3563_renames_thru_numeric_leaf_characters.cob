      *>--- conformance-case
      *> id: pb3563_renames_thru_numeric_leaf_characters
      *> rules: [GR-13.18.45.4-2, GR-14.9.25.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3563 - a store through a RENAMES ... THROUGH alias deposits the
      *> sending CHARACTERS into the numeric DISPLAY leaf it spans; it never
      *> converts them. ISO 13.18.45.4 GR2: "When the THROUGH phrase is specified,
      *> data-name-1 defines an alphanumeric group item that includes all
      *> elementary items starting with data-name-2 ...", so a move to the alias
      *> is a group move, and 14.9.25.4 GR4: "Any move that is not an elementary
      *> move ... is treated exactly as if it were an alphanumeric to alphanumeric
      *> elementary move, except that there is no conversion of data from one
      *> form of internal representation to another". R-AC is the seven
      *> characters R-A R-B R-C, so MOVE "XXXXXXX" TO R-AC leaves "XXX" in R-B and
      *> DISPLAY R-AC shows XXXXXXX, where the defect stored XX000XX.
      *> Why each leg can fail: leg 1 and 2 (the alias, then the class test of
      *> the spanned leaf, which holds no digits); leg 3 (the plain group
      *> control, which already conformed); leg 4 (an alias whose numeric leaf
      *> the program never names, which converted too); legs 5-6 (valid digits
      *> still flow both ways through the same leaf); legs 7-9 (a figurative
      *> constant and an INSPECT REPLACING through the alias are characters
      *> too); legs 10-11 (the numeric leaf is a REDEFINES class member, which
      *> the same two rules do not exempt: through the alias, and as the
      *> receiver of a group move).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3563MOVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 R-A PIC X(2) VALUE "AB".
          05 R-B PIC 9(3) VALUE 123.
          05 R-C PIC X(2) VALUE "CD".
       66 R-AC RENAMES R-A THROUGH R-C.
       01 G.
          05 G-A PIC X(2).
          05 G-B PIC 9(3).
          05 G-C PIC X(2).
       01 Q.
          05 Q-A PIC X(2) VALUE "AB".
          05 Q-B PIC 9(3) VALUE 123.
          05 Q-C PIC X(2) VALUE "CD".
       66 Q-AC RENAMES Q-A THROUGH Q-C.
       01 RV.
          05 RV-A PIC X(2) VALUE "AB".
          05 RV-B PIC 9(3) VALUE 123.
          05 RV-B2 REDEFINES RV-B PIC 9V99.
          05 RV-C PIC X(2) VALUE "CD".
       66 RV-AC RENAMES RV-A THROUGH RV-C.
       01 SG.
          05 SG-1 PIC X(3) VALUE "ABC".
       01 RE.
          05 RE-B PIC 9(3).
          05 RE-B2 REDEFINES RE-B PIC 9V99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "XXXXXXX" TO R-AC
           DISPLAY "1 [" R-AC "]"
           IF R-B IS NUMERIC DISPLAY "2 NUMERIC"
              ELSE DISPLAY "2 NOT NUMERIC"
           END-IF
           MOVE "XXXXXXX" TO G
           DISPLAY "3 [" G "]"
           MOVE "XXXXXXX" TO Q-AC
           DISPLAY "4 [" Q-AC "]"
           MOVE "AB123CD" TO R-AC
           DISPLAY "5 " R-B
           ADD 1 TO R-B
           DISPLAY "6 [" R-AC "]"
           MOVE SPACES TO R-AC
           DISPLAY "7 [" R-AC "]"
           IF R-B IS NUMERIC DISPLAY "8 NUMERIC"
              ELSE DISPLAY "8 NOT NUMERIC"
           END-IF
           INSPECT R-AC REPLACING ALL SPACE BY "Z"
           DISPLAY "9 [" R-AC "]"
           MOVE "XXXXXXX" TO RV-AC
           DISPLAY "10 [" RV-AC "]"
           MOVE SG TO RE-B
           DISPLAY "11 [" RE-B "]"
           STOP RUN.
