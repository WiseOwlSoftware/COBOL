*>--- conformance-case
*> id: pb4425-literal-where-the-next-sentence-begins
*> rules: [GR-14.5.3.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4425 - a literal written where the next sentence of a paragraph begins is named by the diagnostic,
*> not left the bare COBOL0001. The separator period after DISPLAY "A" terminates its statement (ISO 14.5.3.3
*> 1) c), cite.py OK 14.5.3.3) and its sentence ("A sentence is a sequence of one or more procedural
*> statements, the last of which is terminated by a separator period", 14.5.1, cite.py OK), so the next
*> sentence may begin at "B". A statement "begins with the first word of the statement name" (14.5.3.1,
*> cite.py OK), and a literal is none. The refusal is the statement arm of the written-kind refusal (COBOL5506,
*> PB4224) at every edition. It fails if the report is the bare COBOL0001 the parse gives at the program's end,
*> where it stands once every sentence, paragraph and section has refused the literal.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4425NEXTSENT.
PROCEDURE DIVISION.
MAIN-P.
    DISPLAY "A". "B".
    STOP RUN.
