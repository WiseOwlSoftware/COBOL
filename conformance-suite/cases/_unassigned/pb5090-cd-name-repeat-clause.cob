      *>--- conformance-case
      *> id: pb5090-cd-name-repeat-clause
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5090 - 1985:XIV.2.2.3 SR6: "Data-name-1, ..., data-name-11 must be unique within the CD entry." (cite.py: OK).
      *> END KEY IS EK and STATUS KEY IS EK give one name to two fields. The refusal stands at the clause that REPEATS the name, the
      *> STATUS KEY clause, not at the CD entry's first line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5090A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS CQ
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS EK.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
