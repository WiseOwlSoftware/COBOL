      *>--- conformance-case
      *> id: pb5866-controls-renames-one-item-85
      *> rules: [1985-SR-XIII.3.7.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5866. §1985:XIII.3.7.3 SR2 (cite.py: OK): "Each
      *>   recurrence of data-name-1 must identify a different data item".
      *> R66 is a level 66 entry without THROUGH: §1985:VI.5.11.4 GR2
      *>   (cite.py: OK): "all of the data attributes of data-name-2
      *>   become the data attributes for data-name-1", so R66 identifies
      *>   the data item WS-B, and CONTROLS ARE WS-B R66 names one data
      *>   item twice, as CONTROLS ARE WS-B WS-B does: refused (COBOL1699).
      *> conformance:pb5866_controls_distinct_items_85 is the legal side:
      *>   a level 66 entry WITH THROUGH and a REDEFINES item are other
      *>   data items.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5866RN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO P5866RN.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  WS-A PIC X VALUE "A".
           05  WS-B PIC X VALUE "B".
       66  R66 RENAMES WS-B.
       REPORT SECTION.
       RD  RPT CONTROLS ARE WS-B R66.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X SOURCE WS-A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
