*>--- conformance-case
*> id: pb4239_cobol_words_substitute_json_is_a_data_name
*> rules: [SR-7.3.10.3-3, GR-7.3.10.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.4 GR4 (cite.py --check 7.3.10.4 "the content of literal-4 shall no longer be a reserved word, a
*> context-sensitive word, nor an intrinsic function name within this compilation group" -> OK 4)): SUBSTITUTE "JSON" BY "JSN"
*> frees JSON, a reserved word of this implementation (4.2.10 3; SR3 allows it), so JSON is the data-name below, and JSN, which
*> replaces it, is not used as one. Review R1-1 of kb/Work PB4239: the refusal of JSN as a data-name names GR4, never GR2.
*> Leg that can fail: JSON still refused as a user-defined word after the SUBSTITUTE.
       >>COBOL-WORDS SUBSTITUTE "JSON" BY "JSN"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4239E8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 JSON PIC X VALUE "D".
       PROCEDURE DIVISION.
           DISPLAY JSON.
           STOP RUN.
