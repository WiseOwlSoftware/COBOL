      *>--- conformance-case
      *> id: g2div06_cd_symbolic_dest_divisor
      *> rules: [2002-SR-14.8.11.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.11.2 SR1 - "Identifier-1 and identifier-2 shall
      *> reference an elementary data item of category numeric." SD1, an
      *> output CD SYMBOLIC DESTINATION, is "an elementary alphanumeric
      *> data item of 12 characters" (§2002:A.1.2.3 GR16d), so the
      *> divisor (identifier-1) SD1 is refused at the DIVIDE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIV06.
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
           DIVIDE SD1 INTO W.
           STOP RUN.
       END PROGRAM G2DIV06.
