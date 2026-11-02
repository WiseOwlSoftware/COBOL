*>--- conformance-case
*> id: pb4447-stray-end-if-after-an-evaluate-in-an-if-holding-an-empty-if
*> rules: [GR-14.5.3.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4447. ISO 14.5.3.2: "An explicit scope terminator terminates the scope of: 1) the most-recently preceding
*> unterminated statement having the statement-name for which that scope terminator is defined" (cite.py --check
*> 14.5.3.2 -> OK). The innermost IF has no statement-1 (one fault, COBOL2072, at its ELSE: ISO 5.2.6.3, the format's
*> imperative-statement-1 is not bracketed). Its ELSE is that IF's own phrase (ISO 14.5.3.3: "Any phrase encountered is
*> the next phrase of the most-recently preceding unterminated statement with which that phrase may be syntactically
*> associated", cite.py --check 14.5.3.3 -> OK), the first END-IF terminates it, the first END-EVALUATE the EVALUATE,
*> the second END-IF the outer IF: so the program holds ONE fault and ONE misplaced terminator, the third END-IF,
*> refused COBOL0306. It fails if the outer IF takes the inner IF's ELSE or END-IF for its own, so that the
*> END-EVALUATE draws a false COBOL0306 (an EVALUATE the source holds) and the true stray END-IF is never reported.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4447EVALIF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-K PIC 9(4) VALUE 1.
01 W-A PIC X VALUE "A".
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY "X" END-EVALUATE END-IF END-IF.
    STOP RUN.
