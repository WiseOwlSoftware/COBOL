*> reject-at: 2002 2014 2023
*> ISO 1989:2023 13.18.60 GR25 - a RESTRICTED program-pointer (TO program-prototype-name) is recognized
*> but staged LOUD: signature matching needs the program-prototype registry (P13).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPP05.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 PP USAGE PROGRAM-POINTER TO SOMEPROTO.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
