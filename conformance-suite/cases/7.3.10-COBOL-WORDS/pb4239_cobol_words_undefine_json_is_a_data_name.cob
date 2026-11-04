*>--- conformance-case
*> id: pb4239_cobol_words_undefine_json_is_a_data_name
*> rules: [SR-7.3.10.3-3, GR-7.3.10.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.3 SR3 (cite.py --check 7.3.10.3 "The content of literal-1, literal-3, and literal-4 shall be a
*> reserved word, a context-sensitive word, or an intrinsic function name" -> OK 3)): JSON is a reserved word of this
*> implementation (4.2.10 3: "additional reserved words" as nonstandard extensions; 8.3.2.1 1) "Reserved words shall not be
*> used as user-defined words"), so UNDEFINE "JSON" names a reserved word and is legal. 7.3.10.4 GR3 (OK 3)): the word "shall
*> no longer be reserved or restricted in any way, and may be used as a user-defined intrinsic name, data-name or any other
*> user-defined word", so JSON is the data-name below. D.12.3: UNDEFINE "can be useful when a change of compiler that includes
*> new reserved words that conflict with existing code" (owner decision 2026-10-31, kb/Work PB4239).
*> Leg that can fail: the directive refused with COBOL1623 "is not a reserved word" for a word the compiler reserves.
       >>COBOL-WORDS UNDEFINE "JSON"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4239E1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 JSON PIC X VALUE "J".
       PROCEDURE DIVISION.
           DISPLAY JSON.
           STOP RUN.
