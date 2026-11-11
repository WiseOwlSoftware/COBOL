*>--- conformance-case
*> id: pb5074_display_decimal_comma_literal
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §2023:14.9.11.4 GR1 (Annex A.1 item 56; kb/Work PB3505, PB1643): under DECIMAL-POINT IS
      *> COMMA a numeric literal is shown as written, its separator the program's comma. COBOL-85 refuses
      *> the literal (1985 VI.6.10.3 SR2: "an unsigned integer"; kb/Work PB5074), so the case runs from
      *> 2002, where the literal of a DISPLAY has no such rule. Expected: LIT=12,50|.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074DC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "LIT=" 12,50 "|"
           STOP RUN.
