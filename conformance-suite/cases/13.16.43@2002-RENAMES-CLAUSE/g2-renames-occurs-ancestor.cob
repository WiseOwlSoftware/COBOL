      *>--- conformance-case
      *> id: g2-renames-occurs-ancestor
      *> rules: [2002-SR-13.16.43.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR3 - "Neither data-name-2 nor data-name-3 may
      *> have an OCCURS clause in its data description entry nor be
      *> subordinate to an item that has an OCCURS clause in its data
      *> description entry." Data-name-2 E1 has no OCCURS clause of its own
      *> but is subordinate to T, which has OCCURS 3; the 66 entry is
      *> refused at its line. The OCCURS-operand leg is
      *> g2-renames-cd-occurs-operand. Fails if the entry is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WREC.
           05 T OCCURS 3.
              10 E1 PIC X(2).
           05 W2 PIC X(4).
       66  RX RENAMES E1 THRU W2.
       PROCEDURE DIVISION.
           DISPLAY W2
           STOP RUN.
