      *>--- conformance-case
      *> id: g2div08_cd_giving_status_key
      *> rules: [2002-SR-14.8.11.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.11.2 SR2 - "Identifier-3 and identifier-4 shall
      *> reference an elementary data item of category numeric or
      *> numeric-edited." SK, an output CD STATUS KEY, is alphanumeric
      *> (§2002:A.1.2.3 GR16c), so GIVING SK is refused at the DIVIDE
      *> line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIV08.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q  PIC 9(4) VALUE 0.
       01 W  PIC 9(4) VALUE 8.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       CD CDIN FOR INPUT.
       01 INREC.
          05 IQ     PIC X(12).
          05 FILLER PIC X(75).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 2 INTO W GIVING SK.
           STOP RUN.
       END PROGRAM G2DIV08.
