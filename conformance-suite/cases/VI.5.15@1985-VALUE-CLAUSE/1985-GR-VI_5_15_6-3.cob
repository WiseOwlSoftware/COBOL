      *>--- conformance-case
      *> id: 1985-GR-VI_5_15_6-3
      *> rules: [1985-GR-VI.5.15.6-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> 1985:VI.5.15.6 GR3 (THE VALUE CLAUSE): "the group area is
      *> initialized without consideration for the individual
      *> elementary or group items contained within this group".
      *> kb/Work PB5851: a REDEFINES ANYWHERE UNDER THE GROUP LOST THE
      *> WHOLE VALUE - G1 PRINTED SPACES, G2 0000, AND ONE PAIR A LEVEL
      *> DOWN (G3) LOST IT FOR THE MEMBERS THAT TAKE NO PART IN THE
      *> REDEFINITION TOO. GR2 BARS A VALUE ONLY IN AN ENTRY THAT
      *> CONTAINS A REDEFINES CLAUSE OR IS SUBORDINATE TO ONE; EACH
      *> GROUP BELOW CONTAINS NEITHER (ITS SUBORDINATE DOES), SO EACH
      *> IS LEGAL 1985 SOURCE. EVERY MEMBER IS WHAT ITS OWN
      *> DESCRIPTION MAKES OF THE AREA'S CHARACTERS UNDER IT, AND A
      *> REDEFINING MEMBER READS THE SAME POSITIONS AS ITS TARGET.
      *> G4 IS THE FIGURATIVE FORM: ZEROS FILL THE WHOLE AREA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GV85R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1 VALUE "ABCD".
          05 A1 PIC X(4).
          05 B1 REDEFINES A1 PIC X(4).
       01 G2 VALUE "1234".
          05 N2 PIC 9(4).
          05 X2 REDEFINES N2 PIC X(4).
       01 G3 VALUE "AB12CD".
          05 H3.
             10 A3 PIC X(2).
             10 B3 REDEFINES A3 PIC 9(2).
          05 N3 PIC 9(2).
          05 C3 PIC X(2).
       01 G4 VALUE ZEROS.
          05 A4 PIC X(2).
          05 N4 REDEFINES A4 PIC 99.
          05 C4 PIC X(2).
       PROCEDURE DIVISION.
       P0.
           DISPLAY "G1=[" G1 "] B1=[" B1 "]".
           DISPLAY "G2=[" G2 "] N2=[" N2 "] X2=[" X2 "]".
           DISPLAY "G3=[" G3 "] A3=[" A3 "] N3=[" N3 "] C3=[" C3 "]".
           DISPLAY "G4=[" G4 "] N4=[" N4 "] C4=[" C4 "]".
           STOP RUN.
