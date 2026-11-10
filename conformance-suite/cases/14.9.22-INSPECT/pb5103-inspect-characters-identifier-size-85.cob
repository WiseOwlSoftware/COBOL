      *>--- conformance-case
      *> id: pb5103-inspect-characters-identifier-size-85
      *> rules: [SR-14.9.22.3-7]
      *> source-format: fixed
      *>---
      *> kb/Work PB5103: refused at --std 85 only.
      *> §1985:VI.6.18.3 SR8: "When the CHARACTERS phrase is used,
      *> literal-3 or the size of the data item referenced by
      *> identifier-5 must be one character in length". WS-T2 is two
      *> characters, so CHARACTERS BY WS-T2 breaks the rule at --std
      *> 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG5103B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X  PIC X(6) VALUE "ABCDAB".
       01  WS-F  PIC XX VALUE "AB".
       01  WS-T1 PIC X VALUE "0".
       01  WS-T2 PIC XX VALUE "00".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-X REPLACING CHARACTERS BY WS-T2.
           DISPLAY WS-X.
           STOP RUN.
