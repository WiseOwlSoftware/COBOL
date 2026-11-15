      *>--- conformance-case
      *> id: g2-sign-cd-record-subject
      *> rules: [2002-SR-13.16.50.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.50.2 SR1 - "The SIGN clause may be specified only
      *> for: ... a numeric data or screen description entry whose picture
      *> character-string contains the symbol 'S' ..." In a record under a
      *> CD (§2002:A.1.1.2), U1 PIC 9(3) is numeric with no 'S' and X1
      *> PIC X(3) is not numeric, so the SIGN clause on each is refused
      *> (COBOL2422). The accepted subjects are g2-sign-cd-record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SGN04.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 U1 PIC 9(3) SIGN IS LEADING.
           05 X1 PIC X(3) SIGN IS TRAILING.
       PROCEDURE DIVISION.
           STOP RUN.
