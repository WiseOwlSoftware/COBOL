*>--- conformance-case
*> id: pb1746-linage-counter-unstring-sender
*> rules: [GR-8.4.3.14.4-1, SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1746 - LINAGE-COUNTER IS CATEGORY NUMERIC TOO, SO UNSTRING DOES NOT SEND IT.
*> ISO 14.9.48.3 SR2: "Identifier-1, identifier-2, identifier-3, and identifier-5 shall reference data
*> items of category alphanumeric or national." LINAGE-COUNTER is "a temporary unsigned integer data item
*> of class and category numeric" (8.4.3.14.4 GR1). It IS usage display (its size is the page size's digits),
*> so STRING and the alphanumeric relation take it, but its category is the one UNSTRING refuses. The sender
*> screen had no arm for a register (a computed operand), so this compiled and split the register's image.
*> Edition-invariant: SR2 is unchanged at 85/2002/2014/2023.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1746LUS.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "pb1746lus.txt".
DATA DIVISION.
FILE SECTION.
FD PRT LINAGE IS 20 LINES.
01 PRT-REC PIC X(10).
WORKING-STORAGE SECTION.
01 X4 PIC X(4).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT PRT.
    MOVE "AA" TO PRT-REC.
    WRITE PRT-REC.
    UNSTRING LINAGE-COUNTER OF PRT DELIMITED BY "0" INTO X4.
    CLOSE PRT.
    STOP RUN.
