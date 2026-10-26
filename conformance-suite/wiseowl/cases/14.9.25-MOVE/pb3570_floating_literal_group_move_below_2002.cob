*>--- conformance-case
*> id: pb3570_floating_literal_group_move_below_2002
*> rules: [GR-14.9.25.4-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3570, the negative of pb3570_floating_literal_group_move: a floating-point numeric
*> literal (ISO 8.3.3.3.3) is a COBOL-2002 introduction, so the MOVE of one to a group is
*> refused at COBOL-85 by name (COBOL0900) and accepted from 2002 on.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3570FN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 H.
   05 H1 PIC X(6).
PROCEDURE DIVISION.
MAIN.
    MOVE 1.5E+3 TO H
    STOP RUN.
