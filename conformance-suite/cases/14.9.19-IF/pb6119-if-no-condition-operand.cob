*>--- conformance-case
*> id: pb6119-if-no-condition-operand
*> rules: [FMT-14.9.19.2, 1985-FMT-VI.6.16.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6119. §2023:14.9.19.2 Format 1 prints 'IF condition-1 THEN statement-1 ...' (§1985:VI.6.16.2 the same). The condition N = needs the right operand of
*> its relation, and this IF ends at the separator period where that operand is due; the operand may open with a '('. WHAT THIS PINS: the refusal names the IF statement and the operand it still needs (COBOL5506, 'ends the IF
*> statement where an operand is still due'), not the bare COBOL0001 "cannot parse construct near '.'". The case fails if the bare error returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6119I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9.
       01  I PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           IF N = .
           STOP RUN.
