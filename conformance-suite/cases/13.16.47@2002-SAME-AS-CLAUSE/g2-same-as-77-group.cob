      *>--- conformance-case
      *> id: g2-same-as-77-group
      *> rules: [2002-SR-13.16.47.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.2 SR8 - "If the subject of the entry is a level 77
      *> item, data-name-1 shall reference an elementary item." WG is a
      *> level 1 group item, which SR7 alone would admit; the subject X is a
      *> level 77 item, so SR8 refuses it (COBOL1555). The accepted control
      *> is g2-same-as-77-elementary.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SA772.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WG.
           05 WG1 PIC X.
       77  X SAME AS WG.
       PROCEDURE DIVISION.
           DISPLAY "OK".
           STOP RUN.
