      *>--- conformance-case
      *> id: g2-renames-strong-subgroup
      *> rules: [2002-SR-13.16.43.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR7 - "None of the items within the range,
      *> including data-name-2 and data-name-3, if specified, shall be ...
      *> a strongly-typed group item". Data-name-2 SG OF R is itself a
      *> strongly-typed group item (TYPE ST, ST a STRONG type declaration).
      *> SG may sit below level 1 only inside a STRONG type declaration
      *> (§2002:13.16.55.2 SR6), here ST2, so the range is also subordinate
      *> to the strongly-typed record R: at 2002 the two arms always occur
      *> together. The 66 entry is refused at its line. The level-1 record
      *> arm alone is g2-renames-strong-record. Fails if the entry is
      *> accepted.
      *> The program also violates §2002:13.16.55.2 SR3 ("the
      *> subject of the entry shall not be renamed in whole or in
      *> part"), so the refusal does not show that the SR7 strong arm
      *> is checked apart from the TYPE-clause rule; no legal 2002
      *> program separates the two.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN26.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ST TYPEDEF STRONG.
           05 S1 PIC X(4).
           05 S2 PIC X(4).
       01  ST2 TYPEDEF STRONG.
           05 W1 PIC X(2).
           05 SG TYPE ST.
           05 W2 PIC X(2).
       01  R TYPE ST2.
       66  X1 RENAMES SG OF R THRU W2 OF R.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
