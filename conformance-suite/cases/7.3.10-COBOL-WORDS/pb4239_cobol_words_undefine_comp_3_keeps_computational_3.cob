*>--- conformance-case
*> id: pb4239_cobol_words_undefine_comp_3_keeps_computational_3
*> rules: [SR-7.3.10.3-3, GR-7.3.10.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.3 SR3 (cite.py --check 7.3.10.3 "The content of literal-1, literal-3, and literal-4 shall be a
*> reserved word, a context-sensitive word, or an intrinsic function name" -> OK 3)) with 4.2.10 3) and 8.3.2.1 1): COMP-3 is a
*> reserved word this implementation adds (docs/CONFORMANCE.md D-RW1), so UNDEFINE "COMP-3" is legal and, by 7.3.10.4 GR3,
*> frees only that word: COMP-3 is a data-name here, and COMPUTATIONAL-3, a different word, still names the packed usage.
*> S9(5) packed takes ceil((5+1)/2) = 3 bytes, and DISPLAY of a numeric item shows its decimal point and sign: +00012.
*> Leg that can fail: the directive refused (COBOL1623), or the freed word taking COMPUTATIONAL-3 with it.
       >>COBOL-WORDS UNDEFINE "COMP-3"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4239E2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMP-3 PIC X VALUE "C".
       01 A PIC S9(5) USAGE COMPUTATIONAL-3 VALUE 12.
       PROCEDURE DIVISION.
           DISPLAY COMP-3 " " A " " FUNCTION BYTE-LENGTH(A).
           STOP RUN.
