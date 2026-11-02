*>--- conformance-case
*> id: pb4382-evaluate-when-group-without-statement
*> rules: [FMT-14.9.13.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4382, PB4300. ISO 14.9.13.2 prints
*>     { { WHEN selection-object [ ALSO selection-object ] ... } ... imperative-statement-1 } ...
*>     [ WHEN OTHER imperative-statement-2 ]
*>   cite.py: OK  14.9.13.2  (General format) - "imperative-statement-2"
*> imperative-statement-1 stands inside the outer braces with no bracket, so each WHEN group requires it (ISO
*> 5.2.6.2 licenses an omission only for a bracketed portion; cite.py OK 5.2.6.2). WHEN 1 below has none, and the
*> WHEN OTHER phrase after it is written once, after the WHEN group: in its place. The refusal is COBOL2072 (the
*> group's statement is missing). It fails if the report at OTHER names a misplaced or repeated WHEN OTHER phrase
*> (COBOL2073), which the program does not hold.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4382WHENGROUP.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-A PIC X VALUE "A".
PROCEDURE DIVISION.
MAIN-P.
    EVALUATE W-N
        WHEN 1
        WHEN OTHER
            DISPLAY W-A
    END-EVALUATE
    STOP RUN.
