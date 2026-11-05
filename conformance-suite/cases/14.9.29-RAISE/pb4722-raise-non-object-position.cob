*>--- conformance-case
*> id: pb4722-raise-non-object-position
*> rules: [SR-14.9.29.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.29.3 SR2 - RAISE identifier-1 shall be an object reference; the refusal (COBOL0848) is
      *> reported at identifier-1 (kb/Work PB4722), not at the RAISE keyword.
      *> The statement below is at line 17, its keyword at column 12.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4722C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           RAISE X.
