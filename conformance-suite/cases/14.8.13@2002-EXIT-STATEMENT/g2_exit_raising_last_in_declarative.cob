      *>--- conformance-case
      *> id: g2_exit_raising_last_in_declarative
      *> rules: [2002-SR-14.8.13.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.2 SR6 - "The LAST phrase may be specified only in
      *>   a declarative procedure." The granted form, accepted and run:
      *>   EXIT PROGRAM RAISING LAST EXCEPTION in a declarative of
      *>   G2EXT02S (executed) and EXIT FUNCTION RAISING LAST EXCEPTION
      *>   in a declarative of G2EXT02F (compiled; that declarative does
      *>   not run). The refusals beside it are
      *>   g2-exit-raising-last-program-outside-decl, -method- and
      *>   -function- (COBOL2103).
      *> EXPECTED (derived): F=GOOD - G2EXT02F runs MOVE "GOOD" then
      *>   EXIT FUNCTION, §2002:14.8.13.3 GR8 ("The value in the data
      *>   item referenced by the RETURNING phrase in the containing
      *>   function definition becomes the result of the function
      *>   activation"). It precedes every TURN directive, so no
      *>   checking is on in it. IN-DECL - ADD 1 TO N (PIC 9 VALUE 9)
      *>   stores 10 into one digit with no SIZE ERROR phrase:
      *>   §2002:14.6.4 sets EC-SIZE-TRUNCATION, a fatal condition;
      *>   checking is on and D1 names it, so §2002:14.5.12.1.2 item 2
      *>   executes D1. CALLER-DECL - D1's EXIT PROGRAM RAISING LAST
      *>   ends G2EXT02S (§2002:14.8.13.3 GR6); by §2002:14.8.13.3 GR3c1
      *>   the raised EC-SIZE-TRUNCATION "is set to exist in the
      *>   activating runtime element", where checking is on (the TURN
      *>   before the CALL), so the caller's DC runs and its STOP RUN
      *>   ends the run unit. Neither NOT-REACHED line may appear.
      *> FAILS IF: the LAST phrase is refused in a declarative, or the
      *>   declarative's EXIT PROGRAM does not end the subprogram.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2EXT02F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC X(4).
       PROCEDURE DIVISION RETURNING LR.
       DECLARATIVES.
       DF SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-TRUNCATION.
       DF-P.
           EXIT FUNCTION RAISING LAST EXCEPTION.
       END DECLARATIVES.
       MF SECTION.
       MF1.
           MOVE "GOOD" TO LR.
           EXIT FUNCTION.
       END FUNCTION G2EXT02F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2EXT02F.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DC SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-TRUNCATION.
       DC-P.
           DISPLAY "CALLER-DECL".
           STOP RUN.
       END DECLARATIVES.
       MC SECTION.
       MAIN.
           DISPLAY "F=" FUNCTION G2EXT02F.
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
           CALL "G2EXT02S".
           DISPLAY "NOT-REACHED-CALLER".
           STOP RUN.
       END PROGRAM G2EXT02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT02S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-TRUNCATION.
       D1-P.
           DISPLAY "IN-DECL".
           EXIT PROGRAM RAISING LAST EXCEPTION.
       END DECLARATIVES.
       M SECTION.
       M1.
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
           ADD 1 TO N.
           DISPLAY "NOT-REACHED-SUB".
           EXIT PROGRAM.
       END PROGRAM G2EXT02S.
