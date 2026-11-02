*>--- conformance-case
*> id: pb4449-second-else-after-an-empty-if-in-an-if
*> rules: [GR-14.5.3.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4449. ISO 14.5.3.3 4): "Any phrase encountered is the next phrase of the most-recently preceding
*> unterminated statement with which that phrase may be syntactically associated. If all permitted occurrences of a
*> phrase have already been specified for a given statement, a subsequent occurrence of that phrase is not
*> syntactically associated with that statement" (cite.py --check 14.5.3.3 -> OK). The inner IF has no statement-1 (one
*> fault, COBOL2072, at its END-IF: ISO 5.2.6.3). The first ELSE is the next phrase of the outer IF, which holds
*> DISPLAY "Q" and the inner IF; the IF's format prints the ELSE phrase once, so the second ELSE is associated with no
*> statement: refused COBOL2269, the statement ends before it. It fails if the recovery that left the outer IF takes every
*> later ELSE for its phrase, so that the second one is never reported.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4449SECONDELSE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-K PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 DISPLAY "Q" IF W-K = 1 END-IF ELSE DISPLAY "Z" ELSE DISPLAY "Y" END-IF.
    STOP RUN.
