*>--- conformance-case
*> id: pb3666_boolean_receiver_converts_characters_below_2002
*> rules: [GR-14.9.25.4-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3666, the negative of pb3666_boolean_receiver_converts_characters: boolean data, the PICTURE symbol 1
*> and USAGE BIT, is a COBOL-2002 introduction (ISO/IEC 1989:2002), so the boolean receiver of the MOVE is refused
*> at COBOL-85 by name (COBOL0900) and accepted from 2002 on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3666NEG85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SX PIC X(2) VALUE "AB".
       01 BD PIC 1(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SX TO BD
           DISPLAY BD
           STOP RUN.
