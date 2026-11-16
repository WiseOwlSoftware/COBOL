      *>--- conformance-case
      *> id: pb5734-string-pointer-refmod
      *> rules: [SR-14.9.43.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.43.3 SR7: "Identifier-4 shall be described as an
      *> elementary numeric integer data item". PT (2:1) is no integer
      *> data item: the unique data item a reference modification of a
      *> numeric item creates has class and category alphanumeric
      *> (ISO 8.4.3.3.4 GR6; X3.23-1985 IV 4.3.8.4.4 GR5). The binder
      *> read the base item's PICTURE through the reference
      *> modification and the generated C# failed to compile (CS1503);
      *> it is refused with COBOL1651 (kb/Work PB5734).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5734A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DST PIC X(10).
       01 PT PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING "AB" DELIMITED BY SIZE INTO DST WITH POINTER PT (2:1).
           STOP RUN.
