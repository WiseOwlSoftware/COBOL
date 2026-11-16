      *>--- conformance-case
      *> id: g85_bf17_04
      *> rules: [1985-GR-VI.6.23.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.4 GR8 - several condition-names in one
      *> format 4 SET act as separate SETs in the written order.
      * GR7 places each condition-name's first VALUE literal in
      * its conditional variable. H-ONE and H-TWO share WS-H, so
      * the order is visible: the later one wins.
      *   SET H-ONE H-TWO -> 1 then 2: H12=2
      *   SET H-TWO H-ONE -> 2 then 1: H21=1
      *   SET F-YES H-TWO -> two variables, both set: F=Y H=2
      * H-TWO's VALUE 2 5 places 2, its first literal (GR7).
      * A SET that ignored order, or set only the first name,
      * prints a different digit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1704.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-H PIC 9 VALUE 0.
          88 H-ONE VALUE 1.
          88 H-TWO VALUE 2 5.
       01 WS-F PIC X VALUE "N".
          88 F-YES VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-P.
           SET H-ONE H-TWO TO TRUE.
           DISPLAY "H12=" WS-H.
           SET H-TWO H-ONE TO TRUE.
           DISPLAY "H21=" WS-H.
           SET F-YES H-TWO TO TRUE.
           DISPLAY "F=" WS-F " H=" WS-H.
           STOP RUN.
