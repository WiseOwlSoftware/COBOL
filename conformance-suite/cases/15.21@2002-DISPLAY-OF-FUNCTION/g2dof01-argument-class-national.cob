      *>--- conformance-case
      *> id: g2dof01-argument-class-national
      *> rules: [2002-AR-15.21.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.21.2 AR1 - "Argument-1 shall be of class national." Each
      *>   argument-1 below is of class national by §2002:8.5.2 Table 3 and
      *>   §2002:8.5.2.7, so each is ACCEPTED and converted:
      *>   NAT     PIC N(3), national by its PICTURE (8.5.2.7 item 1);
      *>   NAT-ED  PIC NBN, category national-edited (8.5.2.8), class
      *>           national;
      *>   NUM-ED  PIC ZZ9.9 USAGE NATIONAL, numeric-edited of usage
      *>           national, class national ("Numeric-edited (if usage is
      *>           national)");
      *>   NAT-GRP a group with GROUP-USAGE NATIONAL (8.5.2.7 item 4);
      *>   NAT-LIT the national literal N"LIT";
      *>   NAT-FN  FUNCTION NATIONAL-OF("FN"), a national function (8.5.2.7
      *>           item 5).
      *> §2002:15.21.3 RV1 and RV4: each national character becomes its
      *>   alphanumeric correspondent, the returned value as long as the
      *>   converted argument. The correspondence is WiseOwl's documented
      *>   identity (docs/conformance/7/DOC-A.1-33.md), so N"ABC" gives
      *>   "ABC", "X Y" stays "X Y", " 12.5" stays " 12.5"; LEN-NUM-ED is 5.
      *> Not of class national, and refused beside this case (not the
      *>   closing witness): PIC 999 USAGE NATIONAL, class numeric in 2002
      *>   (§2002:8.5.2.9 item 1; kb/Work PB5625 reading B),
      *>   g2dof02-argument-numeric-national; and an alphanumeric literal,
      *>   g2dof03-argument-alphanumeric.
      *> A compiler that refused any form above fails to compile the case;
      *>   one that converted the wrong characters prints other text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DOF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NN              PIC N(3) VALUE N"ABC".
       01  NE              PIC NBN.
       01  NNE             PIC ZZ9.9 USAGE NATIONAL.
       01  NG              GROUP-USAGE NATIONAL.
           05  NG1         PIC N(2) VALUE N"GH".
           05  NG2         PIC N(1) VALUE N"I".
       01  R               PIC X(8).
       01  L               PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE N"XY" TO NE
           MOVE 12.5 TO NNE
           MOVE FUNCTION DISPLAY-OF(NN) TO R
           DISPLAY "NAT=[" R "]"
           MOVE FUNCTION DISPLAY-OF(NE) TO R
           DISPLAY "NAT-ED=[" R "]"
           MOVE FUNCTION DISPLAY-OF(NNE) TO R
           DISPLAY "NUM-ED=[" R "]"
           MOVE FUNCTION LENGTH(FUNCTION DISPLAY-OF(NNE)) TO L
           DISPLAY "LEN-NUM-ED=" L
           MOVE FUNCTION DISPLAY-OF(NG) TO R
           DISPLAY "NAT-GRP=[" R "]"
           MOVE FUNCTION DISPLAY-OF(N"LIT") TO R
           DISPLAY "NAT-LIT=[" R "]"
           MOVE FUNCTION DISPLAY-OF(FUNCTION NATIONAL-OF("FN")) TO R
           DISPLAY "NAT-FN=[" R "]"
           STOP RUN.
