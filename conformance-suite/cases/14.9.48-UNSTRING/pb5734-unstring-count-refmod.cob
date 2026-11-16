      *>--- conformance-case
      *> id: pb5734-unstring-count-refmod
      *> rules: [SR-14.9.48.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.48.3 SR5: "Identifier-6 and identifier-8 shall
      *> reference integer data items". CN (2:1) is a reference
      *> modification of a numeric item, which has class and category
      *> alphanumeric (ISO 8.4.3.3.4 GR6; X3.23-1985 VI 6.27.3 SR4 with
      *> IV 4.3.8.4.4 GR5), so it is no integer data item: COBOL1651.
      *> The generated C# failed to compile before (kb/Work PB5734).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5734C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(20) VALUE "ABCD,CDE,F".
       01 R1 PIC X(5).
       01 CN PIC 99 VALUE 77.
       PROCEDURE DIVISION.
       MAIN-PARA.
           UNSTRING SRC DELIMITED BY "," INTO R1 COUNT IN CN (2:1).
           STOP RUN.
