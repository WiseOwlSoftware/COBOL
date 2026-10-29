*>--- conformance-case
*> id: pb3996-call-format1-omitted-bare
*> rules: [FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.2 Format 1 (Program) prints its USING phrase as
      *> `[ BY REFERENCE ] { identifier-2 } ...` or `BY CONTENT { identifier-2 } ...`:
      *> the word OMITTED appears only in Format 2 (program-prototype). A CALL with
      *> literal-1 and no AS phrase is Format 1, so the bare OMITTED argument is
      *> illegal source (kb/Work PB3996). The binder narrowed every other Format-2-only
      *> spelling and missed this arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3996NA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           CALL "OTHER" USING W OMITTED
           STOP RUN.
