*>--- conformance-case
*> id: pb5575-rt-std-arith-before-2002
*> rules: [2002-GR-8.8.1.3.1.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5575. The OPTIONS paragraph and its ARITHMETIC clause are COBOL-2002 introductions (§2002:8.8.1.3 says
*> standard arithmetic is in effect "when the ARITHMETIC IS STANDARD clause is specified in the OPTIONS paragraph"), so a
*> COBOL-85 program that writes it is refused: the program of the 2002 test pb5575-rt-std-arith-32-digit-truncation
*> in miniature.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5575S.
OPTIONS.
    ARITHMETIC IS STANDARD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 C PIC 9V9(5).
01 E2 PIC 9 VALUE 2.
01 D3 PIC 9 VALUE 3.
PROCEDURE DIVISION.
MAIN-PARA.
    COMPUTE C = E2 / D3 * D3
    DISPLAY "C=" C
    STOP RUN.
