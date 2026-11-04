*>--- conformance-case
*> id: pb4239-cobol-words-reserve-extension-word
*> rules: [SR-7.3.10.3-4, GR-7.3.10.4-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.3 SR4 (cite.py --check 7.3.10.3 "The content of literal-2, literal-5, and literal-6 shall not be a
*> reserved word, a context-sensitive word, nor an intrinsic function-name" -> OK 4)): literal-6 of RESERVE shall not be a
*> reserved word, and JSON is one of this implementation (4.2.10 3); 8.3.2.1 1)). RESERVE "JSON" adds a word the compiler
*> already reserves; the standard refuses it, though GnuCOBOL accepts it. Before kb/Work PB4239 this compiled clean.
       >>COBOL-WORDS RESERVE "JSON"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4239E6.
       PROCEDURE DIVISION.
           DISPLAY "A".
           STOP RUN.
