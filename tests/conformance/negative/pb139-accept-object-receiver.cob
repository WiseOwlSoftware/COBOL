      *> reject-at: 2023
      *> ISO 14.9.1.3 SR1: identifier-1 shall not reference a data item of class object (kb/Work PB139).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB139N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OREF USAGE OBJECT REFERENCE CBASE.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT OREF
           STOP RUN.
