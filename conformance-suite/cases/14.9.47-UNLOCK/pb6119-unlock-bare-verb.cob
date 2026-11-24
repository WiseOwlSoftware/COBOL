*>--- conformance-case
*> id: pb6119-unlock-bare-verb
*> rules: [FMT-14.9.47.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6119. §2023:14.9.47.2 prints 'UNLOCK file-name-1 { RECORD | RECORDS }', file-name-1 required. UNLOCK is a reserved word from COBOL-2002 (§2002:8.9; §2023:8.9),
*> so from there 'UNLOCK.' cannot be a paragraph header and is the UNLOCK statement without its operand (a COBOL-85 program may name a paragraph
*> UNLOCK, so 85 is not refused here). The parser weighed the paragraph header against the statement and the report named no statement. WHAT THIS PINS: the refusal names the UNLOCK statement and the operand it still needs (COBOL5506, 'ends the UNLOCK
*> statement where an operand is still due'), not the bare COBOL0001 "cannot parse construct near '.'". The case fails if the bare error returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6119U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9.
       01  I PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           UNLOCK.
           STOP RUN.
