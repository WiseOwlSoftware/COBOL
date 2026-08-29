      *> reject-at: 2023
      *> ISO 14.9.4.3 SR1: identifier-1 shall be defined as an alphanumeric, national, or program-pointer
      *> data item. A numeric target fell through to a garbage name-string read at RUN time (kb/Work PB132).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N9 PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           CALL N9
           STOP RUN.
