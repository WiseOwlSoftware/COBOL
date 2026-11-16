      *>--- conformance-case
      *> id: g2-dde-same-as-co-clauses
      *> rules: [2002-SR-13.14.2-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR12 - "The SAME AS clause shall not be
      *> specified in the same data description entry with any clauses
      *> except entry-name, EXTERNAL, GLOBAL, level-number, and
      *> OCCURS." Each permitted co-clause is written: GLOBAL and
      *> EXTERNAL on level 1 entries in working-storage, OCCURS on an
      *> entry of a record after a CD, and a bare SAME AS there too.
      *> Refusing any fails the case. Each subject takes WT's
      *> description, PIC X(4) (§2002:13.16.47.3 GR1), so "EF" pads to
      *> "EF ", CDREC is 12 positions, and WG and WX pad likewise.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP13.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WT PIC X(4).
       01  WG IS GLOBAL SAME AS WT.
       01  WX IS EXTERNAL SAME AS WT.
       01  L PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 B1 SAME AS WT.
           05 B2 SAME AS WT OCCURS 2.
       PROCEDURE DIVISION.
           MOVE "ABCD" TO B1
           MOVE "EF" TO B2(1)
           MOVE "GHIJ" TO B2(2)
           MOVE FUNCTION LENGTH(CDREC) TO L
           DISPLAY "CDREC=[" CDREC "] LEN=" L
           MOVE "KL" TO WG
           MOVE "MN" TO WX
           DISPLAY "WG=[" WG "] WX=[" WX "]"
           STOP RUN.
