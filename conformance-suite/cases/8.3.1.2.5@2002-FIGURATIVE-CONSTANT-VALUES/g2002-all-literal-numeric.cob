      *>--- conformance-case
      *> id: g2002-all-literal-numeric
      *> rules: [2002-SR-8.3.1.2.5.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.5.2 SR2 - "Literal-1 shall be an alphanumeric,
      *> boolean, or national literal": the numeric literal 5 after ALL
      *> violates SR2 and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FIG02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 X1 PIC X(4).
       PROCEDURE DIVISION.
           MOVE ALL 5 TO X1
           STOP RUN.
