      *>--- conformance-case
      *> id: g2-subscript-index-cd-table
      *> rules: [2002-SR-8.4.1.2.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.2.2 SR4 - "Index-name-1 shall correspond to a data
      *> description entry in the hierarchy of the table being
      *> referenced that contains an INDEXED BY phrase specifying that
      *> index-name." At 2002 the table may be described in a record
      *> under a CD entry (§2002:A.1.2.2 SR9, §2002:8.4.1.1.1).
      *> CX is in CE's own INDEXED BY: CE (CX), CE (CX + 1), CE (CX - 1)
      *> are occurrences 2, 3, 1 (§2002:8.4.1.2.3 GR1c) - X1 BB CC AA;
      *> qualified by the cd-name, X2 BB. GX (of G) and EX (of E) are
      *> both in the hierarchy of E: E (GX, EX) is row 2 col 1 = R and
      *> E (GX - 1, EX + 1) row 1 col 2 = Q - X3 RQ. Every leg can fail:
      *> a refused index-name fails the compile, a wrong occurrence
      *> mapping prints other letters. The refused half is
      *> g2-subscript-index-cd-foreign-* beside it.
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IDX01.
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
           DISPLAY "X1 " CE (CX) " " CE (CX + 1) " " CE (CX - 1).
           DISPLAY "X2 " CE OF CDREC IN CDOUT (CX).
           DISPLAY "X3 " E (GX, EX) E (GX - 1, EX + 1).
           STOP RUN.
