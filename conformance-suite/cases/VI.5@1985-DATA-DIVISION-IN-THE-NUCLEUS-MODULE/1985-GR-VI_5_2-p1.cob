      *>--- conformance-case
      *> id: 1985-GR-VI_5_2-p1
      *> rules: [1985-GR-VI.5.2-p1, 1985-GR-VI.5.2.1-p1, 1985-GR-VI.5.3.1-p1, 1985-GR-VI.5.8.1-p1, 1985-SR-VI.5.11.1-p1, 1985-SR-VI.5.2.2-p1, 1985-SR-VI.5.2.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.2 p1; §1985:VI.5.2.1 p1; §1985:VI.5.3.1 p1;
      *> §1985:VI.5.8.1 p1; §1985:VI.5.11.1 p1; §1985:VI.5.2.2 p1;
      *> §1985:VI.5.2.3 p1 (DATA DIVISION IN THE NUCLEUS MODULE). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-09.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-09 probe: Working-Storage (VI.5.2): 77 noncontiguous items,
      * a record that is a single elementary item, a group record with
      * FILLER, OCCURS and subscripts, level 88, and two RENAMES (66)
      * entries whose groupings overlap (VI.5.11).
      * Expect: 07ABC / 042 / [PQ34123] / PQ34 / 34 / 4 / ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09DAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  CNT PIC 99 VALUE 7.
       77  NAM PIC X(3) VALUE "ABC".
       01  SINGLE PIC 9(3) VALUE 42.
       01  REC.
           05  FILLER PIC X VALUE "[".
           05  F1 PIC X(2) VALUE "PQ".
           05  F2.
               10  F2A PIC 9 VALUE 3.
               10  F2B PIC 9 VALUE 4.
           05  TAB1 PIC 9 OCCURS 3 TIMES.
           05  FILLER PIC X VALUE "]".
       66  ALT1 RENAMES F1 THRU F2.
       66  ALT2 RENAMES F2A THRU F2B.
       01  FLAG PIC 9 VALUE 1.
           88  IS-ON VALUE 1.
       PROCEDURE DIVISION.
       P0.
           DISPLAY CNT NAM.
           DISPLAY SINGLE.
           MOVE 1 TO TAB1 (1).
           MOVE 2 TO TAB1 (2).
           MOVE 3 TO TAB1 (3).
           DISPLAY REC.
           DISPLAY ALT1.
           DISPLAY ALT2.
           DISPLAY F2B.
           IF IS-ON DISPLAY "ON" ELSE DISPLAY "OFF".
           STOP RUN.
