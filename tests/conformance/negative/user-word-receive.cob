*> reject-at: 2023
*> RECEIVE (the STATEMENT is §14.9.31; this fixture is about the WORD) is NON-MONOTONIC in §8.9's
*> reserved-word list: reserved at 85, a USER WORD at 2002 and 2014, re-reserved at 2023
*> (Annex E.2 item 25). Wave H gave it a hard lexer token, so this fixture pins that the cobolWord nameSlot
*> funnel still lets it be a data-name at 2002/2014 while §8.9 rejects it at 2023.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGUWRCV.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 RECEIVE PIC X(4) VALUE "ABCD".
PROCEDURE DIVISION.
MAIN.
    DISPLAY RECEIVE.
    STOP RUN.
