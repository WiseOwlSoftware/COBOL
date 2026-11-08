      *>--- conformance-case
      *> id: pb4939-set-not-admitted-cites-1985-rules-at-85
      *> rules: [SR-14.9.39.3-23]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4939. At --std 85 a SET UP/DOWN BY list that holds an
      *> index-name beside an integer data item selects Format 2, whose
      *> receiving operand is index-name-3 (§1985:VI.6.23.2 Format 2,
      *> §1985:VI.6.23.4 GR4; both checked with cite.py --check). WS-N is
      *> an integer data item, so Format 2's own rule refuses it, and the
      *> refusal cites those 1985 clauses, not the 2023 ones.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4939B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 1.
       01 T.
          05 E OCCURS 5 INDEXED BY I1 PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-N I1 UP BY 2
           STOP RUN.
