      *>--- conformance-case
      *> id: g2-dde-cd-condition-name-qualified
      *> rules: [2002-SR-13.14.2-21]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR21 - "Each condition-name is subordinate to the
      *> data-name with which it is associated." Two condition-names
      *> spelled B1-ONE, one under B1 and one under B2, in a record
      *> after a CD. Being subordinate, each is made unique by naming
      *> its conditional variable as qualifier (§2002:8.4.1.1 rule 6).
      *> A condition-name not subordinate to its variable would refuse
      *> the qualified references or resolve one to the wrong variable,
      *> which the B2 NOT line and the SET line (B2 set to 1) show.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE10.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 B1 PIC 9.
              88 B1-ONE VALUE 1.
           05 B2 PIC 9.
              88 B1-ONE VALUE 1.
       PROCEDURE DIVISION.
           MOVE 1 TO B1
           MOVE 0 TO B2
           IF B1-ONE OF B1 DISPLAY "B1 ONE" ELSE DISPLAY "B1 NOT"
           END-IF
           IF B1-ONE IN B2 DISPLAY "B2 ONE" ELSE DISPLAY "B2 NOT"
           END-IF
           IF B1-ONE IN B1 IN CDREC DISPLAY "Q ONE"
           ELSE DISPLAY "Q NOT" END-IF
           SET B1-ONE OF B2 TO TRUE
           DISPLAY "CDREC=[" CDREC "]"
           STOP RUN.
