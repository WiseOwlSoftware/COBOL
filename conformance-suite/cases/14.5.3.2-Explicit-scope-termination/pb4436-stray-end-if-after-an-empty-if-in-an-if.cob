*>--- conformance-case
*> id: pb4436-stray-end-if-after-an-empty-if-in-an-if
*> rules: [GR-14.5.3.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4436. ISO 14.5.3.2: "An explicit scope terminator terminates the scope of: 1) the most-recently preceding
*> unterminated statement having the statement-name for which that scope terminator is defined" (cite.py --check
*> 14.5.3.2 -> OK). The inner IF below has no statement-1 (one fault, COBOL2072, at its own END-IF), and that END-IF
*> terminates it. The second END-IF terminates the outer IF, which holds DISPLAY "A". The DISPLAY "X" that follows
*> stands in no IF, so the third END-IF has no unterminated IF to end: refused COBOL0306, the misplaced scope
*> terminator. It fails if the recovery of the inner IF swallows the outer IF's END-IF, so that the outer IF is
*> still awaiting one and takes the third as its own, and nothing is said about it.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4436STRAYNESTED.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-K PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 DISPLAY "A" IF W-K = 2 END-IF END-IF DISPLAY "X" END-IF.
    STOP RUN.
