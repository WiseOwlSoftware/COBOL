      *>--- conformance-case
      *> id: g2-cancel-program-name-scope
      *> rules: [2002-GR-14.8.5.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.5.3 GR2 - "The program-name is used by the runtime
      *> system to locate the program according to the rules specified
      *> in 8.4.5, Scope of names, and 8.4.5.2, Scope of program-names."
      *> Each leg cancels a program reached by one 8.4.5.2 rule, and the
      *> next CALL shows the program in its initial state
      *> (§2002:14.8.5.3 GR3), so a CANCEL that located no program would
      *> leave the counter at its last value.
      *>  rule 1 (not common, directly contained): G2CAN02 cancels its
      *>    own G2CN2D               -> D=1 D=2 D=1
      *>  rule 1, scope choosing between two programs of one name: a
      *>    second outermost program G2CN2X contains its own non-common
      *>    G2CN2D (names need be unique only "within that outermost
      *>    program", 8.4.5.2). G2CN2X's CALL reaches its own G2CN2D, and
      *>    G2CAN02's CANCEL "G2CN2D" reaches only G2CAN02's G2CN2D, so
      *>    G2CN2X's G2CN2D keeps its last-used state
      *>                             -> XD=6 before the D lines, XD=7
      *>    after them (a run-unit-wide CANCEL would print XD=6 again)
      *>  rule 2 (common, directly contained): G2CN2C, a sibling
      *>    contained in the same containing program, cancels G2CN2B
      *>                             -> B=1 B=2 B=1
      *>  rule 3 (outermost program): G2CAN02 cancels the separately
      *>    written outermost G2CN2H by an identifier holding its name
      *>                             -> H=1 H=2 H=1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 HN PIC X(6) VALUE "G2CN2H".
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2CN2X".
           CALL "G2CN2D".
           CALL "G2CN2D".
           CANCEL "G2CN2D".
           CALL "G2CN2D".
           CALL "G2CN2X".
           CALL "G2CN2C".
           CALL "G2CN2H".
           CALL "G2CN2H".
           CANCEL HN.
           CALL "G2CN2H".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN2B IS COMMON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MB.
           ADD 1 TO N.
           DISPLAY "B=" N.
           GOBACK.
       END PROGRAM G2CN2B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN2C.
       PROCEDURE DIVISION.
       MC.
           CALL "G2CN2B".
           CALL "G2CN2B".
           CANCEL "G2CN2B".
           CALL "G2CN2B".
           GOBACK.
       END PROGRAM G2CN2C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN2D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MD.
           ADD 1 TO N.
           DISPLAY "D=" N.
           GOBACK.
       END PROGRAM G2CN2D.
       END PROGRAM G2CAN02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN2H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MH.
           ADD 1 TO N.
           DISPLAY "H=" N.
           GOBACK.
       END PROGRAM G2CN2H.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN2X.
       PROCEDURE DIVISION.
       MX.
           CALL "G2CN2D".
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN2D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MXD.
           ADD 1 TO N.
           DISPLAY "XD=" N.
           GOBACK.
       END PROGRAM G2CN2D.
       END PROGRAM G2CN2X.
