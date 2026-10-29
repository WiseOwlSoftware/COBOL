*>--- conformance-case
*> id: pb3996-call-format1-omitted-by-content
*> rules: [FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.2 Format 1 (Program) prints `BY CONTENT { identifier-2 } ...`, and
      *> the BY CONTENT phrase's transitivity (14.9.4.4 GR5) does not make OMITTED an
      *> identifier-2: in `USING BY CONTENT W OMITTED` the second operand is the bare
      *> OMITTED argument, which only Format 2 (program-prototype) prints. A CALL with
      *> literal-1 and no AS phrase is Format 1, so the statement is illegal source
      *> (kb/Work PB3996).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3996NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           CALL "OTHER" USING BY CONTENT W OMITTED
           STOP RUN.
