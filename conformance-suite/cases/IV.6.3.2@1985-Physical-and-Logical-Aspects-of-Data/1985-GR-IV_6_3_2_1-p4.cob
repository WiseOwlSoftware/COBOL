      *>--- conformance-case
      *> id: 1985-GR-IV_6_3_2_1-p4
      *> rules: [1985-GR-IV.6.3.2.1-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.3.2.1 p4 (Physical and Logical Aspects of Data
      *> Description). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-06.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.6.3.2.1-p4: the Linkage Section of the called program
      * describes items referred to by the caller and the callee; its
      * structure is that of Working-Storage (a 77 entry and a record).
      * Expect: IN B: 07 XY / IN A: 08 XZ
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LKA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  CNT    PIC 99 VALUE 7.
       01  GRP.
           02 G1  PIC X VALUE "X".
           02 G2  PIC X VALUE "Y".
       PROCEDURE DIVISION.
       P0.
           CALL "LKB" USING CNT GRP.
           DISPLAY "IN A: " CNT " " GRP.
           STOP RUN.
       END PROGRAM LKA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LKB.
       DATA DIVISION.
       LINKAGE SECTION.
       77  L-CNT  PIC 99.
       01  L-GRP.
           02 L1  PIC X.
           02 L2  PIC X.
       PROCEDURE DIVISION USING L-CNT L-GRP.
       P0.
           DISPLAY "IN B: " L-CNT " " L-GRP.
           ADD 1 TO L-CNT.
           MOVE "Z" TO L2.
           EXIT PROGRAM.
       END PROGRAM LKB.
