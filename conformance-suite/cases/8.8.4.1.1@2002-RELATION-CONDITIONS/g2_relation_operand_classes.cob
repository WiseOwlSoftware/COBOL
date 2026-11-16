      *>--- conformance-case
      *> id: g2_relation_operand_classes
      *> rules: [2002-SR-8.8.4.1.1.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.1.2 SR2 (cite.py OK): "All identifiers shall be
      *> of class alphabetic, alphanumeric, national, or numeric or
      *> shall be strongly-typed group items." The accepted side: one
      *> Format 1 relation per permitted class and pairing.
      *> C1 alphabetic/alphabetic "ABC" < "ABD" T. C2 alphabetic vs
      *> alphanumeric, "A class alphabetic operand shall be treated as
      *> though it were an operand of class alphanumeric"
      *> (§2002:8.8.4.1.1, cite.py OK): "ABC" = "ABC" T. C3
      *> alphanumeric vs national, "Two operands, one class
      *> alphanumeric and one class national, may be compared"
      *> (§2002:8.8.4.1.1.5, cite.py OK): T. C4 N"ABC" = "ABD" F.
      *> C5 numeric 123 = +123.0 by algebraic value
      *> (§2002:8.8.4.1.1.3, cite.py OK) T. C6/C7 strongly-typed
      *> groups of one type (§2002:8.8.4.1.1.2 SR1, cite.py OK),
      *> compared item by item (§2002:8.8.4.1.1.9, cite.py OK):
      *> (12,"AB") < (12,"AC") T; (12,"AB") = (12,"AB") T.
      *> The refused side is g2-relation-boolean-identifier and
      *> g2-relation-pointer-identifier. A leg fails if a permitted
      *> class is refused or compared by the wrong rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REL06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AL1 PIC A(3) VALUE "ABC".
       01  AL2 PIC A(3) VALUE "ABD".
       01  AN1 PIC X(3) VALUE "ABC".
       01  NA1 PIC N(3) VALUE N"ABC".
       01  NU1 PIC 9(3) VALUE 123.
       01  NU2 PIC S9(3)V9 VALUE +123.0.
       01  ST-T TYPEDEF STRONG.
           05  ST-K PIC 99.
           05  ST-N PIC X(2).
       01  SG1 TYPE ST-T.
       01  SG2 TYPE ST-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF AL1 < AL2 DISPLAY "C1 T" ELSE DISPLAY "C1 F".
           IF AL1 = AN1 DISPLAY "C2 T" ELSE DISPLAY "C2 F".
           IF AN1 = NA1 DISPLAY "C3 T" ELSE DISPLAY "C3 F".
           IF NA1 = AL2 DISPLAY "C4 T" ELSE DISPLAY "C4 F".
           IF NU1 = NU2 DISPLAY "C5 T" ELSE DISPLAY "C5 F".
           MOVE 12 TO ST-K OF SG1. MOVE "AB" TO ST-N OF SG1.
           MOVE 12 TO ST-K OF SG2. MOVE "AC" TO ST-N OF SG2.
           IF SG1 < SG2 DISPLAY "C6 T" ELSE DISPLAY "C6 F".
           MOVE "AB" TO ST-N OF SG2.
           IF SG1 = SG2 DISPLAY "C7 T" ELSE DISPLAY "C7 F".
           STOP RUN.
