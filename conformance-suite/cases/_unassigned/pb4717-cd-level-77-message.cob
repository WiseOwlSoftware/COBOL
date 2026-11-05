      *>--- conformance-case
      *> id: pb4717-cd-level-77-message
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4717: the refusal names a CD entry and cites 2002 13.16.31 SR2, not FD or SD - ISO/IEC 1989:2002 13.16.31 SR2: 66, 88 or 1 through 49.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4717CD.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT STATUS KEY IS S1.
       77 I1 PIC X(87).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
