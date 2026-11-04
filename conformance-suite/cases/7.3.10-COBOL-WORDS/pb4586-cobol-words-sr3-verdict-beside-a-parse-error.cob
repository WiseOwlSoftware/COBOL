*>--- conformance-case
*> id: pb4586-cobol-words-sr3-verdict-beside-a-parse-error
*> rules: [SR-7.3.10.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.3 SR3 (cite.py --check 7.3.10.3 "The content of literal-1, literal-3, and literal-4 shall be a
*> reserved word, a context-sensitive word, or an intrinsic function name" -> OK 3)): AUTHOR is in none of 8.9, 8.10 and
*> 8.11, so the directive violates SR3 whether or not the program it heads parses. The statement MOVE TO. is a syntax
*> error; the COBOL1623 verdict on the directive is reported beside it. Before kb/Work PB4586 only the parse error was.
       >>COBOL-WORDS UNDEFINE "AUTHOR"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4586N1.
       PROCEDURE DIVISION.
           MOVE TO.
           STOP RUN.
