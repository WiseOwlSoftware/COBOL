      *>--- conformance-case
      *> id: g2002-hex-boolean-literal-164-positions
      *> rules: [2002-SR-8.3.1.2.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.3.2 SR1 - at most 160 boolean character
      *> positions.
      *> Each hexadecimal digit is four boolean characters
      *> (§2002:8.3.1.2.3.3 GR4), so the 41-digit BX literal below is
      *> 164
      *> positions and is refused. Its 40-digit (160) twin is
      *> g2002_boolean_literal_forms.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BOL04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 B1 PIC 1(164).
       PROCEDURE DIVISION.
           MOVE BX"FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"
               TO B1
           STOP RUN.
