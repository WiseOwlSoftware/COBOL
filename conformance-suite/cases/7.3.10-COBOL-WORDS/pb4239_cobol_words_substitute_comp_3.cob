*>--- conformance-case
*> id: pb4239_cobol_words_substitute_comp_3
*> rules: [SR-7.3.10.3-3, GR-7.3.10.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.3 SR3 (cite.py --check 7.3.10.3 "The content of literal-1, literal-3, and literal-4 shall be a
*> reserved word, a context-sensitive word, or an intrinsic function name" -> OK 3)) with 4.2.10 3) and 8.3.2.1 1): COMP-3 is a
*> reserved word this implementation adds (docs/CONFORMANCE.md D-RW1), so SUBSTITUTE "COMP-3" BY "PACK3" names a reserved
*> word and is legal. 7.3.10.4 GR4 (cite.py --check 7.3.10.4 "the content of literal-4 shall no longer be a reserved word,
*> a context-sensitive word, nor an intrinsic function name within this compilation group" -> OK 4)): COMP-3 is a data-name
*> below, and PACK3 now stands for the packed-decimal usage it was. S9(5) packed takes 3 bytes.
*> Leg that can fail: the directive refused (COBOL1623), or PACK3 not taking the usage.
       >>COBOL-WORDS SUBSTITUTE "COMP-3" BY "PACK3"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4239E3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMP-3 PIC X VALUE "C".
       01 A PIC S9(5) USAGE PACK3 VALUE 12.
       PROCEDURE DIVISION.
           DISPLAY COMP-3 " " A " " FUNCTION BYTE-LENGTH(A).
           STOP RUN.
