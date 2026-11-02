*>--- conformance-case
*> id: pb4448-empty-if-after-a-statement-in-an-inline-perform
*> rules: [GR-14.5.3.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4448. ISO 5.2.6.3: braces enclosing a portion of a general format indicate that "the syntax element
*> contained within the braces or one of the alternatives contained within the braces shall be explicitly specified"
*> (cite.py --check 5.2.6.3 -> OK), and IF's statement-1 is such an element, so the IF below, which has none, holds
*> ONE fault, refused COBOL2072 at the END-IF that stands where the statement is due. The first END-IF terminates that
*> IF (ISO 14.5.3.2: "1) the most-recently preceding unterminated statement having the statement-name for which that
*> scope terminator is defined", cite.py --check 14.5.3.2 -> OK), and the second one then terminates nothing: COBOL0306.
*> The IF follows a complete DISPLAY in an inline PERFORM's block, whose END-PERFORM is mandatory, so the block's own
*> parse ends before the IF and reports where it stands. It fails if the report is the bare "unexpected 'IF'" that
*> says no statement may begin there (COBOL5506), a claim about a word the source may legally begin a statement with,
*> and the fault in the IF is then never named.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4448PERFORMIF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-K PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    PERFORM UNTIL W-N = 2 DISPLAY "A" IF W-K = 2 END-IF END-IF END-PERFORM.
    STOP RUN.
