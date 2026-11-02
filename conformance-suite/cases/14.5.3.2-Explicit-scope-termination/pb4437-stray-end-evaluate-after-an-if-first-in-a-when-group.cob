*>--- conformance-case
*> id: pb4437-stray-end-evaluate-after-an-if-first-in-a-when-group
*> rules: [GR-14.5.3.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4437. ISO 14.5.3.2: "An explicit scope terminator terminates the scope of: 1) the most-recently preceding
*> unterminated statement having the statement-name for which that scope terminator is defined" (cite.py --check
*> 14.5.3.2 -> OK). The IF that is the first statement of the WHEN 1 group has no statement-1 (one fault, COBOL2072,
*> at its ELSE: ISO 5.2.6.2, the format's imperative-statement-1 is not bracketed). Its ELSE branch DISPLAY W-A, its
*> END-IF and the DISPLAY "X" after it are all in order, and the first END-EVALUATE terminates the EVALUATE: so the
*> program holds ONE fault and ONE misplaced terminator, the second END-EVALUATE, refused COBOL0306. It fails if the
*> recovery takes the IF's own END-IF for a stray one (a false COBOL0306 that discards the rest of the sentence with it),
*> so that the second END-EVALUATE, the true stray, is never reported.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4437WHENIF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-K PIC 9(4) VALUE 1.
01 W-A PIC X VALUE "A".
PROCEDURE DIVISION.
MAIN-P.
    EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY "X" END-EVALUATE END-EVALUATE.
    STOP RUN.
