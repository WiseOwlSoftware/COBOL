*>--- conformance-case
*> id: external-linage-externality-at-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.8.4.2 conjunct 1 (Annex E.2 item 9): at >=2023 a LINAGE data item of an external
*> file connector shall ITSELF be an external data item. Here a SINGLE program declares
*> FD F IS EXTERNAL LINAGE IS WS-LN LINES, where WS-LN is a plain (non-external) item. A
*> LITERAL LINAGE operand would be exempt. The 1985 text has it too (§1985:VII.3.7.4 GR11c,
*> pb5982-external-linage-not-external); 2002 and 2014 are not screened (kb/Work PB5982).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XLNE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN "fdata".
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL LINAGE IS WS-LN LINES.
       01 REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-LN PIC 99 VALUE 10.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
