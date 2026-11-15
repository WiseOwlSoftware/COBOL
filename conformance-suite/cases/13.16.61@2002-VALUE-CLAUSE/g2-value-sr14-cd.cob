      *>--- conformance-case
      *> id: g2-value-sr14-cd
      *> rules: [2002-SR-13.16.61.2-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR14 - "A data description entry that contains
      *> the VALUE clause shall contain an OCCURS clause or be
      *> subordinate to a data description entry that contains an OCCURS
      *> clause." A1 carries a format 2 (table) VALUE, "A" FROM (1), in a
      *> record description entry under an output CD (§2002:A.1.1.2),
      *> but neither A1 nor the group A above it has an OCCURS clause, so
      *> the entry is refused (COBOL1944). Fails if the compiler accepts
      *> it in the communication section. The accepted format 2 form in
      *> a CD record is g2-value-cd-record-table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V14A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  A.
           05 A1 PIC X VALUE "A" FROM (1).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
