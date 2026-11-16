      *>--- conformance-case
      *> id: g2-dde-cd-usage-no-picture
      *> rules: [2002-SR-13.14.2-8, 2002-SR-13.14.2-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR8 - "The PICTURE clause shall not be specified
      *> for ... an item whose usage is binary-char, binary-short,
      *> binary-long, ..., index, ..., pointer"; every other elementary
      *> entry carries one. §2002:13.14.2 SR10 - "The VALUE clause
      *> shall not be specified for data items of class index, object,
      *> or pointer", so A (alphanumeric) may carry one. A record
      *> description entry after a CD holds A with PICTURE and VALUE,
      *> and M, S, IX with no PICTURE and no VALUE; a second record PX
      *> (level 1, as §2002:13.16.58.2 SR13 asks of a pointer) has
      *> neither. Refusing any entry fails the case. A=[ABCD] pins A's
      *> VALUE taking effect in the communication section
      *> (§2002:13.16.61.3 GR4); M=07 S=12 pins M and S as integer
      *> binary items holding 7 and 12; PX=NULL pins PX as a pointer
      *> set to NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W2 PIC 99.
       01  W3 PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC X(4) VALUE "ABCD".
           05 M BINARY-LONG.
           05 S BINARY-SHORT.
           05 IX USAGE INDEX.
       01  PX USAGE POINTER.
       PROCEDURE DIVISION.
           DISPLAY "A=[" A "]"
           MOVE 7 TO M
           MOVE 12 TO S
           MOVE M TO W2
           MOVE S TO W3
           DISPLAY "M=" W2 " S=" W3
           SET PX TO NULL
           IF PX = NULL DISPLAY "PX=NULL"
           ELSE DISPLAY "PX NOT NULL" END-IF
           STOP RUN.
