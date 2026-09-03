      *> reject-at: 2002 2014 2023
      *> ISO 12.3.7 - the SPECIAL-NAMES CURSOR clause (item 25)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42CURS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURSOR IS WS-CUR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-CUR PIC 9(6).
       01 WS-X PIC X.
       PROCEDURE DIVISION.
           STOP RUN.
