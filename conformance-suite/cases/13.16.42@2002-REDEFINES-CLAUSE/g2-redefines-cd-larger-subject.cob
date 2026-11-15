      *>--- conformance-case
      *> id: g2-redefines-cd-larger-subject
      *> rules: [2002-SR-13.16.42.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR9: "The storage area required for the subject of
      *> the entry shall not be larger than the storage area required for the
      *> data item referenced by data-name-2, unless the data item referenced by
      *> data-name-2 has been specified with level number 1 and without the
      *> EXTERNAL clause." In a record under a CD a REDEFINES can stand only
      *> below level 1 (§2002:13.16.42.2 SR4), so the exemption never applies:
      *> C2 (4 positions) REDEFINES C1 (2 positions) at level 5 is refused.
      *> Fails if the size check is skipped for the communication section.
      *> Accepted control: g2-redefines-cd-record-storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF09.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1 PIC X(2).
           05 C2 REDEFINES C1 PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
