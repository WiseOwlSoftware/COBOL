*>--- conformance-case
*> id: pb4572_cobol_words_synonym_of_comma_is_the_decimal_point_word
*> rules: [GR-7.3.10.4-2, GR-12.3.7.4-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR2 (kb/Work PB4572): "When the EQUATE option is specified, the COBOL word that is the
*> content of literal-2 shall be treated as a synonym for the COBOL word that is the content of literal-1, and may
*> be used in any syntax requiring the use of the reserved word, context-sensitive word, or intrinsic function name
*> that is the content of literal-1."
*>   cite.py: OK  §7.3.10.4 2)  (General rules)
*> 12.3.7.2 general format: [ DECIMAL-POINT IS COMMA ]  (cite.py: OK  §12.3.7.2 (General format)). The clause requires
*> the reserved word COMMA (8.9), so CMA, its synonym, is the word the clause requires.
*> 12.3.7.4 GR14: "If the DECIMAL-POINT IS COMMA clause is specified, the functionality of the characters used to
*> represent the decimal separator and the grouping separator are exchanged."
*>   cite.py: OK  §12.3.7.4 14)  (General rules)
*> COMMA is a reserved word the lexer has no token for, so the directive reaches a synonym of it only by name. The
*> binder decided the clause by comparing the written word with COMMA, so CMA was refused (COBOL0894) and the program
*> that follows never compiled.
*> Why each leg can fail: DP is PIC 9,99 = one integer digit, the decimal point, two fractional digits; the literal
*> 1,5 is one and a half (GR14: the comma is the decimal separator). The item holds 1.50 and DISPLAY prints it in its
*> edited form, 1,50. If the clause were not in effect the literal 1,5 and the picture 9,99 would be refused.
*> EXPECTED OUTPUT: 1,50
       >>COBOL-WORDS EQUATE "COMMA" WITH "CMA"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4572DP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS CMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DP PIC 9,99 VALUE 1,5.
       PROCEDURE DIVISION.
           DISPLAY DP
           STOP RUN.
