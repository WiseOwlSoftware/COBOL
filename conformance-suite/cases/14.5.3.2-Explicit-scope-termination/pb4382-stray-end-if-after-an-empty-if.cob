*>--- conformance-case
*> id: pb4382-stray-end-if-after-an-empty-if
*> rules: [GR-14.5.3.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4382 (review R1-1 of the wave 20261823 branch). ISO 14.5.3.2: "An explicit scope terminator terminates
*> the scope of: 1) the most-recently preceding unterminated statement having the statement-name for which that
*> scope terminator is defined" (cite.py --check 14.5.3.2 -> OK). The first END-IF below ends the IF whose
*> statement-1 is missing (one fault, at that END-IF, ISO 5.2.6.2: brackets license an omission and the format's
*> imperative-statement-1 is not bracketed; COBOL2072). The IF is then terminated, so the second END-IF follows a
*> complete DISPLAY with no IF open and terminates nothing: refused COBOL0306, the misplaced scope terminator. It
*> fails if the recovery treats the second END-IF as the first IF's own (the record of a statement the recovery
*> left outliving the terminator the fault stood at) and draws nothing for it.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4382STRAYENDIF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 END-IF DISPLAY "X" END-IF.
    STOP RUN.
