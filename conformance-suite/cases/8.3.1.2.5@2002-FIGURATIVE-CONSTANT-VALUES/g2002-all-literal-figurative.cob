      *>--- conformance-case
      *> id: g2002-all-literal-figurative
      *> rules: [2002-SR-8.3.1.2.5.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.5.2 SR2 - "The literal shall not be a figurative
      *> constant": ALL "A" is itself a figurative constant (format 6),
      *> so
      *> ALL ALL "A" writes a figurative constant as literal-1 and is
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FIG03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 X1 PIC X(4).
       PROCEDURE DIVISION.
           MOVE ALL ALL "A" TO X1
           STOP RUN.
