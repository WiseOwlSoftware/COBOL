*>--- conformance-case
*> id: pb6119-accept-bare-verb
*> rules: [FMT-14.9.1.2, 1985-FMT-VI.6.5.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6119. §2023:14.9.1.2 Format 1 prints 'ACCEPT identifier-1 [ FROM mnemonic-name-1 ] ...' (§1985:VI.6.5.2 the same), identifier-1 required. ACCEPT is a
*> reserved word at every edition (§2023:8.9), so the sentence 'ACCEPT.' cannot be a paragraph header and is the ACCEPT statement without
*> its operand. The parser's choice among the statements that begin with ACCEPT failed before any statement was entered, and the report
*> named no statement. WHAT THIS PINS: the refusal names the ACCEPT statement and the operand it still needs (COBOL5506, 'ends the ACCEPT
*> statement where an operand is still due'), not the bare COBOL0001 "cannot parse construct near '.'". The case fails if the bare error returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6119A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9.
       01  I PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT.
           STOP RUN.
