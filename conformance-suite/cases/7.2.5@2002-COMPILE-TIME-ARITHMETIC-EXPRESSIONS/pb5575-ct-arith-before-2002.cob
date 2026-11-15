*>--- conformance-case
*> id: pb5575-ct-arith-before-2002
*> rules: [2002-GR-7.2.5.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5575. Compile-time arithmetic expressions, and the constant entry that holds one, are COBOL-2002
*> introductions (§2002:7.2.5 Compile-time arithmetic expressions; §2002:13.9 Constant entry), so a COBOL-85
*> program that writes one is refused: the same program the 2002 test pb5575-ct-arith-32-digit-truncation runs.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5575N.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K2 CONSTANT AS 2 / 3 * 3.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "K2=" K2
    STOP RUN.
