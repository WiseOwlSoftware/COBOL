      *> reject-at: 85 2002 2014 2023
      *> AN OCCURS KEY OPERAND IS WRITTEN WITHOUT SUBSCRIPTS (kb/Work PB885's sibling sweep).
      *> ISO/IEC 1989:2023 §13.18.38.3 SR2: "Data-name-1 and data-name-2 shall not be subscripted." and SR5:
      *> "Data-name-2 shall be specified without the subscripting normally required." The KEY capture kept
      *> the base word and dropped the written subscript in silence; it is now the ONE data-name-n screen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB885OKS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2.
          05 E2 OCCURS 3 ASCENDING KEY IS K2 (1).
             10 K2 PIC 9.
       PROCEDURE DIVISION.
           DISPLAY T2
           STOP RUN.
