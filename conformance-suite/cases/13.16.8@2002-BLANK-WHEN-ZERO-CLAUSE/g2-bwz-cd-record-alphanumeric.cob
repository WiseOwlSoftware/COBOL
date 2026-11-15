      *>--- conformance-case
      *> id: g2-bwz-cd-record-alphanumeric
      *> rules: [2002-SR-13.16.8.2-1]
      *> source-format: fixed
      *>---
      *> §2002:13.16.8.2 SR1 - BLANK WHEN ZERO "may be specified only
      *> for an elementary item described by its picture
      *> character-string as category numeric-edited or as numeric
      *> without the picture symbol 'S'".  U1 PIC X(3) in a record
      *> description entry following a CD entry (§2002:A.1.1.2) is
      *> category alphanumeric, so the program is refused at U1's
      *> entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BWZ03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 U1 PIC X(3) BLANK WHEN ZERO.
           05 E1 PIC ZZ9.99 BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       P-PARA.
           MOVE "A" TO U1.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
