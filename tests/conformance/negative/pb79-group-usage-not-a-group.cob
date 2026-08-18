      *> reject-at: 2002 2014 2023
      *> ISO 13.18.29.3 SR1: "The GROUP-USAGE clause may be specified only if the subject of the entry is a group
      *> item" - an entry with a PICTURE clause is elementary. kb/Work PB79.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB79N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC 1(3) GROUP-USAGE BIT.
       PROCEDURE DIVISION.
           STOP RUN.
