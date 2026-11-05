*>--- conformance-case
*> id: pb4722-goback-raising-ecuser-not-in-pd-position
*> rules: [SR-14.9.18.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.18.3 SR2 - an EC-USER exception-name shall be specified in the RAISING phrase of the
      *> procedure division header. The refusal (COBOL0717) is reported at the exception-name word
      *> (kb/Work PB4722), not at the GOBACK keyword. The statement is at line 17, its keyword at column 12.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4722G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           GOBACK RAISING EXCEPTION EC-USER-FOO.
