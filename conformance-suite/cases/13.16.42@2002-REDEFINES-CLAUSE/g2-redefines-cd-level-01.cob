      *>--- conformance-case
      *> id: g2-redefines-cd-level-01
      *> rules: [2002-SR-13.16.42.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR4 - "This clause shall not be specified in
      *> level 1 entries in the communication section." CREC-B is a level 1
      *> record description entry under the input CD CDIN and carries
      *> REDEFINES CREC-A, so the entry is refused at its line. Every other
      *> rule of 13.16.42.2 holds (same level 01, same size, no VALUE, no
      *> OCCURS), so only SR4 can reject it. The accepted controls are
      *> g2-redefines-cd-record-storage (REDEFINES below level 1 in a CD
      *> record, legal) and the second 01 entry without REDEFINES, which
      *> implicitly redefines the CD area. Fails if the level-1 REDEFINES is
      *> accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF30.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           MESSAGE COUNT IS MC.
       01  CREC-A.
           05 CA1 PIC X(10).
           05 FILLER PIC X(77).
       01  CREC-B REDEFINES CREC-A.
           05 CB1 PIC 9(10).
           05 FILLER PIC X(77).
       PROCEDURE DIVISION.
           MOVE "1234567890" TO CA1
           DISPLAY CB1
           STOP RUN.
