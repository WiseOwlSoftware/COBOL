      *>--- conformance-case
      *> id: g2-subscript-cd-condition-non-table
      *> rules: [2002-SR-8.4.1.2.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.2.2 SR2 - "If a subscript is specified, the data
      *> description entry describing qualified-data-name-1 or the
      *> conditional variable associated with qualified-condition-name-1
      *> shall contain an OCCURS clause or shall be subordinate to a
      *> data description entry that contains an OCCURS clause."
      *> S1-ONE is a condition-name whose conditional variable S1, in
      *> the record under CD CDOUT, has no OCCURS clause and no
      *> ancestor that has one, so S1-ONE (2) is refused.
      *> The accepted half is g2-subscript-cd-table-element.
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUB04.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 T1 PIC X OCCURS 3 TIMES.
           05 S1 PIC 9.
              88 S1-ONE VALUE 1.
       PROCEDURE DIVISION.
           IF S1-ONE (2) DISPLAY "X".
           STOP RUN.
