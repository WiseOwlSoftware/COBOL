      *>--- conformance-case
      *> id: g2occ01_cd_record_occurs
      *> rules: [2002-GR-13.16.36.3-1, 2002-GR-13.16.36.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The OCCURS clause on items of a record description entry that
      *> follows a CD entry (§2002:A.1.1 "The communication description
      *> entry is followed by none, one, or more record description
      *> entries"); the CD facility draws obsolete warning COBOL0903.
      *> §2002:13.16.36.3 GR1 - every clause but OCCURS applies to EACH
      *> occurrence: T1 is PIC X(2) in each occurrence, so "PQR" into
      *> T1(1) and "TUV" into T1(3) truncate on the right and G1 is
      *> PQRSTU; T2 is JUSTIFIED RIGHT in each occurrence, so G2 is
      *> "  A BC"; T3 is PIC 9V9 in each occurrence, so 12.34 aligns to
      *> 2.3 and 5 to 5.0 and G3 is 2350. Fails if any occurrence after
      *> the first loses its PICTURE, JUSTIFIED or scale.
      *> §2002:13.16.36.3 GR4 - integer-2 is the fixed number of
      *> occurrences: LENGTH(G1) = 3 x 2 = 6, LENGTH(G2) = 2 x 3 = 6,
      *> LENGTH(G3) = 2 x 2 = 4; LENGTH(T1(1)) = 2 (one occurrence).
      *> Fails if the table is sized as one occurrence or any other
      *> count.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCC01.
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
              10 T1 PIC X(2) OCCURS 3 TIMES.
           05 G2.
              10 T2 PIC X(3) JUSTIFIED RIGHT OCCURS 2 TIMES.
           05 G3.
              10 T3 PIC 9V9 OCCURS 2 TIMES.
       PROCEDURE DIVISION.
           MOVE "PQR" TO T1(1)
           MOVE "RS"  TO T1(2)
           MOVE "TUV" TO T1(3)
           MOVE "A"   TO T2(1)
           MOVE "BC"  TO T2(2)
           MOVE 12.34 TO T3(1)
           MOVE 5     TO T3(2)
           DISPLAY "G1=[" G1 "] G2=[" G2 "] G3=[" G3 "]"
           DISPLAY "LEN T1 " FUNCTION LENGTH(T1(1))
                   " G1 " FUNCTION LENGTH(G1)
                   " G2 " FUNCTION LENGTH(G2)
                   " G3 " FUNCTION LENGTH(G3)
           STOP RUN.
