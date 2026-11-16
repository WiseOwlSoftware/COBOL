      *>--- conformance-case
      *> id: pb5848-copy-text-name-ordinary
      *> rules: [1985-SR-IV.4.2.2.1-2]
      *> files: [PB5848TX.cpy]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The control of the refusals of PB5848: PB5848TX is no reserved
      *> word, so COPY PB5848TX is legal and TXT, described by the
      *> copied entry, prints.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5848C85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY PB5848TX.
       PROCEDURE DIVISION.
       P0.
           DISPLAY TXT.
           STOP RUN.
