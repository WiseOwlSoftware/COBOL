*>--- conformance-case
*> id: pb4132-stray-end-display
*> rules: [GR-14.5.3.2-1, SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4132. ISO 14.5.3.2: "The scope of a statement may be explicitly
*> terminated by using its associated scope terminator as specified in Table 12"
*> and "An explicit scope terminator terminates the scope of: 1) the
*> most-recently preceding unterminated statement having the statement-name for
*> which that scope terminator is defined" (cite.py --check 14.5.3.2 -> OK).
*> END-DISPLAY below follows a complete MOVE and no DISPLAY statement is open, so
*> it terminates nothing: refused COBOL0306, the misplaced scope terminator, as a
*> stray END-ADD in the same place is.
*> END-DISPLAY is reserved from 2002 (ISO 8.9), so there it is that keyword and
*> never a user-defined word: ISO 8.3.2.1 1) "Reserved words shall not be used as
*> user-defined words or system-names" (cite.py --check 8.3.2.1 -> OK) is not what
*> the source breaks, although the MOVE's receiving list would take another
*> operand. It used to draw COBOL0901 ("cannot be used as a user-defined word"):
*> the syntax-error listener wrote the reservation over every report on a
*> reservation-gated word. At 85, where 8.9 leaves END-DISPLAY free, the same
*> text is a second receiving operand that names nothing, a different refusal,
*> so 85 is not listed.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4132SD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9 VALUE 0.
01 W-K PIC 9 VALUE 0.
PROCEDURE DIVISION.
MAIN-P.
    MOVE 1 TO W-N END-DISPLAY
    MOVE 2 TO W-K.
    DISPLAY W-N W-K.
    STOP RUN.
