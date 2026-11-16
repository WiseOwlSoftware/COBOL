      *>--- conformance-case
      *> id: g2-dde-cd-condition-name-associations
      *> rules: [2002-SR-13.14.2-22]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR22 - "A condition-name may be associated with
      *> any data description entry that contains a level-number
      *> except" a)-g). In a record after a CD, condition-names follow:
      *> an alphanumeric group of display items (G1, not c), its member
      *> A1, an elementary COMP item (C1: c names groups only), and a
      *> JUSTIFIED and a SYNCHRONIZED elementary item (J1, S1: d names
      *> groups only). Refusing any fails the case. Each condition is
      *> true only when its variable holds the value (§2002:8.8.4.1.4.2
      *> GR3); C1 is tested at 7 and 8.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP17.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G1.
              88 G1-AB12 VALUE "AB12".
              10 A1 PIC XX.
                 88 A1-AB VALUE "AB".
              10 N1 PIC 99.
           05 C1 PIC 9(4) COMP.
              88 C1-SEVEN VALUE 7.
           05 G3.
              10 J1 PIC X(3) JUSTIFIED RIGHT.
                 88 J1-X VALUE "  X".
              10 S1 PIC S9(4) COMP SYNCHRONIZED.
                 88 S1-ONE VALUE 1.
       PROCEDURE DIVISION.
           MOVE "AB" TO A1
           MOVE 12 TO N1
           IF G1-AB12 DISPLAY "G1-AB12 Y" ELSE DISPLAY "G1-AB12 N"
           END-IF
           IF A1-AB DISPLAY "A1-AB Y" ELSE DISPLAY "A1-AB N" END-IF
           MOVE 7 TO C1
           IF C1-SEVEN DISPLAY "C1-SEVEN Y" ELSE DISPLAY "C1-SEVEN N"
           END-IF
           MOVE 8 TO C1
           IF C1-SEVEN DISPLAY "C1-SEVEN Y" ELSE DISPLAY "C1-SEVEN N"
           END-IF
           MOVE "X" TO J1
           IF J1-X DISPLAY "J1-X Y" ELSE DISPLAY "J1-X N" END-IF
           MOVE 1 TO S1
           IF S1-ONE DISPLAY "S1-ONE Y" ELSE DISPLAY "S1-ONE N" END-IF
           STOP RUN.
