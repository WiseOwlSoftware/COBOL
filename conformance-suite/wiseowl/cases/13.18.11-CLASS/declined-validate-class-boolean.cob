*>--- conformance-case
*> id: declined-validate-class-boolean
*> rules: [FMT-13.18.11.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.11.2 CLASS clause general format: CLASS IS { NUMERIC | ALPHABETIC | ALPHABETIC-LOWER |
*> ALPHABETIC-UPPER | BOOLEAN | alphabet-name-1 | class-name-1 }, BOOLEAN underlined - the clause's own
*> keyword, never a user-defined word. The clause is DECLINED with the VALIDATE facility (Annex A.4.14, owner
*> decision kb/Work PB375), so it is refused BY NAME, COBOL1708, whichever operand is written.
*> kb/Work PB3831: BOOLEAN drew COBOL0901 "'BOOLEAN' is a reserved word ... cannot be used as a
*> user-defined word" instead, because the operand rule had no BOOLEAN arm and the word fell to cobolWord,
*> which refuses a reserved word at 2002 and later. Why this can fail: drop the BOOLEAN arm of
*> validateClassOperand (Core/CobolDeclined.g4) and the compile reports COBOL0901 and no COBOL1708.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLCLSBL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC X(1) CLASS IS BOOLEAN VALUE "1".
       PROCEDURE DIVISION.
           DISPLAY WS-B.
           STOP RUN.
