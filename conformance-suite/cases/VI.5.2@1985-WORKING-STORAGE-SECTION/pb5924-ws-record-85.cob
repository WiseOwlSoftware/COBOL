      *>--- conformance-case
      *> id: pb5924-ws-record-85
      *> rules: [1985-SR-VI.5.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.5.2.2 (cite.py: OK): "must be grouped
      *> into records according to the rules for formation of record
      *> descriptions"
      *> The same hierarchy grouped into a working-storage record:
      *> compiles, and each group reads its subordinates' content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59247.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R.
           05  A PIC X VALUE "P".
           05  G.
               10  B PIC X VALUE "Q".
               10  C PIC 99 VALUE 42.
       PROCEDURE DIVISION.
       P0.
           DISPLAY R.
           DISPLAY G.
           STOP RUN.
