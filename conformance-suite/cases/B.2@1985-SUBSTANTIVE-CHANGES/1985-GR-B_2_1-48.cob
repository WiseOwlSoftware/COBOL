      *>--- conformance-case
      *> id: 1985-GR-B_2_1-48
      *> rules: [1985-GR-B.2.1-48]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:B.2.1 GR48 (SUBSTANTIVE CHANGES). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 B.2.1 item 48 / XIV CD FOR I-O: one CD entry serves
      * both input (RECEIVE) and output (SEND). Compile-only: the
      * MCS is the implementor's; acceptance is the witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDFIO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG               PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  CDIO FOR I-O.
       01  CDIO-REC.
           05  CDIO-TERM     PIC X(12).
           05  CDIO-DATE     PIC 9(6).
           05  CDIO-TIME     PIC 9(8).
           05  CDIO-TLEN     PIC 9(4).
           05  CDIO-STKEY    PIC XX.
           05  CDIO-ENDKEY   PIC X.
       PROCEDURE DIVISION.
       P1.
           RECEIVE CDIO MESSAGE INTO MSG.
           MOVE 5 TO CDIO-TLEN.
           SEND CDIO FROM MSG WITH EMI.
           STOP RUN.
