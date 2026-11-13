      *>--- conformance-case
      *> id: pb5241_cancel_main_program_active
      *> rules: [GR-14.9.5.4-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
       >>TURN EC-PROGRAM-CANCEL-ACTIVE CHECKING ON
      *> 14.9.5.4 GR5: "The program to be canceled shall not be in the
      *> active state or contain a program in the active state" (the
      *> 2002 text, 14.8.5.3 GR5, the same). The run unit's main
      *> program is in the active state though no CALL entered it, so
      *> its own CANCEL raises EC-PROGRAM-CANCEL-ACTIVE and "the
      *> referenced program is not canceled" (kb/Work PB5241; at 1985
      *> the same CANCEL takes no action, X.5.3.4 GR8). The declarative
      *> takes the condition and RESUME continues. The EC= line fails
      *> if nothing was raised; the last line fails if the CANCEL
      *> canceled the main program's contained PB5241B (GR4) all the
      *> same (a count of 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5241A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 M PIC 9 VALUE 0.
       77 MX PIC X.
       77 EX PIC X(31).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-CANCEL-ACTIVE.
       ERR-PARA.
           MOVE FUNCTION EXCEPTION-STATUS TO EX
           DISPLAY "EC=" EX
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P0.
           ADD 1 TO M
           CALL "PB5241B"
           CANCEL "PB5241A"
           MOVE M TO MX
           DISPLAY "AFTER-SELF-CANCEL M=" MX
           CALL "PB5241B"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5241B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 C PIC 9 VALUE 0.
       77 CX PIC X.
       PROCEDURE DIVISION.
       S0.
           ADD 1 TO C
           MOVE C TO CX
           DISPLAY "SUB " CX
           GOBACK.
       END PROGRAM PB5241B.
       END PROGRAM PB5241A.
