*>--- conformance-case
*> id: pb4239-cobol-words-substitute-synonym-of-extension-word-is-reserved
*> rules: [GR-7.3.10.4-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.4 GR4 (cite.py --check 7.3.10.4 "the content of literal-4 shall no longer be a reserved word, a
*> context-sensitive word, nor an intrinsic function name within this compilation group" -> OK 4)): JSN replaces JSON, a
*> reserved word of this implementation (4.2.10 3; 8.3.2.1 1)), so JSN is reserved in its place and cannot be a data-name,
*> while JSON is freed. The refusal is at the use, and its sentence names the SUBSTITUTE and GR4 (review R1-1 of kb/Work PB4239).
       >>COBOL-WORDS SUBSTITUTE "JSON" BY "JSN"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4239E9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 JSN PIC X VALUE "C".
       PROCEDURE DIVISION.
           DISPLAY JSN.
           STOP RUN.
