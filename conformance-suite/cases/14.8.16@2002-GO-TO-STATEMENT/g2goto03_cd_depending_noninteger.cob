      *>--- conformance-case
      *> id: g2goto03_cd_depending_noninteger
      *> rules: [2002-SR-14.8.16.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.16.2 SR1 - "Identifier-1 shall reference a numeric
      *> elementary data item that is an integer."
      *> RX PIC 9V9 in the input CD record is numeric but not an
      *> integer, so DEPENDING ON RX is refused at the GO TO line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOT03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       CD CDIN FOR INPUT.
       01 INREC.
          05 RN     PIC 9.
          05 RX     PIC 9V9.
          05 FILLER PIC X(84).
       PROCEDURE DIVISION.
       MAIN.
           GO TO P1 P2 DEPENDING ON RX.
       P1. STOP RUN.
       P2. STOP RUN.
       END PROGRAM G2GOT03.
