*>--- conformance-case
*> id: pb4722-raise-level2-name-position
*> rules: [SR-14.9.29.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.29.3 SR1 - only a level-3 exception-name may be raised; EC-SIZE is level 2. The refusal
      *> (COBOL0710) is reported at the exception-name word (kb/Work PB4722), not at the RAISE keyword.
      *> The statement below is at line 17, its keyword at column 12.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4722F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           RAISE EXCEPTION EC-SIZE.
