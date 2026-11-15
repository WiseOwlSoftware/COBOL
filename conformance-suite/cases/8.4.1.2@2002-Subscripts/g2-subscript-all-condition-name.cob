      *>--- conformance-case
      *> id: g2-subscript-all-condition-name
      *> rules: [2002-SR-8.4.1.2.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.2.2 SR7 - "ALL shall not be specified if
      *> qualified-condition-name-1 is specified." E-ONE is a
      *> condition-name whose conditional variable E is a table element
      *> (OCCURS 3), so a subscript on it is otherwise permitted (SR2);
      *> E-ONE (ALL) writes the subscript ALL on a
      *> qualified-condition-name-1 and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E PIC 9 OCCURS 3 TIMES VALUE 1.
              88 E-ONE VALUE 1.
       PROCEDURE DIVISION.
           IF E-ONE (ALL) DISPLAY "X".
           STOP RUN.
