      *>--- conformance-case
      *> id: g2002-national-literal-161-positions
      *> rules: [2002-SR-8.3.1.2.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.4.2 SR1 - at most 160 national character
      *> positions.
      *> The literal below, continued with the floating indicator, holds
      *> 161 (80 A, 81 B) and is refused. Its 160 twin is
      *> g2002_national_literal_forms.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NAT04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 N1 PIC N(161).
       PROCEDURE DIVISION.
           MOVE N"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"-
           "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBB"-
           "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"-
           "BBBBBBBBBBB"
               TO N1
           STOP RUN.
