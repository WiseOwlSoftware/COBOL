*>--- conformance-case
*> id: pb4382-stray-end-if-after-an-enclosing-terminator
*> rules: [GR-14.5.3.2-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4382 (review R2-2 of the wave 20261823 branch). ISO 14.5.3.2: "An explicit scope terminator terminates
*> the scope of: ... 2) any unterminated statements that appear between that statement-name and the explicit scope
*> terminator" (cite.py --check 14.5.3.2 -> OK). The IF below has no statement-1 (one fault, COBOL2072, at the
*> END-PERFORM that follows its condition). That END-PERFORM terminates the PERFORM and the unterminated IF between
*> them, so the END-IF after DISPLAY "X" follows a complete statement with no IF open and terminates nothing: refused
*> COBOL0306, the misplaced scope terminator. It fails if the recovery keeps the IF it left as awaiting an END-IF
*> after the statement enclosing it ended, and draws nothing for the stray one.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4382STRAYENCL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    PERFORM UNTIL W-N = 2 IF W-N = 1 END-PERFORM DISPLAY "X" END-IF.
    STOP RUN.
