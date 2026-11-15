      *>--- conformance-case
      *> id: g2can03_numeric_identifier_refused
      *> rules: [2002-SR-14.8.5.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.5.2 SR1 - "Identifier-1 shall be defined as an
      *> alphanumeric or national data item". WN is a numeric
      *> working-storage item and MC is the MESSAGE COUNT item of an
      *> input CD, which §2002:A.1.2.3 GR2k describes as "an integer
      *> of 6 digits, without an operational sign": both are
      *> numeric, so each CANCEL is refused at its line. A compiler
      *> that resolved the CANCEL target by the item's content without
      *> a class screen (the silent no-op) accepts this program.
      *> COBOL0903 (the obsolete communication facility) is a warning.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNX3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WN PIC 9(7) VALUE 1.
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS QN
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       MAIN.
           CANCEL WN
           CANCEL MC
           STOP RUN.
