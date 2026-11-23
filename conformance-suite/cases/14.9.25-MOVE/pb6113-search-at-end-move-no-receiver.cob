*>--- conformance-case
*> id: pb6113-search-at-end-move-no-receiver
*> rules: [FMT-14.9.25.2, 1985-FMT-VI.6.19.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6113. §2023:14.9.25.2 Format 1 prints 'MOVE ... TO { identifier-2 } ...' with identifier-2 in unbracketed braces,
*> and §1985:VI.6.19.2 prints it the same way: the receiving operand after TO is required. This MOVE stands in the AT END
*> phrase of a SEARCH and ends at the separator period right after TO, so it lacks its receiver and is refused at every
*> edition. WHAT THIS PINS: the refusal names the MOVE statement and the operand it still needs (COBOL5506, 'ends the
*> MOVE statement where an operand is still due'), not the bare COBOL0001 "unexpected '.'".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6113SM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E PIC 9 OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH E AT END MOVE 1 TO.
           STOP RUN.
