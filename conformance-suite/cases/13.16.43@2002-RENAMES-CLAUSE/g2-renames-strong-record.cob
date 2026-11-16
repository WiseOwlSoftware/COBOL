      *>--- conformance-case
      *> id: g2-renames-strong-record
      *> rules: [2002-SR-13.16.43.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR7 - "None of the items within the range,
      *> including data-name-2 and data-name-3, if specified, shall be ...
      *> an item subordinate to a strongly-typed group item". R2 is
      *> declared TYPE ST, and ST is a STRONG type declaration, so R2 is a
      *> strongly-typed group item and S1 and S2 are subordinate to it; the
      *> 66 entry is refused at its line. Fails if the entry is accepted.
      *> The program also violates §2002:13.16.55.2 SR3 ("the
      *> subject of the entry shall not be renamed in whole or in
      *> part"), so the refusal does not show that the SR7 strong arm
      *> is checked apart from the TYPE-clause rule; no legal 2002
      *> program separates the two.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN25.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ST TYPEDEF STRONG.
           05 S1 PIC X(4).
           05 S2 PIC X(4).
       01  R2 TYPE ST.
       66  X1 RENAMES S1 OF R2 THRU S2 OF R2.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
