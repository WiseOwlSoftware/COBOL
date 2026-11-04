*>--- conformance-case
*> id: pb4573-cobol-words-synonym-of-display-is-no-operand-word
*> rules: [GR-7.3.10.4-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR2 (kb/Work PB4573): "When the EQUATE option is specified, the COBOL word that is the
*> content of literal-2 shall be treated as a synonym for the COBOL word that is the content of literal-1, and may
*> be used in any syntax requiring the use of the reserved word, context-sensitive word, or intrinsic function name
*> that is the content of literal-1."
*>   cite.py: OK  §7.3.10.4 2)  (General rules)
*> 8.3.2.1 rule 1: "Reserved words shall not be used as user-defined words or system-names." SHOW, a synonym for the
*> reserved word DISPLAY, is DISPLAY in this compilation group, so MOVE "A" TO SHOW names no data item and the program
*> is refused (COBOL0901). The refusal named the original, "'DISPLAY' is a reserved word", which the source never wrote;
*> it now names SHOW and the word it stands for, as the report of a synonym the lexer has no token for always did.
       >>COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4573SW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X.
       PROCEDURE DIVISION.
           MOVE "A" TO SHOW
           STOP RUN.
