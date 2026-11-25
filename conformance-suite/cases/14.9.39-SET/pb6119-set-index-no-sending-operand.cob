*>--- conformance-case
*> id: pb6119-set-index-no-sending-operand
*> rules: [FMT-14.9.39.2, 1985-FMT-VI.6.23.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6119. §2023:14.9.39.2 Format 1 prints 'SET { index-name-1 | identifier-1 } ... TO { arithmetic-expression-1 | index-name-2 | identifier-2 }' with the
*> sending operand required (§1985:VI.6.23.2 the same). This SET ends at the separator period right after TO. The operand that is due may
*> open with a '(' (arithmetic-expression-1), and the bare COBOL0001 came from reading that '(' as a sign that an element before the period
*> was complete. WHAT THIS PINS: the refusal names the SET statement and the operand it still needs (COBOL5506, 'ends the SET
*> statement where an operand is still due'), not the bare COBOL0001 "cannot parse construct near '.'". The case fails if the bare error returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6119S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9.
       01  I PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           SET I TO.
           STOP RUN.
