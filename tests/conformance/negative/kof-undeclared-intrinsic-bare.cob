*> reject-at: 2002 2014 2023
      *> kb/Work R22 - the BARE-NAME shape of the same defect: a zero-argument catalogued intrinsic
      *> (CURRENT-DATE, 15.21.2's format is the name alone) written without FUNCTION and without a
      *> REPOSITORY declaration. ISO 8.4.3.2.3 SR2 requires the word FUNCTION here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R22NEGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC X(21).
       PROCEDURE DIVISION.
           MOVE CURRENT-DATE TO WS-R.
           DISPLAY WS-R.
           STOP RUN.
