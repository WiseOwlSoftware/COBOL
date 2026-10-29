*>--- conformance-case
*> id: pb3996-call-format1-omitted-by-reference
*> rules: [FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.2 Format 1 (Program) prints `[ BY REFERENCE ] { identifier-2 } ...`:
      *> OMITTED is not one of its operands. Format 2 (program-prototype) prints
      *> `[ BY REFERENCE ] { identifier-2 | OMITTED }`. A CALL with literal-1 and no AS
      *> phrase is Format 1, so `BY REFERENCE OMITTED` is illegal source (kb/Work PB3996).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3996NR.
       PROCEDURE DIVISION.
       MAIN.
           CALL "OTHER" USING BY REFERENCE OMITTED
           STOP RUN.
