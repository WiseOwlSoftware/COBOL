      *>--- conformance-case
      *> id: g2-qual-cd-name-ambiguous
      *> rules: [2002-SR-8.4.1.1.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.1.2 SR1 - "For each nonunique user-defined name
      *> that is explicitly referenced, uniqueness shall be established
      *> through a sequence of qualifiers that precludes any ambiguity of
      *> reference." FLD is defined under CDA and under CDB; the MOVE
      *> names it with no qualifier, so it is ambiguous and refused at
      *> the MOVE. The accepted half, where IN CDA / OF CDB make it
      *> unique, is g2-qual-cd-name-two-cds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2QUAL04.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDA FOR OUTPUT
           DESTINATION COUNT IS DCA
           TEXT LENGTH IS TLA
           STATUS KEY IS SKA
           ERROR KEY IS EKA
           SYMBOLIC DESTINATION IS SDA.
       01  RECA.
           05 FLD PIC X(3).
              88 FLD-YES VALUE "YES".
       CD  CDB FOR OUTPUT
           DESTINATION COUNT IS DCB
           TEXT LENGTH IS TLB
           STATUS KEY IS SKB
           ERROR KEY IS EKB
           SYMBOLIC DESTINATION IS SDB.
       01  RECB.
           05 FLD PIC X(3).
              88 FLD-YES VALUE "YES".
       PROCEDURE DIVISION.
           MOVE "YES" TO FLD.
           STOP RUN.
