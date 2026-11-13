      *>--- conformance-case
      *> id: pb5241_cancel_main_program_85
      *> rules: [1985-GR-X.5.3.4-8, 1985-GR-X.5.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> 1985 X.5.3.4 GR5 forbids a CANCEL that refers "directly or
      *> indirectly to any program that has been called and has not
      *> yet executed an EXIT PROGRAM statement"; the main program of
      *> the run unit is active but was never called, so GR8 governs:
      *> "No action is taken when an explicit or implicit CANCEL
      *> statement is executed naming a program that has not been
      *> called in this run unit" (kb/Work PB5241). From 2002 the same
      *> CANCEL raises EC-PROGRAM-CANCEL-ACTIVE (2023 14.9.5.4 GR5).
      *> The second line fails if the CANCEL ended the run unit (it
      *> would be missing, with the third); the third shows a count of
      *> 2 because no action means the contained PB5241S is not
      *> canceled either (GR4's cascade is part of the action), so it
      *> fails if the CANCEL cascaded (a count of 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5241M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 M PIC 9 VALUE 0.
       77 MX PIC X.
       PROCEDURE DIVISION.
       P0.
           ADD 1 TO M.
           CALL "PB5241S".
           CANCEL "PB5241M".
           MOVE M TO MX.
           DISPLAY "AFTER-SELF-CANCEL M=" MX.
           CALL "PB5241S".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5241S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 C PIC 9 VALUE 0.
       77 CX PIC X.
       PROCEDURE DIVISION.
       S0.
           ADD 1 TO C.
           MOVE C TO CX.
           DISPLAY "SUB " CX.
           EXIT PROGRAM.
       END PROGRAM PB5241S.
       END PROGRAM PB5241M.
