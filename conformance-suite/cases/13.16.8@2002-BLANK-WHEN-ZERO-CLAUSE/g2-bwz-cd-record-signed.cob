      *>--- conformance-case
      *> id: g2-bwz-cd-record-signed
      *> rules: [2002-SR-13.16.8.2-1]
      *> source-format: fixed
      *>---
      *> §2002:13.16.8.2 SR1 - BLANK WHEN ZERO "may be specified only
      *> for an elementary item described by its picture
      *> character-string as category numeric-edited or as numeric
      *> without the picture symbol 'S'".  U1 PIC S999 in a record
      *> description entry following a CD entry (§2002:A.1.1.2) has the
      *> symbol 'S', so the program is refused at U1's entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BWZ02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 U1 PIC S999 BLANK WHEN ZERO.
           05 E1 PIC ZZ9.99 BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       P-PARA.
           MOVE 0 TO U1 E1.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
