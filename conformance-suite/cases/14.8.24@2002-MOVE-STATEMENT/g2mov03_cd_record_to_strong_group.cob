      *>--- conformance-case
      *> id: g2mov03_cd_record_to_strong_group
      *> rules: [2002-SR-14.8.24.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR2 - "If identifier-2 references a strongly-
      *>   typed group item, identifier-1 shall be specified and be
      *>   described as a group item of the same type." SG is of the
      *>   strong type TS; OUTREC, a record under an output CD
      *>   (§2002:A.1.2.2 SR9), has the same layout but is not of type
      *>   TS. The MOVE is refused at its line (COBOL1533). Fails if
      *>   a same-layout CD record is taken as the same type.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TS TYPEDEF STRONG.
          05 TA PIC X(3).
          05 TB PIC 9(2).
       01 SG TYPE TS.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OUTREC.
          05 CA PIC X(3).
          05 CB PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE OUTREC TO SG.
           STOP RUN.
