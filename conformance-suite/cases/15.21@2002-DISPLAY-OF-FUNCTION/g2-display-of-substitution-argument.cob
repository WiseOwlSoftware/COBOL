      *>--- conformance-case
      *> id: g2-display-of-substitution-argument
      *> rules: [2002-AR-15.21.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.21.2 AR2 - "Argument-2 shall be of class alphabetic or
      *> alphanumeric and shall be one character position in length.
      *> Argument-2 specifies an alphanumeric substitution character"
      *> for national characters with no alphanumeric correspondent.
      *> Each permitted form is written: a PIC X item (class
      *> alphanumeric), a PIC A item (class alphabetic) and a
      *> one-character alphanumeric literal.
      *> §2002:15.21.3 RV1 leaves the correspondence to the implementor;
      *> WiseOwl documents it as the total identity, every national
      *> character having its alphanumeric correspondent
      *> (docs/conformance/7/DOC-A.1-33.md), so §2002:15.21.3 RV2's
      *> substitution never fires and each result is argument-1's
      *> characters, of length 3 (§2002:15.21.3 RV4):
      *>   SX  DISPLAY-OF(NN, SUBX)  -> [ABC]
      *>   SA  DISPLAY-OF(NN, SUBA)  -> [ABC]
      *>   SL  DISPLAY-OF(NN, "#")   -> [ABC]
      *>   LEN LENGTH(DISPLAY-OF(NN, "#")) -> 003
      *>   WX  NW holds a CJK character between A and B; it corresponds,
      *>       so position 2 is not the substitute "#" and the result
      *>       equals DISPLAY-OF(NW) without argument-2 -> KEPT, SAME
      *> A leg fails if a permitted argument-2 form is refused, if the
      *> substitute replaces a character that has a correspondent, or if
      *> argument-2 alters the length.
      *> The over-long argument-2 is refused by
      *> g2-display-of-arg2-length-refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DSO01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NN      PIC N(3) VALUE N"ABC".
       01  NW      PIC N(3) VALUE N"A世B".
       01  SUBA    PIC A VALUE "Q".
       01  SUBX    PIC X VALUE "?".
       01  R       PIC X(3).
       01  R2      PIC X(3).
       01  L       PIC 9(3).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION DISPLAY-OF(NN, SUBX) TO R
           DISPLAY "SX=[" R "]"
           MOVE FUNCTION DISPLAY-OF(NN, SUBA) TO R
           DISPLAY "SA=[" R "]"
           MOVE FUNCTION DISPLAY-OF(NN, "#") TO R
           DISPLAY "SL=[" R "]"
           MOVE FUNCTION LENGTH(FUNCTION DISPLAY-OF(NN, "#")) TO L
           DISPLAY "LEN=" L
           MOVE FUNCTION DISPLAY-OF(NW, "#") TO R
           IF R(1:1) = "A" AND R(3:1) = "B" AND R(2:1) NOT = "#"
               DISPLAY "WX=KEPT"
           ELSE
               DISPLAY "WX=SUBSTITUTED"
           END-IF
           MOVE FUNCTION DISPLAY-OF(NW) TO R2
           IF R = R2 DISPLAY "WXEQ=SAME" ELSE DISPLAY "WXEQ=DIFF"
           END-IF
           STOP RUN.
