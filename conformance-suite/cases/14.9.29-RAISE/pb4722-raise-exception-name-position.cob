*>--- conformance-case
*> id: pb4722-raise-exception-name-position
*> rules: [SR-14.9.29.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.29.3 SR1 - RAISE EXCEPTION names a level-3 exception-name; the refusal (COBOL0711) is
      *> reported at the exception-name word (kb/Work PB4722), not at the RAISE keyword.
      *> The statement below is at line 17, its keyword at column 12.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4722B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           RAISE EXCEPTION EC-BOGUS.
