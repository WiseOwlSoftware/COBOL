      *>--- conformance-case
      *> id: pb5607-unstring-pointer-too-small
      *> rules: [SR-14.9.48.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.48.3 SR6 (X3.23-1985 VI 6.27.3 SR5 is the same rule):
      *> "Identifier-7 shall be described as an elementary numeric
      *> integer data item of sufficient size to contain a value equal to
      *> 1 plus the size of the data item referenced by identifier-1."
      *> SRC is 12 characters long, so identifier-7 has to hold 13; P1
      *> is PIC 9 and holds at most 9. Refused at bind (COBOL1651), the
      *> sibling of the STRING pointer size rule (kb/Work PB5607).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5607C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(12) VALUE "ABCDEFGHIJKL".
       01 R PIC X(10).
       01 P1 PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           UNSTRING SRC INTO R WITH POINTER P1.
           DISPLAY "[" R "] P1=" P1.
           STOP RUN.
