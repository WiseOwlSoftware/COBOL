      *>--- conformance-case
      *> id: pb5848-sr1-copy-text-name-ordinary
      *> rules: [SR-8.3.2.1-1]
      *> files: [PB5848TX.cpy]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The control of the refusals of PB5848: PB5848TX is no reserved
      *> word, so COPY PB5848TX is legal and TXT, described by the
      *> copied entry, prints.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5848C2023.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY PB5848TX.
       PROCEDURE DIVISION.
       P0.
           DISPLAY TXT.
           STOP RUN.
