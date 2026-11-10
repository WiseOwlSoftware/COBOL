      *>--- conformance-case
      *> id: pb5103-inspect-replacing-identifier-size-85
      *> rules: [SR-14.9.22.3-6]
      *> source-format: fixed
      *>---
      *> kb/Work PB5103: refused at --std 85 only.
      *> §1985:VI.6.18.3 SR7: "The size of literal-3 or the data item
      *> referenced by identifier-5 must be equal to the size of
      *> literal-1 or the data item referenced by identifier-3". WS-F
      *> is two characters and WS-T1 one, so REPLACING ALL WS-F BY
      *> WS-T1 breaks the rule at --std 85 (from 2002 the same words
      *> are GR14, the run-time EC-RANGE-INSPECT-SIZE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG5103A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X  PIC X(6) VALUE "ABCDAB".
       01  WS-F  PIC XX VALUE "AB".
       01  WS-T1 PIC X VALUE "0".
       01  WS-T2 PIC XX VALUE "00".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-X REPLACING ALL WS-F BY WS-T1.
           DISPLAY WS-X.
           STOP RUN.
