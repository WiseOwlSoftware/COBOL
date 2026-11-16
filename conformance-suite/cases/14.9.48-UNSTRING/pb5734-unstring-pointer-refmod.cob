      *>--- conformance-case
      *> id: pb5734-unstring-pointer-refmod
      *> rules: [SR-14.9.48.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.48.3 SR6: "Identifier-7 shall be described as an
      *> elementary numeric integer data item". PT (2:1) is a reference
      *> modification of a numeric item, which has class and category
      *> alphanumeric (ISO 8.4.3.3.4 GR6; X3.23-1985 IV 4.3.8.4.4 GR5),
      *> so it is no integer data item: COBOL1651 (kb/Work PB5734).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5734B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(20) VALUE "ABCD,CDE,F".
       01 R1 PIC X(5).
       01 PT PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           UNSTRING SRC DELIMITED BY "," INTO R1 WITH POINTER PT (2:1).
           STOP RUN.
