*>--- conformance-case
*> id: pb4722-exit-program-raising-null-position
*> rules: [SR-14.9.14.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.14.3 SR5 - identifier-1 shall be an object reference; NULL references no object, so the
      *> refusal (COBOL4211) is reported at the word NULL (kb/Work PB4722), not at the EXIT keyword.
      *> The word sits at (line 17, column 33) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4722B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           EXIT PROGRAM RAISING NULL.
