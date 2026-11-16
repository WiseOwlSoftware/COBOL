      *>--- conformance-case
      *> id: g2-lvl-cd-first-entry-not-01
      *> rules: [2002-GR-13.16.31.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.31.3 GR1 - "The level-number 1 identifies the first
      *> entry in each record description or report group." The record
      *> description that follows an OUTPUT CD opens with a level-05
      *> entry, which cannot identify the first entry of a record, so
      *> the entry is refused. The CD carries a TEXT LENGTH clause so
      *> that §2002:A.1.2.2 SR8 (a CD with none of its optional clauses
      *> shall be followed by a level 01 entry) does not apply: only
      *> the GR1 record-structure rule refuses the 05 entry. This
      *> refusal stands beside the accepted case g2-lvl-cd-records.
      *> Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LVLN1.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT TEXT LENGTH IS COUT-TL.
       05  R1 PIC X(23).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
