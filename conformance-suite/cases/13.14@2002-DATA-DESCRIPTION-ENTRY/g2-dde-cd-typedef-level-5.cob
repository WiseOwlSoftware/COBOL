      *>--- conformance-case
      *> id: g2-dde-cd-typedef-level-5
      *> rules: [2002-SR-13.14.2-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR14 - "The TYPEDEF clause may be specified only
      *> in a data description entry whose level-number is 1". Refusal
      *> beside g2-dde-typedef-level-1: a level 5 TYPEDEF in a record
      *> after a CD. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE09.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CI FOR INPUT
           SYMBOLIC QUEUE IS Q1.
       01  CREC.
           05 T IS TYPEDEF PIC X(3).
           05 F PIC X(84).
       PROCEDURE DIVISION.
           STOP RUN.
