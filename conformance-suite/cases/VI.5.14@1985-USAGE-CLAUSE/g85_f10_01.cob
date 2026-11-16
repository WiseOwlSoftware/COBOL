      *>--- conformance-case
      *> id: g85_f10_01
      *> rules: [1985-SR-VI.5.14.3-1, 1985-SR-VI.5.14.3-2,
      *>    1985-SR-VI.5.14.3-4, 1985-GR-VI.5.14.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.3 SR1 - USAGE written in a level 77, a level 01
      *>   group, a level 05 group and level 05/10 elementary entries.
      *> §1985:VI.5.14.3 SR2 - a group USAGE repeated in a subordinate
      *>   group (CMP-SUB) and a subordinate elementary item (CA, CC, DA)
      *>   with the same usage.
      *> §1985:VI.5.14.3 SR4 - "COMP is an abbreviation for
      *>   COMPUTATIONAL": CMP-GRP says COMP, CMP-SUB and CC say
      *>   COMPUTATIONAL; SR2's "same usage" holds only through SR4.
      *> §1985:VI.5.14.4 GR1 - "it applies to each elementary item in
      *>   the group": I1 and I2 carry no USAGE and no PICTURE; they are
      *>   index data items only because IDX-GRP's USAGE INDEX applies
      *>   to them, so SET and the relation condition on them hold
      *>   occurrence numbers (lines 1-4).
      *> Fails if: the pin refuses COMP beside COMPUTATIONAL (SR2/SR4),
      *>   refuses the pictureless I1/I2 (GR1), or any value differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1001.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  W77 PIC S9(4) USAGE IS COMPUTATIONAL VALUE -17.
       01  TBL-AREA.
           05  TBL-VALS PIC X(5) VALUE "ABCDE".
           05  TBL-R REDEFINES TBL-VALS.
               10  TBL PIC X OCCURS 5 TIMES INDEXED BY X1.
       01  IDX-GRP USAGE IS INDEX.
           05  I1.
           05  I2.
       01  CMP-GRP USAGE IS COMP.
           05  CMP-SUB USAGE IS COMPUTATIONAL.
               10  CA PIC S9(4) USAGE COMP.
               10  CB PIC 9(3).
           05  CC PIC S9(5) COMPUTATIONAL.
       01  DSP-GRP USAGE DISPLAY.
           05  DA PIC X(3) USAGE IS DISPLAY.
           05  DB PIC 9(2).
       01  OUT-N PIC -9(5).
       PROCEDURE DIVISION.
       P-MAIN.
           SET X1 TO 4.
           SET I1 TO X1.
           SET X1 TO 2.
           SET I2 TO X1.
           SET X1 TO I1.
           DISPLAY "GR1 I1 " TBL (X1).
           SET X1 TO I2.
           DISPLAY "GR1 I2 " TBL (X1).
           IF I1 = I2
               DISPLAY "GR1 I1=I2"
           ELSE
               DISPLAY "GR1 I1<>I2".
           SET I2 TO I1.
           IF I1 = I2
               DISPLAY "GR1 I1=I2"
           ELSE
               DISPLAY "GR1 I1<>I2".
           MOVE -1234 TO CA.
           MOVE 567 TO CB.
           COMPUTE CC = CA - CB.
           MOVE CC TO OUT-N.
           DISPLAY "SR2 CC " OUT-N.
           ADD W77 TO CC.
           MOVE CC TO OUT-N.
           DISPLAY "SR1 CC " OUT-N.
           MOVE "XYZ" TO DA.
           MOVE 7 TO DB.
           DISPLAY "SR2 DSP " DSP-GRP.
           STOP RUN.
