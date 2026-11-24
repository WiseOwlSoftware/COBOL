*>--- conformance-case
*> id: pb6119-compute-no-expression
*> rules: [FMT-14.9.8.2, 1985-FMT-VI.6.8.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6119. §2023:14.9.8.2 Format 1 prints 'COMPUTE { identifier-1 [ rounded-phrase ] } ... = arithmetic-expression-1' (§1985:VI.6.8.2 the same), the
*> expression required. This COMPUTE ends at the separator period right after '='. An arithmetic expression may open with a '(', which the
*> operand-due sentence once read as a sign that an element before the period was complete. WHAT THIS PINS: the refusal names the COMPUTE statement and the operand it still needs (COBOL5506, 'ends the COMPUTE
*> statement where an operand is still due'), not the bare COBOL0001 "cannot parse construct near '.'". The case fails if the bare error returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6119C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9.
       01  I PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE N =.
           STOP RUN.
