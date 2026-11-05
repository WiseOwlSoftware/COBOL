*>--- conformance-case
*> id: pb4722-goback-raising-null-position
*> rules: [SR-14.9.18.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.18.3 SR4 - identifier-1 shall be an object reference; NULL references no object, so the
      *> refusal (COBOL4211) is reported at the word NULL (kb/Work PB4722), not at the GOBACK keyword.
      *> The word sits at (line 17, column 27) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4722A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           GOBACK RAISING NULL.
