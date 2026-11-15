      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_4-7
      *> rules: [1985-GR-VI.5.9.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.4 GR7 (THE PICTURE CLAUSE): "An unsigned nonzero
      *> integer which is enclosed in parentheses following the symbols
      *> 'A', ',', 'X', '9', 'P', 'Z', '*', 'B', '/', '0', '+', '-', or
      *> the currency symbol indicates the number of consecutive
      *> occurrences of the symbol" (cite.py: OK; kb/Work PB5118). Each
      *> item repeats a listed symbol, and each edits as its expansion:
      *> $$$9 12 -> " $12"; +++9 -5 -> "  -5"; 99BB9 123 -> "12  3";
      *> ZZ9 7 -> "  7"; **9 7 -> "**7"; 99/9 123 -> "12/3";
      *> 9900 12 -> "1200"; 99,999 12345 -> "12,345"; XXX "ABC".
      *> The refusals of V(1), S(1) and CR(1) are
      *> 13.18.40-PICTURE/pb5118-repeat-after-{v,s,cr}-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICREP7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1 PIC $(3)9.
       01  E2 PIC +(3)9.
       01  E3 PIC 9(2)B(2)9.
       01  E4 PIC Z(2)9.
       01  E5 PIC *(2)9.
       01  E6 PIC 9(2)/(1)9.
       01  E7 PIC 9(2)0(2).
       01  E8 PIC 9(2),(1)9(3).
       01  E9 PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12 TO E1.
           MOVE -5 TO E2.
           MOVE 123 TO E3.
           MOVE 7 TO E4.
           MOVE 7 TO E5.
           MOVE 123 TO E6.
           MOVE 12 TO E7.
           MOVE 12345 TO E8.
           MOVE "ABC" TO E9.
           DISPLAY "[" E1 "]".
           DISPLAY "[" E2 "]".
           DISPLAY "[" E3 "]".
           DISPLAY "[" E4 "]".
           DISPLAY "[" E5 "]".
           DISPLAY "[" E6 "]".
           DISPLAY "[" E7 "]".
           DISPLAY "[" E8 "]".
           DISPLAY "[" E9 "]".
           STOP RUN.
