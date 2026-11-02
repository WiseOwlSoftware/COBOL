*>--- conformance-case
*> id: pb4300-evaluate-only-when-other-after-incomplete-display
*> rules: [FMT-14.9.13.2, FMT-14.9.11.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4300. ISO 14.9.11.2 prints DISPLAY's operand in braces, not brackets, so one is required
*>   cite.py: OK  14.9.11.2  (General format) - "mnemonic-name-1 ] [ WITH"
*> and the DISPLAY before WHEN OTHER below has none: that is the fault, at the WHEN. The WHEN OTHER phrase is the
*> program's only one, written after the WHEN group, which ISO 14.9.13.2 prints it after
*>   cite.py: OK  14.9.13.2  (General format) - "imperative-statement-2"
*> so the refusal is a syntax error at the WHEN that names no misplaced phrase. It fails if the report is COBOL2073
*> "A WHEN OTHER phrase may be specified at most once, and only after every WHEN phrase", which is false of the
*> program.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4300ONLYOTHER.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    EVALUATE TRUE
        WHEN W-N = 2 DISPLAY
        WHEN OTHER DISPLAY "O"
    END-EVALUATE
    STOP RUN.
