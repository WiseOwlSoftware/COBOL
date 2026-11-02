*>--- conformance-case
*> id: pb4436-stray-end-if-after-an-else-of-a-left-if
*> rules: [GR-14.5.3.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4436, review R1-1. ISO 14.5.3.2: "An explicit scope terminator terminates the scope of: 1) the most-recently
*> preceding unterminated statement having the statement-name for which that scope terminator is defined" (cite.py
*> --check 14.5.3.2 -> OK). The inner IF has no statement-1 (one fault, COBOL2072, at its own END-IF), and that END-IF
*> terminates it. The ELSE after it is a phrase of the OUTER IF, which holds DISPLAY "Q" and DISPLAY "Z", and the second
*> END-IF terminates the outer IF; the third END-IF has no unterminated IF to end: refused COBOL0306, the misplaced
*> scope terminator. It fails if the ELSE is taken for a fault of its own (a false second diagnostic) or if the recovery
*> loses the stray END-IF that follows.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4436ELSELEFT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-K PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 DISPLAY "Q" IF W-K = 1 END-IF ELSE DISPLAY "Z" END-IF END-IF.
    STOP RUN.
