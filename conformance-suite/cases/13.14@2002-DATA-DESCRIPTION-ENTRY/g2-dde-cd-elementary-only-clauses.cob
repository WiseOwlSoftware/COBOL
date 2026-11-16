      *>--- conformance-case
      *> id: g2-dde-cd-elementary-only-clauses
      *> rules: [2002-SR-13.14.2-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR11 - "The SYNCHRONIZED, PICTURE, JUSTIFIED,
      *> and BLANK WHEN ZERO clauses may be specified only for an
      *> elementary data item." All four on elementary items of a
      *> record after a CD; refusing any fails the case. B1 JUSTIFIED
      *> RIGHT receives "AB": right-aligned, space fill on the left
      *> (§2002:13.16.30.3 GR2), [ AB]. B2 BLANK WHEN ZERO receives
      *> zero: all spaces (§2002:13.16.8.3 GR1), then 5: 005. B3
      *> SYNCHRONIZED holds 7, moved to a PIC 9(4) item: 0007.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP07.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W4 PIC 9(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G1.
              10 B1 PIC X(4) JUSTIFIED RIGHT.
              10 B2 PIC 9(3) BLANK WHEN ZERO.
              10 B3 PIC S9(4) COMP SYNCHRONIZED.
       PROCEDURE DIVISION.
           MOVE "AB" TO B1
           MOVE 0 TO B2
           DISPLAY "B1=[" B1 "] B2=[" B2 "]"
           MOVE 5 TO B2
           MOVE 7 TO B3
           MOVE B3 TO W4
           DISPLAY "B2=[" B2 "] B3=" W4
           STOP RUN.
