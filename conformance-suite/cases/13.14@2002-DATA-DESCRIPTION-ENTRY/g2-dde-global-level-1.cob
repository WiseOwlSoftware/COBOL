      *>--- conformance-case
      *> id: g2-dde-global-level-1
      *> rules: [2002-SR-13.14.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR6 - "The GLOBAL clause may be specified only in
      *> data description entries whose level-number is 1."
      *> The granted form: a working-storage level 1 entry R IS GLOBAL
      *> (§2002:13.16.25.2 SR1 b lists working storage), read by name
      *> from a contained program that does not describe it
      *> (§2002:13.16.25.3 GR1: "All data-names subordinate to a global
      *> name are global names"; GR2: a contained program "may
      *> reference that name without describing it again").
      *> The refused forms are g2-dde-ws-global-77 (level 77) and
      *> g2-dde-cd-global-05 (level 5 in a record after a CD).
      *> DERIVATION. R1 holds "CD" from its VALUE; INNER displays it:
      *>   "R1=CD". A leg fails if the level 1 GLOBAL entry is refused or
      *>   if R1 is not visible in INNER (the compile fails).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE21.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R IS GLOBAL.
           05 R1 PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           CALL "G2DDE21I"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE21I.
       PROCEDURE DIVISION.
           DISPLAY "R1=" R1
           EXIT PROGRAM.
       END PROGRAM G2DDE21I.
       END PROGRAM G2DDE21.
