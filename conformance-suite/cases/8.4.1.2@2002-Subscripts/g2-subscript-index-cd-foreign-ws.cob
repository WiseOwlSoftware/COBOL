      *>--- conformance-case
      *> id: g2-subscript-index-cd-foreign-ws
      *> rules: [2002-SR-8.4.1.2.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.2.2 SR4 - "Index-name-1 shall correspond to a data
      *> description entry in the hierarchy of the table being
      *> referenced that contains an INDEXED BY phrase specifying that
      *> index-name." At 2002 the table may be described in a record
      *> under a CD entry (§2002:A.1.2.2 SR9, §2002:8.4.1.1.1).
      *> WX is the index-name of the working-storage table WE; it is in
      *> no INDEXED BY of the hierarchy of CE, so CE (WX) is refused.
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IDX02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WT.
           05 WE PIC X OCCURS 3 TIMES INDEXED BY WX.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 CE PIC X(2) OCCURS 3 TIMES INDEXED BY CX.
           05 G OCCURS 2 TIMES INDEXED BY GX.
              10 E PIC X OCCURS 2 TIMES INDEXED BY EX.
       PROCEDURE DIVISION.
           MOVE "AABBCCPQRS" TO CDREC.
           MOVE "XYZ" TO WT.
           SET CX TO 2.
           SET WX TO 1.
           SET GX TO 2.
           SET EX TO 1.
           DISPLAY CE (WX).
           STOP RUN.
