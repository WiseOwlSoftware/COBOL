      *>--- conformance-case
      *> id: pb5607-string-pointer-too-small-2002
      *> rules: [2002-SR-14.8.39.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 2002 14.8.39.2 SR6: "Identifier-4 shall be described as an
      *> elementary numeric integer data item of sufficient size to
      *> contain a value equal to 1 plus the size of the data item
      *> referenced by identifier-3."
      *> R is 10 characters long, so identifier-4 has to hold 11; P1 is
      *> PIC 9 and holds at most 9. Refused at bind (COBOL1651); it was
      *> accepted and the pointer truncated on store (kb/Work PB5607).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5607B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X(12) VALUE "ABCDEFGHIJKL".
       01 R PIC X(10).
       01 P1 PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING S1 DELIMITED BY SIZE INTO R WITH POINTER P1
             ON OVERFLOW DISPLAY "OVF"
             NOT ON OVERFLOW DISPLAY "NO-OVF" END-STRING.
           DISPLAY "[" R "] P1=" P1.
           STOP RUN.
