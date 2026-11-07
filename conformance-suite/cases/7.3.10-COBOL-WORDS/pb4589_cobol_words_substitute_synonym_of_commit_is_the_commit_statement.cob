*>--- conformance-case
*> id: pb4589_cobol_words_substitute_synonym_of_commit_is_the_commit_statement
*> rules: [GR-7.3.10.4-4, GR-14.9.7.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR4 (kb/Work PB4589): "When the SUBSTITUTE option is specified, the COBOL word that is the
*> content of literal-5 shall be used in any syntax where the COBOL word that is the content of literal-4 is
*> documented as required or optional. The COBOL word that is the content of literal-4 may then be used as a
*> user-defined word".
*>   cite.py: OK  §2023:7.3.10.4 4)  (General rules)
*> So CMT is the COMMIT statement of 14.9.7, and COMMIT is a user-defined word, here the data item COMMIT.
*> 14.9.7.4 GR1: with no active APPLY COMMIT clause the statement "has the same effect as a CONTINUE statement with
*> no additional phrases" (pb137_commit_inert.cob cites it; cite.py: OK  §2023:14.9.7.4 1)  (General rules)).
*> Why each leg can fail: CMT is reserved as the COMMIT statement's word (8.3.2.1 rule 1), so it is a statement that
*> ends the DISPLAY before it. Read as an operand, the retyped token's text COMMIT named the item and the line
*> showed "AC" (the item's value beside the literal) instead of "A". The item COMMIT still holds "C".
*> EXPECTED OUTPUT: A, then C.
>>COBOL-WORDS SUBSTITUTE "COMMIT" BY "CMT"
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4589CM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 COMMIT PIC X VALUE "C".
PROCEDURE DIVISION.
    DISPLAY "A" CMT.
    DISPLAY COMMIT
    STOP RUN.
