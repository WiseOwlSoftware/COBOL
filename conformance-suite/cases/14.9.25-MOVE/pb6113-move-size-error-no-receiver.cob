*>--- conformance-case
*> id: pb6113-move-size-error-no-receiver
*> rules: [FMT-14.9.25.2, 1985-FMT-VI.6.19.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6113. A MOVE with no receiver written in the ON SIZE ERROR phrase of an ADD (a phrase that holds an imperative
*> statement) lacks identifier-2 of §2023:14.9.25.2 (§1985:VI.6.19.2 the same) and is refused at every
*> edition. WHAT THIS PINS: the refusal names the MOVE statement and the operand it still needs (COBOL5506, 'ends the MOVE
*> statement where an operand is still due'), as it does outside a phrase, not the bare COBOL0001 "unexpected '.'".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6113MS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO N ON SIZE ERROR MOVE 1 TO.
           STOP RUN.
