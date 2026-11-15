      *>--- conformance-case
      *> id: g2mov14_zero_to_cd_alphabetic
      *> rules: [2002-SR-14.8.24.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR6 - "The figurative constant ZERO shall not
      *>   be moved to an alphabetic data item." IA (PIC A(5)) is an
      *>   alphabetic item of the record that implicitly redefines the
      *>   input CD area (§2002:A.1.2.2 SR5); identifier-2 may name a
      *>   communication-section item, and the rule holds there as in
      *>   any other section. MOVE ZERO TO IA is refused at the MOVE
      *>   line. Fails if the CD record item escapes the screen. The
      *>   communication facility draws the obsolete warning COBOL0903.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV14.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL.
       01 INREC.
          05 FILLER PIC X(12).
          05 IA     PIC A(5).
          05 FILLER PIC X(70).
       PROCEDURE DIVISION.
       MAIN.
           MOVE ZERO TO IA.
           STOP RUN.
