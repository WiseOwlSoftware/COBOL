*>--- conformance-case
*> id: pb4309-cobol-words-substitute-intrinsic-name
*> rules: [GR-7.3.10.4-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 7.3.10.4 GR4 (kb/Work PB4309): after SUBSTITUTE "LENGTH" BY "SIZEOF", "the content of literal-4 shall no longer be
*> ... an intrinsic function name within this compilation group" and literal-5 is used where LENGTH is documented. FUNCTION
*> LENGTH is therefore refused, and the refusal (COBOL5776) names the SUBSTITUTE and the spelling that now denotes the
*> function. The accepted spelling is the pb1372_cobol_words_substitute_keyword_functions family's.
       >>COBOL-WORDS SUBSTITUTE "LENGTH" BY "SIZEOF"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4309SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(4).
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N = FUNCTION LENGTH(S).
           STOP RUN.
