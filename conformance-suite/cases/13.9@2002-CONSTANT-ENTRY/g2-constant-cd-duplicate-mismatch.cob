      *>--- conformance-case
      *> id: g2-constant-cd-duplicate-mismatch
      *> rules: [2002-SR-13.9.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR9 - "If constant-name-1 duplicates another
      *> constant-name, the specification of arithmetic-expression-1,
      *> literal-1, data-name-1, data-name-2, or
      *> compilation-variable-name-1 shall be the same as specified in
      *> the other constant-name". KD is AS 5 in working-storage and
      *> AS 6 in a constant entry under a CD (§2002:13.1.1): the
      *> specifications differ, so the program is refused at the second
      *> KD entry (it fails if the CD entry is not compared with the
      *> working-storage one and the program compiles).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KD CONSTANT AS 5.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KD CONSTANT AS 6.
       01  CDREC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
