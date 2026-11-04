*>--- conformance-case
*> id: pb4239-cobol-words-extension-word-is-no-fresh-word
*> rules: [SR-7.3.10.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.3 SR4 (cite.py --check 7.3.10.3 "The content of literal-2, literal-5, and literal-6 shall not be a
*> reserved word, a context-sensitive word, nor an intrinsic function-name" -> OK 4)): COMP-3 is a reserved word of this
*> implementation (4.2.10 3) additional reserved words; 8.3.2.1 1)), so it cannot be the new word of an EQUATE. The refusal
*> names the reservation (docs/CONFORMANCE.md D-RW1). Before kb/Work PB4239 this compiled clean.
       >>COBOL-WORDS EQUATE "MOVE" WITH "COMP-3"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4239E5.
       PROCEDURE DIVISION.
           DISPLAY "A".
           STOP RUN.
