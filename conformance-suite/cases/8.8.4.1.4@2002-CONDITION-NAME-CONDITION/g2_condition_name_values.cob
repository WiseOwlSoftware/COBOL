      *>--- conformance-case
      *> id: g2_condition_name_values
      *> rules: [2002-GR-8.8.4.1.4.2-1, 2002-GR-8.8.4.1.4.2-2,
      *>    2002-GR-8.8.4.1.4.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.4.2 (cite.py OK on GR1, GR2, GR3):
      *>  GR1 "the conditional variable is tested to determine whether
      *>      or not its value falls in this range, including the end
      *>      values."
      *>  GR2 "The rules for comparing a conditional variable with a
      *>      condition-name value are the same as those specified for
      *>      relation conditions."
      *>  GR3 "The result of the test is true if one of the values
      *>      corresponding to condition-name-1 equals the value of its
      *>      associated conditional variable."
      *> Relation rules GR2 reaches (cite.py OK, clause level):
      *>  §2002:8.8.4.1.1.3 numeric: "a comparison is made with
      *>      respect to the algebraic value of the operands regardless
      *>      of the manner in which their usage is described";
      *>  §2002:8.8.4.1.1.6.1 alphanumeric, unequal length: the shorter
      *>      is "extended on the right by sufficient alphanumeric
      *>      spaces"; §2002:8.8.4.1.1.8.1 national: "extended on the
      *>      right by sufficient national spaces"; §2002:8.8.4.1.1.7
      *>      boolean: "extended on the right by sufficient boolean
      *>      zeros".
      *> DERIVATION (GR1: R-*, GR3: M-*, GR2: the rest):
      *>  NV-RNG is -10 THRU -1: NV=-10 T (low end), -1 T (high end),
      *>    0 F, -11 F.
      *>  NV-MULTI is 3, 7, 11 THRU 20: 7 T (one value), 5 F, 20 T
      *>    (end of the range among the values), 21 F.
      *>  DV PIC 9V99 = 2.50, 88 VALUE 2.5: equal algebraic values  T
      *>  BN BINARY PIC S9(4) = 7, 88 VALUE 7: usage irrelevant      T
      *>  AX PIC X(4) = "AB", 88 VALUE "AB": "AB" padded = "AB  "    T
      *>  AR PIC X(3) 88 VALUE "B" THRU "C": "BCD" lies between
      *>    "B  " and "C  " T; "CAA" > "C  " F (A above space).
      *>  NN PIC N(3) = N"ABC", 88 N"AAA" THRU N"AZZ"                T
      *>  NP PIC N(3) = N"AB", 88 VALUE N"AB": padded with national
      *>    spaces                                                   T
      *>  BV PIC 1(3) = B"100", 88 VALUE B"1": B"1" extended with
      *>    boolean zeros is B"100"                                  T;
      *>    BV = B"101" F.
      *> A leg fails if an end value is excluded, a value other than
      *> the first is ignored, or the comparison is by image (DV,
      *> BN), unpadded (AX, AR, NP) or space-filled for boolean (BV).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NV PIC S9(3).
           88  NV-RNG VALUE -10 THRU -1.
           88  NV-MULTI VALUE 3 7 11 THRU 20.
       01  DV PIC 9V99 VALUE 2.50.
           88  DV-25 VALUE 2.5.
       01  BN PIC S9(4) USAGE BINARY VALUE 7.
           88  BN-7 VALUE 7.
       01  AX PIC X(4) VALUE "AB".
           88  AX-AB VALUE "AB".
       01  AR PIC X(3).
           88  AR-BC VALUE "B" THRU "C".
       01  NN PIC N(3) VALUE N"ABC".
           88  NN-RNG VALUE N"AAA" THRU N"AZZ".
       01  NP PIC N(3) VALUE N"AB".
           88  NP-AB VALUE N"AB".
       01  BV PIC 1(3) VALUE B"100".
           88  BV-1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE -10 TO NV
           IF NV-RNG DISPLAY "R1=T" ELSE DISPLAY "R1=F" END-IF
           MOVE -1 TO NV
           IF NV-RNG DISPLAY "R2=T" ELSE DISPLAY "R2=F" END-IF
           MOVE 0 TO NV
           IF NV-RNG DISPLAY "R3=T" ELSE DISPLAY "R3=F" END-IF
           MOVE -11 TO NV
           IF NV-RNG DISPLAY "R4=T" ELSE DISPLAY "R4=F" END-IF
           MOVE 7 TO NV
           IF NV-MULTI DISPLAY "M1=T" ELSE DISPLAY "M1=F" END-IF
           MOVE 5 TO NV
           IF NV-MULTI DISPLAY "M2=T" ELSE DISPLAY "M2=F" END-IF
           MOVE 20 TO NV
           IF NV-MULTI DISPLAY "M3=T" ELSE DISPLAY "M3=F" END-IF
           MOVE 21 TO NV
           IF NV-MULTI DISPLAY "M4=T" ELSE DISPLAY "M4=F" END-IF
           IF DV-25 DISPLAY "C1=T" ELSE DISPLAY "C1=F" END-IF
           IF BN-7 DISPLAY "C2=T" ELSE DISPLAY "C2=F" END-IF
           IF AX-AB DISPLAY "C3=T" ELSE DISPLAY "C3=F" END-IF
           MOVE "BCD" TO AR
           IF AR-BC DISPLAY "C4=T" ELSE DISPLAY "C4=F" END-IF
           MOVE "CAA" TO AR
           IF AR-BC DISPLAY "C5=T" ELSE DISPLAY "C5=F" END-IF
           IF NN-RNG DISPLAY "C6=T" ELSE DISPLAY "C6=F" END-IF
           IF NP-AB DISPLAY "C7=T" ELSE DISPLAY "C7=F" END-IF
           IF BV-1 DISPLAY "C8=T" ELSE DISPLAY "C8=F" END-IF
           MOVE B"101" TO BV
           IF BV-1 DISPLAY "C9=T" ELSE DISPLAY "C9=F" END-IF
           STOP RUN.
