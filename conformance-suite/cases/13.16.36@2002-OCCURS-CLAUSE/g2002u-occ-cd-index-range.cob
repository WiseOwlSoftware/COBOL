      *>--- conformance-case
      *> id: g2002u-occ-cd-index-range
      *> rules: [2002-GR-13.16.36.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.3 GR2 - the implementor's index range "shall
      *> include at least the value corresponding to the occurrence
      *> number (1 - integer-2) through and including the value
      *> corresponding to the occurrence number (2 * integer-2)"; an index
      *> is modified only by PERFORM VARYING, SEARCH and SET, and a value
      *> one of them creates outside that range sets EC-RANGE-INDEX.
      *> The table is in a record under an OUTPUT CD (§2002:A.1.2.2 SR9),
      *> OCCURS 5, so occurrence numbers -4 through 10 must be legal.
      *> The index moves by PERFORM VARYING only: §2002:14.8.35.3 GR3 b)
      *> makes a SET UP/DOWN BY outside the table's subscripts a
      *> bound-subscript failure, so SET cannot reach these values.
      *> The implementor range is the signed 64-bit interval
      *> (docs/conformance/7/DOC-A.1-128.md).
      *> EXPECTED, DERIVED FROM THE 2002 TEXT:
      *> LOW  - FROM 5 BY -3 UNTIL IX < -3 visits 5, 2, -1 (3 bodies) and
      *>        stops at -4 = 1 - integer-2: in range, no EC, IX = -4.
      *> HIGH - FROM 1 BY 3 UNTIL IX > 9 visits 1, 4, 7 (3 bodies) and
      *>        stops at 10 = 2 * integer-2: in range, no EC, IX = 10.
      *> OUT  - FROM 1 BY 4611686018427387904 runs the body at 1 and at
      *>        4611686018427387905; the next augment gives 2**63 + 1,
      *>        outside the range: EC-RANGE-INDEX, the declarative runs and
      *>        RESUME AT NEXT STATEMENT (§2002:14.8.31.3 GR2) continues
      *>        after the PERFORM. The index value is then undefined, so it
      *>        is not displayed.
      *> A leg fails if an in-range value raises the EC (a range narrower
      *> than GR2's floor), if IX is not the stopping value, or if the
      *> out-of-range augment raises nothing.
       >>TURN EC-RANGE-INDEX CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCIXR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EC-SEEN PIC X VALUE "N".
       01  BODIES  PIC 9 VALUE 0.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  R1.
           05 E PIC 9(2) OCCURS 5 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HRANGE SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-INDEX.
       HRANGE-P.
           MOVE "Y" TO EC-SEEN
           DISPLAY "RANGE=" FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 0 TO BODIES
           PERFORM VARYING IX FROM 5 BY -3 UNTIL IX < -3
               ADD 1 TO BODIES
           END-PERFORM
           IF IX = -4
               DISPLAY "LOW BODIES=" BODIES " AT-4 EC=" EC-SEEN
           ELSE
               DISPLAY "LOW BODIES=" BODIES " NOT-4 EC=" EC-SEEN
           END-IF
           MOVE 0 TO BODIES
           PERFORM VARYING IX FROM 1 BY 3 UNTIL IX > 9
               ADD 1 TO BODIES
           END-PERFORM
           IF IX = 10
               DISPLAY "HIGH BODIES=" BODIES " AT10 EC=" EC-SEEN
           ELSE
               DISPLAY "HIGH BODIES=" BODIES " NOT10 EC=" EC-SEEN
           END-IF
           MOVE 0 TO BODIES
           PERFORM VARYING IX FROM 1 BY 4611686018427387904
                   UNTIL IX > 9223372036854775000
               ADD 1 TO BODIES
           END-PERFORM
           DISPLAY "OUT BODIES=" BODIES " EC=" EC-SEEN
           STOP RUN.
