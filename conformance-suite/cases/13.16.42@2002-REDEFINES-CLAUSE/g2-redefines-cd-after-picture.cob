      *>--- conformance-case
      *> id: g2-redefines-cd-after-picture
      *> rules: [2002-SR-13.16.42.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR1: "The REDEFINES clause shall immediately follow
      *> the entry-name clause". In a record under an output CD (REDEFINES is
      *> legal below level 1 there, §2002:13.16.42.2 SR4) C2 writes PICTURE
      *> between its name and REDEFINES, and is refused. Fails if clause
      *> placement is screened only outside the communication section.
      *> Accepted control: g2-redefines-cd-entry-rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF21.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1 PIC X(4).
           05 C2 PIC X(4) REDEFINES C1.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
