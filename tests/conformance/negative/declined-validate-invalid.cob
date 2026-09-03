*> reject-at: 2002 2014 2023
*> ISO 13.18.31 INVALID clause - Annex A.4.14 item 4. 13.16.2 prints it inside a repetition group
*> ({ INVALID WHEN condition-2 } ...), so an entry may carry several; each is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLINV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 WS-A PIC 9(4) INVALID WHEN WS-A = 0.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
