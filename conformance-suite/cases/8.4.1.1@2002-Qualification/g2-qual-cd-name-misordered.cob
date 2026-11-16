      *>--- conformance-case
      *> id: g2-qual-cd-name-misordered
      *> rules: [2002-SR-8.4.1.1.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.1.2 SR4 - "Qualifiers shall be specified in the
      *> order of successively more inclusive levels in the hierarchy.
      *> For a condition-name, the hierarchy is that of the associated
      *> conditional variable." FLD-YES OF CDA OF RECA names the cd-name
      *> before the record it contains, so no hierarchy matches it and
      *> the IF is refused. The accepted order is in
      *> g2-qual-cd-name-two-cds (FLD-YES IN FLD IN RECB IN CDB).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2QUAL05.
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
           IF FLD-YES OF CDA OF RECA
               DISPLAY "X"
           END-IF.
           STOP RUN.
