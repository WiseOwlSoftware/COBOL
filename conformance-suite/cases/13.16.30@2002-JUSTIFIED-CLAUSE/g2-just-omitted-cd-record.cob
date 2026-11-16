      *>--- conformance-case
      *> id: g2-just-omitted-cd-record
      *> rules: [2002-GR-13.16.30.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.30.3 GR3 - "When the JUSTIFIED clause is omitted,
      *> the standard rules for aligning data within an elementary item
      *> apply". Every receiver is an elementary item of a record
      *> description entry that follows an OUTPUT CD (the CD facility
      *> draws obsolete warning COBOL0903), none has JUSTIFIED, so each
      *> MOVE aligns by §2002:14.5.8 for its category:
      *> rule 1a, fixed-point numeric "aligned by decimal point ... with
      *> zero fill or truncation on either end": 12.345 into 9(3)V9 is
      *> 012.3 (digits 0123), 98765.4 is 765.4 (7654); rule 1b, no
      *> assumed point: 45 into 9(3) is 045, 12345 is 345;
      *> rule 3, numeric-edited "aligned by decimal point with zero fill
      *> ... except where editing requirements cause replacement of the
      *> leading zeros": 1.5 into ZZ9.99 is "  1.50";
      *> rule 5, alphanumeric/alphabetic/national "aligned at the
      *> leftmost character position ... with space fill or truncation
      *> to the right": "ABCDE" into X(3) is ABC, "A" is "A  ", "QR"
      *> into A(3) is "QR ", N"AB" into N(3) is "AB ";
      *> rule 6, boolean "aligned at the leftmost boolean position ...
      *> with zero fill or truncation to the right": B"11" into 1(4) is
      *> 1100, B"101101" is 1011.
      *> Numeric receivers are shown through their alphanumeric group
      *> (GN, GI) so no implementor-defined numeric DISPLAY conversion
      *> enters the expectation. Fails if any category right-aligns,
      *> truncates on the wrong end, or fills with the wrong character.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2JUS03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT.
       01  REC.
           05 GN.
              10 NUM PIC 9(3)V9.
           05 GI.
              10 NI  PIC 9(3).
           05 NE  PIC ZZ9.99.
           05 AX  PIC X(3).
           05 AB  PIC A(3).
           05 NX  PIC N(3).
           05 BX  PIC 1(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12.345 TO NUM
           DISPLAY "NUM1=[" GN "]"
           MOVE 98765.4 TO NUM
           DISPLAY "NUM2=[" GN "]"
           MOVE 45 TO NI
           DISPLAY "NI1=[" GI "]"
           MOVE 12345 TO NI
           DISPLAY "NI2=[" GI "]"
           MOVE 1.5 TO NE
           DISPLAY "NE=[" NE "]"
           MOVE "ABCDE" TO AX
           DISPLAY "AX1=[" AX "]"
           MOVE "A" TO AX
           DISPLAY "AX2=[" AX "]"
           MOVE "QR" TO AB
           DISPLAY "AB=[" AB "]"
           MOVE N"AB" TO NX
           DISPLAY "NX=[" NX "]"
           MOVE B"11" TO BX
           DISPLAY "BX1=[" BX "]"
           MOVE B"101101" TO BX
           DISPLAY "BX2=[" BX "]"
           STOP RUN.
