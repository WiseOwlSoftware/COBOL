      *>--- conformance-case
      *> id: g2002-cd-record-active-class
      *> rules: [2002-SR-13.16.58.2-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR15 - "The ACTIVE-CLASS phrase may be
      *> specified only in a factory definition, an instance definition,
      *> or the linkage or local-storage section of a method definition."
      *> The communication section is located in the data division of a
      *> source program (§2002:A.1.1), so a record description entry
      *> under a CD is never in any of those positions: the ACTIVE-CLASS
      *> phrase on COBJ below violates SR15 and the entry is refused.
      *> The permitted positions are written and run at 2002 by
      *> cases/13.18.60-USAGE/pb496_active_class_positions and
      *> cases/9.3.6-Method-invocation/pb389_object_reference_descriptor.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USG03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  COBJ USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
           SET COBJ TO NULL.
           STOP RUN.
