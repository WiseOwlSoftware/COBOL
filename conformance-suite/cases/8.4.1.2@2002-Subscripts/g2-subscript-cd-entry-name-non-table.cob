      *>--- conformance-case
      *> id: g2-subscript-cd-entry-name-non-table
      *> rules: [2002-SR-8.4.1.2.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.2.2 SR2 - "If a subscript is specified, the data
      *> description entry describing qualified-data-name-1 or the
      *> conditional variable associated with qualified-condition-name-1
      *> shall contain an OCCURS clause or shall be subordinate to a
      *> data description entry that contains an OCCURS clause."
      *> TL is the TEXT LENGTH data-name the CD entry itself defines,
      *> an elementary 02 PICTURE 9(4) under data-name-0 with no
      *> OCCURS (§2002:A.1.2.3 GR16 implicit description), so TL (1)
      *> is refused.
      *> The accepted half is g2-subscript-cd-table-element.
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUB05.
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
           DISPLAY TL (1).
           STOP RUN.
