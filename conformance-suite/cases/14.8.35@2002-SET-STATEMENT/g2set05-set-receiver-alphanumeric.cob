      *>--- conformance-case
      *> id: g2set05-set-receiver-alphanumeric
      *> rules: [2002-SR-14.8.35.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.35.2 SR1 (format 1) - "Identifier-1 shall
      *>   reference a data item of class index or an integer data
      *>   item."
      *> W-FR PIC X(3) is class alphanumeric: neither an integer
      *> data item nor class index, so the SET must be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SET05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB.
           05  TE PIC X OCCURS 5 INDEXED BY IX.
       01  TB2.
           05  TE2 PIC X OCCURS 5 INDEXED BY IX2.
       01  W-FR  PIC X(3) VALUE SPACES.
       PROCEDURE DIVISION.
       P-MAIN.
           SET IX TO 3
           SET W-FR TO IX
           STOP RUN.
