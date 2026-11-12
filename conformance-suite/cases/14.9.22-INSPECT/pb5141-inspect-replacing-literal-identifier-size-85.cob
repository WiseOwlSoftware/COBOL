      *>--- conformance-case
      *> id: pb5141-inspect-replacing-literal-identifier-size-85
      *> rules: [SR-14.9.22.3-6]
      *> source-format: fixed
      *>---
      *> kb/Work PB5141: refused at --std 85 only, and the diagnostic
      *> names each operand by what it is. §1985:VI.6.18.3 SR7: "The
      *> size of literal-3 or the data item referenced by identifier-5
      *> must be equal to the size of literal-1 or the data item
      *> referenced by identifier-3". Here literal-1 is "AB", two
      *> characters, and identifier-5 is WS-T1, one, so the statement
      *> breaks the rule; the message calls the first a literal-1 and
      *> the second an identifier-5, never a data item a literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG5141.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X  PIC X(6) VALUE "ABCDAB".
       01  WS-T1 PIC X VALUE "0".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-X REPLACING ALL "AB" BY WS-T1.
           DISPLAY WS-X.
           STOP RUN.
