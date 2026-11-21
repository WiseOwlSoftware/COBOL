      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_1-14
      *> rules: [1985-GR-IV.4.2.2.1.1-14, 1985-GR-IV.4.2.2.1.1-19]
      *> accept: [85]
      *> files: [TXT-16.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 GR14; §1985:IV.4.2.2.1.1 GR19 (COBOL Words).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe: 1985-GR-IV.4.2.2.1.1-9,-14,-19 text-name and
      * report-name as user-defined words (§1985:IV.4.2.2.1.1 GR14,
      * GR19); library-name in udw_types3.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDW-TYPES-2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-17 ASSIGN TO "UDWT17.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-17 REPORT IS RPT-18.
       REPORT SECTION.
       RD  RPT-18.
       01  TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(6) VALUE "RPT-18".
       PROCEDURE DIVISION.
       P0.
           COPY TXT-16.
           OPEN OUTPUT PRT-17.
           INITIATE RPT-18.
           TERMINATE RPT-18.
           CLOSE PRT-17.
           DISPLAY "RPT-OK".
           STOP RUN.
