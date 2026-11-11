      *>--- conformance-case
      *> id: pb5090-cd-name-repeat-series
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5090 - 1985:XIV.2.2.3 SR6: the data-names of a CD entry "must be unique within the CD entry" (cite.py: OK). The
      *> series of data-names writes EK for the end key and again for the status key; the refusal stands at the second EK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5090B.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  C1 FOR INPUT Q S1 S2 S3 MD MT SS TL EK ST EK.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
