      *>--- conformance-case
      *> id: g2con02-cd-constant-length-cycle
      *> rules: [2002-SR-13.9.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR4 - "The length of data-name-1 or data-name-2 shall
      *>   not be dependent, directly or indirectly, upon the value of
      *>   constant-name-1." Both the constant entry and its LENGTH OF
      *>   operand are in the COMMUNICATION SECTION, under a CD: CR-A is
      *>   PIC X(KL) and KL is LENGTH OF CR-A, so CR-A's length depends on
      *>   KL. Expected: refused (COBOL1547). Legal twin:
      *>   g2con01_cd_constant_entries (KL, KR).
      *> Fails if: the dependency check does not reach CD records.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05  CR-A PIC X(KL).
       01  KL CONSTANT AS LENGTH OF CR-A.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
