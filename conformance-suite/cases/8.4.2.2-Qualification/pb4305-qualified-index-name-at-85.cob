      *>--- conformance-case
      *> id: pb4305-qualified-index-name-at-85
      *> rules: [SR-8.4.2.2.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_qualified_index_name_2002. COBOL-85 did not allow an
      *>   index-name to be qualified (2002 Annex F.2 item 108).
      *>   Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NQX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABC".
           05 E PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX OF T TO 2
           STOP RUN.
