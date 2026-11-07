*>--- conformance-case
*> id: pb4573_cobol_words_substitute_comma_is_the_decimal_point_word
*> rules: [GR-7.3.10.4-4, GR-12.3.7.4-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR4 (kb/Work PB4573): "When the SUBSTITUTE option is specified, the COBOL word that is the
*> content of literal-5 shall be used in any syntax where the COBOL word that is the content of literal-4 is
*> documented as required or optional."
*>   cite.py: OK  §2023:7.3.10.4 4)  (General rules)
*> 12.3.7.2 general format: [ DECIMAL-POINT IS COMMA ]  (cite.py: OK  §2023:12.3.7.2 (General format)). The clause requires
*> the reserved word COMMA (8.9), so CMA, which SUBSTITUTE put in its place, is the word the clause is written with.
*> 12.3.7.4 GR14: "If the DECIMAL-POINT IS COMMA clause is specified, the functionality of the characters used to
*> represent the decimal separator and the grouping separator are exchanged."
*>   cite.py: OK  §2023:12.3.7.4 14)  (General rules)
*> Why each leg can fail: DP is PIC 9,99 = one integer digit, the decimal point, two fractional digits; the literal
*> 1,5 is one and a half (GR14: the comma is the decimal separator). The item holds 1.50 and DISPLAY prints it in its
*> edited form, 1,50. If the clause were refused, the literal 1,5 and the picture 9,99 would be refused with it.
*> EXPECTED OUTPUT: 1,50
       >>COBOL-WORDS SUBSTITUTE "COMMA" BY "CMA"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4573SC.
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
