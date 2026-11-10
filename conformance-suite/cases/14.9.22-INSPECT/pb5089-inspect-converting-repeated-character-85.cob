      *>--- conformance-case
      *> id: pb5089-inspect-converting-repeated-character-85
      *> rules: [GR-14.9.22.4-23]
      *> source-format: fixed
      *>---
      *> kb/Work PB5089: refused at --std 85 only.
      *> §1985:VI.6.18.3 SR10: "The same character must not appear
      *> more than once either in literal-4 or in the data item
      *> referenced by identifier-6". Literal-4 "ABA" holds A twice,
      *> so the statement breaks the rule at --std 85 (from 2002 the
      *> first occurrence decides, GR23 of 2023).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG5089A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X  PIC X(6) VALUE "ABCDAB".
       01  WS-F  PIC XX VALUE "AB".
       01  WS-T1 PIC X VALUE "0".
       01  WS-T2 PIC XX VALUE "00".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-X CONVERTING "ABA" TO "XYZ".
           DISPLAY WS-X.
           STOP RUN.
