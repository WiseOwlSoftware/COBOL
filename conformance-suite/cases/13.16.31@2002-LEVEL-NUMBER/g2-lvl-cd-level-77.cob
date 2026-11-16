      *>--- conformance-case
      *> id: g2-lvl-cd-level-77
      *> rules: [2002-SR-13.16.31.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.31.2 SR2 - "Data description entries subordinate to
      *> a CD, FD, or SD entry shall have level-numbers with the values
      *> 66, 88, or 1 through 49." BAD is written at level 77 among the
      *> record description entries of an OUTPUT CD, so the entry is
      *> refused. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LVLN2.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT.
       01  R1 PIC X(23).
       77  BAD PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
