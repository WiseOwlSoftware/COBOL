      *>--- conformance-case
      *> id: g2002-cd-pointer-in-group
      *> rules: [2002-SR-13.16.58.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR13 - the OBJECT REFERENCE, POINTER or
      *> PROGRAM-POINTER phrase "may be specified only for an elementary
      *> data item with level 1 or subordinate to a type declaration that
      *> includes the STRONG phrase".
      *> The record description entries after an output CD are ordinary
      *> data description entries that implicitly redefine its record
      *> area (§2002:A.1.2.2 SR9), so the USAGE syntax rules reach them.
      *> CP is at level 05 under the ordinary group CR, which is no
      *> type declaration: refused. The STRONG branch cannot be written
      *> under a CD: a CD record implicitly redefines the record area
      *> (A.1.2.2 SR9) and an item of a STRONG type shall not be
      *> implicitly redefined (§2002:13.16.55.2 SR4). The level-1 form
      *> is accepted by g2002_cd_record_pointer_level_1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR13.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 CN PIC X(4).
           05 CP USAGE POINTER.
       PROCEDURE DIVISION.
           STOP RUN.
