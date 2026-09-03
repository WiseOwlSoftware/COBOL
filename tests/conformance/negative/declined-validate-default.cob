*> reject-at: 2002 2014 2023
*> ISO 13.18.17 DEFAULT clause - Annex A.4.14 item 2, the DECLINED VALIDATE facility. A.4.1 admits an
*> optional element's syntax only when support is claimed, so the clause is refused BY NAME (COBOLNET1708)
*> rather than drawing the bare "no viable alternative at input 'DEFAULT'" it drew before.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLDEF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 WS-A PIC X(4) DEFAULT IS "AB".
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
