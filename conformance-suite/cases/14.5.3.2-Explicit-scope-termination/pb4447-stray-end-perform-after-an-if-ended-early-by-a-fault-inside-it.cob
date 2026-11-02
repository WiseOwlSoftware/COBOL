*>--- conformance-case
*> id: pb4447-stray-end-perform-after-an-if-ended-early-by-a-fault-inside-it
*> rules: [GR-14.5.3.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4447. ISO 14.5.3.2: "An explicit scope terminator terminates the scope of: 1) the most-recently preceding
*> unterminated statement having the statement-name for which that scope terminator is defined" (cite.py --check
*> 14.5.3.2 -> OK). The innermost IF (W-K = 1) has no statement-1 (one fault, COBOL2072, at its ELSE: ISO 5.2.6.3).
*> The ELSE after the second END-IF is a phrase of the OUTER IF, which holds DISPLAY "Z" and the middle IF (ISO 14.5.3.3:
*> "Any phrase encountered is the next phrase of the most-recently preceding unterminated statement with which that
*> phrase may be syntactically associated", cite.py --check 14.5.3.3 -> OK), and the last END-IF terminates it. The
*> END-PERFORM then has no unterminated PERFORM to end: refused COBOL0306, the misplaced scope terminator. It fails
*> if the recovery loses the outer IF (the fault deep inside ended its block before the middle IF), so that its ELSE
*> and END-IF draw false diagnostics and the true stray END-PERFORM is never reported.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4447DEEPFAULT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-K PIC 9(4) VALUE 1.
01 W-A PIC X VALUE "A".
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 DISPLAY "Z" IF W-K = 2 IF W-K = 1 ELSE DISPLAY W-A END-IF END-IF ELSE DISPLAY "Y" END-IF END-PERFORM.
    STOP RUN.
