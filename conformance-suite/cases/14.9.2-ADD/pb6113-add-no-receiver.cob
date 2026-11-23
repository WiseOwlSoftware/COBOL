*>--- conformance-case
*> id: pb6113-add-no-receiver
*> rules: [FMT-14.9.2.2, 1985-FMT-VI.6.6.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6113. §2023:14.9.2.2 Format 1 prints '... TO { identifier-2 [ rounded-phrase ] } ...' with the receiving operand in
*> unbracketed braces, and §1985:VI.6.6.2 prints it the same way. This ADD ends at the separator period right after TO, so it
*> lacks its receiver and is refused at every edition. WHAT THIS PINS: the refusal names the ADD statement and the operand it
*> still needs (COBOL5506, 'ends the ADD statement where an operand is still due'), not the bare COBOL0001 "unexpected '.'".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6113AD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO.
           STOP RUN.
