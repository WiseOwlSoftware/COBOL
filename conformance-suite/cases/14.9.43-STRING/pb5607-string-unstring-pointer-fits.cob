      *>--- conformance-case
      *> id: pb5607-string-unstring-pointer-fits
      *> rules: [SR-14.9.43.3-7, SR-14.9.48.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.43.3 SR7 and 14.9.48.3 SR6: identifier-4 / identifier-7
      *> shall be "of sufficient size to contain a value equal to 1 plus
      *> the size of the data item referenced". The BOUNDARY, accepted:
      *> R and SRC are 8 characters long, so the pointer has to hold 9,
      *> and PIC 9 does (kb/Work PB5607). GR: STRING moves ABC to
      *> positions 1-3 and leaves the pointer 4 (14.9.43.4 GR); UNSTRING
      *> examines AB, (3 characters) and leaves the pointer 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5607D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(8) VALUE SPACES.
       01 P PIC 9 VALUE 1.
       01 SRC PIC X(8) VALUE "AB,CD   ".
       01 R1 PIC X(3).
       01 Q PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING "ABC" DELIMITED BY SIZE INTO R WITH POINTER P.
           DISPLAY "[" R "] P=" P.
           UNSTRING SRC DELIMITED BY "," INTO R1 WITH POINTER Q.
           DISPLAY "[" R1 "] Q=" Q.
           STOP RUN.
