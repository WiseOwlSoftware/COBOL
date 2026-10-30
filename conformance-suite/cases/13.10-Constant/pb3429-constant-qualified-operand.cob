*>--- conformance-case
*> id: pb3429-constant-qualified-operand
*> rules: [SR-7.3.6.2-1, SR-13.10.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.6.2 SR1b (cite.py --check 7.3.6.2 "fixed-point numeric literals" -> OK): every operand of a
*> compile-time arithmetic expression shall be a fixed-point numeric literal, and 13.10.3 SR7 (cite.py --check
*> 13.10.3 "all of the operands shall be literals" -> OK) holds a constant entry's operands to literals.
*> FOO OF X is a qualified data reference to a storage item, neither (kb/Work PB3429). The refusal
*> QUOTES the operand as the programmer wrote it, `FOO OF X`, never the run-together token text `FOOOFX`.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3429N01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X.
   05 FOO PIC 9.
01 C CONSTANT AS FOO OF X.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
