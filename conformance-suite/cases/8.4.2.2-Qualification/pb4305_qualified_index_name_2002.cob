      *>--- conformance-case
      *> id: pb4305_qualified_index_name_2002
      *> rules: [SR-8.4.2.2.3-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 8.4.2.2.3 SR6: "The qualification of an
      *>   index-name may include the name of the table with which the
      *>   index-name is associated" (2002 Annex F.2 item 108). IX OF T
      *>   is set to 2, so E (IX) is the second of "ABC". Expected:
      *>   "B". It fails if the qualified index-name is refused or
      *>   resolves elsewhere.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305QX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABC".
           05 E PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX OF T TO 2
           DISPLAY E (IX)
           STOP RUN.
