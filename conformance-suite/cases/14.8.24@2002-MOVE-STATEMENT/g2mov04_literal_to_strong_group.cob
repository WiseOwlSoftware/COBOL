      *>--- conformance-case
      *> id: g2mov04_literal_to_strong_group
      *> rules: [2002-SR-14.8.24.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR2 - "If identifier-2 references a strongly-
      *>   typed group item, identifier-1 shall be specified and be
      *>   described as a group item of the same type." Here literal-1
      *>   is specified, not identifier-1, and the receiver SG is of
      *>   the strong type TS, so the MOVE is refused at its line.
      *>   Fails if a literal of the group's length slips past.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TS TYPEDEF STRONG.
          05 TA PIC X(3).
          05 TB PIC 9(2).
       01 SG TYPE TS.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC12" TO SG.
           STOP RUN.
