*>--- conformance-case
*> id: pb4162_table_all_argument_held_below_2002
*> rules: [GR-8.4.3.2.4-2, AR-15.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4162 - the negative of pb4162_table_all_argument_held_for_order. The user-defined function whose activation
      *> follows the table(ALL) argument is a COBOL-2002 introduction (the FUNCTION-ID paragraph, ISO 11.5), so the program is
      *> refused by name at 85 - COBOL0900, the introduction gate.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4162N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(8).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE L-X TO L-R.
           GOBACK.
       END FUNCTION PB4162N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4162NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB4162N
           FUNCTION MAX INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-F PIC 9(4) OCCURS 3 VALUE 7.
       01 WS-LO PIC 9(8) VALUE 0.
       01 WS-R  PIC 9(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION MAX (WS-F (ALL), FUNCTION PB4162N (WS-LO))
           STOP RUN.
       END PROGRAM PB4162NEG.
