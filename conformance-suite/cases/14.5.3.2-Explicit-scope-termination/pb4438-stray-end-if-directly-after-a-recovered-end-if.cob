*>--- conformance-case
*> id: pb4438-stray-end-if-directly-after-a-recovered-end-if
*> rules: [GR-14.5.3.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4438. ISO 14.5.3.2: "An explicit scope terminator terminates the scope of: 1) the most-recently preceding
*> unterminated statement having the statement-name for which that scope terminator is defined" (cite.py --check
*> 14.5.3.2 -> OK). The first END-IF below ends the IF whose statement-1 is missing (one fault, COBOL2072, ISO 5.2.6.2:
*> brackets license an omission and the format's imperative-statement-1 is not bracketed). That IF is then terminated,
*> and the second END-IF, written DIRECTLY after it with no statement between, has no unterminated IF to end: refused
*> COBOL0306, the misplaced scope terminator. It fails if the recovery that consumed the first END-IF goes on to the
*> sentence's period and swallows the second one, so that nothing is said about it.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4438DIRECTSTRAY.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 END-IF END-IF.
    STOP RUN.
