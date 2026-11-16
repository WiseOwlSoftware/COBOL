      *>--- conformance-case
      *> id: g2ini02_cd_renames_target
      *> rules: [2002-SR-14.8.19.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.19.2 SR4 - "The data description entry for the
      *> data item referenced by identifier-1 shall not contain a
      *> RENAMES clause." identifier-1 here is AB, a level-66 entry
      *> renaming A1 THRU B1 of an output CD's record (a 66 entry may
      *> follow a CD record, §2002:13.16.43.2 SR3). The 66 entry
      *> itself is legal; only INITIALIZE AB violates SR4, so the
      *> compiler must refuse the INITIALIZE at line 25. The control
      *> INITIALIZE CDREC (no RENAMES in its entry) is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INZ02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CDREC.
          05 A1 PIC X(3).
          05 B1 PIC 99.
       66 AB RENAMES A1 THRU B1.
       PROCEDURE DIVISION.
           INITIALIZE AB.
           INITIALIZE CDREC.
           STOP RUN.
