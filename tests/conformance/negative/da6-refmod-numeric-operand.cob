*> reject-at: 85 2002 2014 2023
*> ISO 8.8.1.1 with 8.4.2.4 - a reference-modified slice is class ALPHANUMERIC, so it is not a numeric
*> operand. The third shape of the same rule. --permissive accepts it.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGDA6REFMODNUMERIC.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(4) VALUE "0012".
01 R PIC 9(6).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = X(1:2) + 1.
    STOP RUN.
