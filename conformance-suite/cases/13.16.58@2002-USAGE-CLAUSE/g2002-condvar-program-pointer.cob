      *>--- conformance-case
      *> id: g2002-condvar-program-pointer
      *> rules: [2002-SR-13.16.58.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR10 - "An elementary data item of class index,
      *> object, or pointer shall not be a conditional variable."
      *> A program-pointer is of class pointer, so the level-88 entry
      *> under PP makes it a conditional variable: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
           88 PP-X VALUE 1.
       PROCEDURE DIVISION.
           STOP RUN.
