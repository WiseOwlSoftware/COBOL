      *>--- conformance-case
      *> id: pb5073-global-data-name-duplicated-by-type-copy
      *> rules: [SR-13.18.27.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5073 review R1-1 - 2023:8.4.2.1 (cite.py: OK): a GLOBAL name is no other data item's name in the source element.
      *>   2023:13.18.57.4 GR1 (cite.py: OK): the TYPE clause is "as though the data description identified by type-name-1 had
      *>   been coded in place", so V.G is a data item named G beside the GLOBAL item G. Nothing references G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5073T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF.
           05  G PIC X.
       01  G PIC X GLOBAL.
       01  V TYPE T.
       PROCEDURE DIVISION.
       P-MAIN.
           STOP RUN.
