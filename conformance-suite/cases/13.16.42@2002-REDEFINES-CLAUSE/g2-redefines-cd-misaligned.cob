      *>--- conformance-case
      *> id: g2-redefines-cd-misaligned
      *> rules: [2002-SR-13.16.42.2-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR15: "The description of the subject of the entry
      *> shall be such that its required alignment is the same as the alignment
      *> of the data item referenced by data-name-2." B (8 bits, USAGE BIT)
      *> starts at the fifth bit of the record, after the 4-bit A; C REDEFINES
      *> B is alphanumeric, whose character position starts on a byte, so its
      *> required alignment differs from B's and the entry is refused. C is not
      *> larger than B (8 bits each), so SR9 is not what refuses it. Fails if
      *> the alignment check skips the communication section. Accepted control
      *> (bit item over bit item at one alignment):
      *> g2-redefines-cd-record-storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF17.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC 1(4) USAGE BIT.
           05 B PIC 1(8) USAGE BIT.
           05 C REDEFINES B PIC X.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
