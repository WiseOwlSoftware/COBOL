      *>--- conformance-case
      *> id: pb5851_group_value_over_redefines
      *> rules: [GR-13.18.63.4-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5851GV.
      *> ISO 13.18.63.4 GR5 - "If a VALUE clause is specified in a data
      *> description entry of a group item, the group area is
      *> initialized without consideration for the individual
      *> elementary or group items contained within this group."
      *> kb/Work PB5851: A REDEFINES ANYWHERE UNDER THE GROUP LOST THE
      *> WHOLE VALUE AT EVERY EDITION. THE RECORD-STRUCT LANE TOOK THE
      *> MEMBER-WISE DEFAULT WHENEVER THE SUBTREE HELD A SHARED-STORAGE
      *> MEMBER, AND A CLASS ROOTED AT A MEMBER SEEDED ITS BACKING FROM
      *> THE MEMBER'S OWN (VALUE-LESS) DESCRIPTION. 13.18.63.3 SR12
      *> BARS A VALUE IN A REDEFINING ENTRY OR ONE SUBORDINATE TO IT,
      *> NEVER IN THE GROUP THAT CONTAINS THE PAIR, SO EVERY GROUP HERE
      *> IS CONFORMING. EACH MEMBER IS WHAT ITS OWN DESCRIPTION MAKES
      *> OF THE AREA'S POSITIONS UNDER IT; A REDEFINING MEMBER READS
      *> ITS TARGET'S POSITIONS (13.18.44.4 GR1).
      *>
      *> GT - A FORMAT 2 GROUP VALUE: EVERY OCCURRENCE'S AREA (GR12).
      *> NG - A NATIONAL GROUP (13.18.29.4 GR2b): NATIONAL POSITIONS,
      *>   WITH A NATIONAL NUMERIC REDEFINER (13.18.29.3 SR3).
      *> NH - THE CLASS ROOTED AT THE NATIONAL VALUE GROUP: READ AT
      *>   BYTE LEVEL THROUGH THE X VIEW WITH FUNCTION ORD, EACH
      *>   NATIONAL CHARACTER BEING TWO BYTES (DOC-A.1, D-N1).
      *> BG - A BIT GROUP (13.18.29.4 GR1b): BOOLEAN POSITIONS.
      *> GL - THE SAME IN LOCAL-STORAGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1 VALUE "ABCDEF".
          05 A1 PIC X(4).
          05 B1 REDEFINES A1 PIC X(4).
          05 C1 PIC X(2).
       01 G2 VALUE "12CD34".
          05 H2.
             10 A2 PIC X(2).
             10 N2 REDEFINES A2 PIC 99.
          05 C2 PIC X(2).
          05 D2 PIC 99.
       01 G3 VALUE "PQRSTU".
          05 H3.
             10 H31 PIC X(2).
             10 H32 PIC X(2).
          05 R3 REDEFINES H3 PIC X(4).
          05 C3 PIC X(2).
       01 GT.
          05 GTE OCCURS 2 VALUE "12YZ" FROM (1) TO (2).
             10 TA PIC X(2).
             10 TN REDEFINES TA PIC 99.
             10 TC PIC X(2).
       01 NG GROUP-USAGE NATIONAL VALUE N"12MN".
          05 NA PIC N(2).
          05 NN REDEFINES NA PIC 99 USAGE NATIONAL.
          05 NC PIC N(2).
       01 NH GROUP-USAGE NATIONAL VALUE N"KL".
          05 NH1 PIC N.
          05 NH2 PIC N.
       01 NR REDEFINES NH PIC X(4).
       01 BG GROUP-USAGE BIT VALUE B"101101".
          05 B01 PIC 1(2).
          05 B02 REDEFINES B01 PIC 1(1).
          05 B03 PIC 1(4).
       LOCAL-STORAGE SECTION.
       01 GL VALUE "1234EF".
          05 LN PIC 9(4).
          05 LX REDEFINES LN PIC X(4).
          05 LC PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "G1=[" G1 "][" B1 "][" C1 "]"
           DISPLAY "G2=[" G2 "][" N2 "][" C2 "][" D2 "]"
           DISPLAY "G3=[" H31 "][" H32 "][" R3 "][" C3 "]"
           DISPLAY "GT=[" GT "][" TN (2) "][" TC (2) "]"
           DISPLAY "NG=[" NA "][" NN "][" NC "]"
           DISPLAY "NH=[" NH1 "][" NH2 "]"
           DISPLAY "NR=" FUNCTION ORD(NR(1:1)) " " FUNCTION ORD(NR(2:1))
               " " FUNCTION ORD(NR(3:1)) " " FUNCTION ORD(NR(4:1))
           DISPLAY "BG=[" B01 "][" B02 "][" B03 "]"
           DISPLAY "GL=[" LN "][" LX "][" LC "]"
           STOP RUN.
