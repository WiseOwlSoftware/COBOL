      *>--- conformance-case
      *> id: pb5103-inspect-converting-identifier-size-85
      *> rules: [SR-14.9.22.3-9]
      *> source-format: fixed
      *>---
      *> kb/Work PB5103: refused at --std 85 only.
      *> §1985:VI.6.18.3 SR9: "The size of literal-5 or the data item
      *> referenced by identifier-7 must be equal to the size of
      *> literal-4 or the data item referenced by identifier-6". WS-F
      *> is two characters and WS-T1 one, so CONVERTING WS-F TO WS-T1
      *> breaks the rule at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG5103C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X  PIC X(6) VALUE "ABCDAB".
       01  WS-F  PIC XX VALUE "AB".
       01  WS-T1 PIC X VALUE "0".
       01  WS-T2 PIC XX VALUE "00".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-X CONVERTING WS-F TO WS-T1.
           DISPLAY WS-X.
           STOP RUN.
