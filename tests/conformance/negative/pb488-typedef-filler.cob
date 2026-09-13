*> reject-at: 2002 2014 2023
*> ISO 1989:2023 13.16.3 syntax rule 15's SECOND half - "and for which the data-name format of the
*> entry-name clause is specified". A FILLER entry-name is the other format of the entry-name clause
*> (13.18.20), so a FILLER TYPEDEF declares a type nothing can name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-TYPEDEF-FILLER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER IS TYPEDEF PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "X"
           STOP RUN.
