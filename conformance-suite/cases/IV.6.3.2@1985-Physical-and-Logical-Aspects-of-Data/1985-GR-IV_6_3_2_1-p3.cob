      *>--- conformance-case
      *> id: 1985-GR-IV_6_3_2_1-p3
      *> rules: [1985-GR-IV.6.3.2.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.3.2.1 p3 (Physical and Logical Aspects of Data
      *> Description). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-06.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.6.3.2.1-p3: Working-Storage describes records and
      * subordinate items developed and processed internally, and items
      * whose values are assigned in the source program (VALUE) and do
      * not change. Expect: REC=ABC12 / K=42 / WS OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WSDEMO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  K      PIC 99 VALUE 42.
       01  REC.
           02 A   PIC X(3).
           02 N   PIC 99.
       PROCEDURE DIVISION.
       P0.
           MOVE "ABC" TO A.
           MOVE 12 TO N.
           DISPLAY "REC=" REC.
           DISPLAY "K=" K.
           DISPLAY "WS OK".
           STOP RUN.
