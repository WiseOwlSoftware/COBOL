      *>--- conformance-case
      *> id: g2-dde-ws-sync-group
      *> rules: [2002-SR-13.14.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR11 - "The SYNCHRONIZED, PICTURE, JUSTIFIED,
      *> and BLANK WHEN ZERO clauses may be specified only for an
      *> elementary data item." G1 is a group with SYNCHRONIZED in
      *> working-storage (2002 names SYNCHRONIZED here; later editions
      *> do not). Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP08.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WREC.
           05 G1 SYNCHRONIZED.
              10 B1 PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
