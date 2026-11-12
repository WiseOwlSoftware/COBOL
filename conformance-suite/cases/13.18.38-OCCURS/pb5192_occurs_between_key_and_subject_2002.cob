      *>--- conformance-case
      *> id: pb5192_occurs_between_key_and_subject_2002
      *> rules: [SR-13.18.38.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5192 - from COBOL-2002 an entry with an OCCURS clause may stand
      *> between the subject and its key as long as it is not a group in the
      *> key's hierarchy (§2002:F.2, item 84: "An entry between a data description
      *> entry that contains an OCCURS clause and the entry describing a data item
      *> referenced in the KEY phrase of the OCCURS may itself contain an OCCURS
      *> clause, as long as it is not a group item in the hierarchy of the data
      *> item referenced in the KEY phrase", cite.py --check OK). S below is an
      *> elementary table beside the key K, so SEARCH ALL is legal. At COBOL-85
      *> §1985:VI.5.8.3 SR12 bars "any entry that contains an OCCURS clause"
      *> there (pb5192-occurs-between-subject-and-key-1985).
      *> DERIVATION: the three elements are AA1, BB2, CC3 with keys 1, 2, 3
      *> ascending; K (I) = 2 holds for I = 2, whose S (2, 1) is "B": the
      *> program displays FOUND B.
      *> Fails if: the table is refused at 2002, 2014 or 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5192OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E OCCURS 3 TIMES ASCENDING KEY IS K INDEXED BY I.
               10  S PIC X OCCURS 2 TIMES.
               10  K PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE "AA1BB2CC3" TO T.
           SEARCH ALL E
               AT END DISPLAY "NOT FOUND"
               WHEN K (I) = 2 DISPLAY "FOUND " S (I, 1).
           STOP RUN.
