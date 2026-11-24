      *>--- conformance-case
      *> id: pb5701-directive-on-debugging-line
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2002 7.2.2 SR8: a compiler directive may be
      *> specified anywhere in a compilation group except d) on a
      *> debugging line (kb/Work PB5701). Line 000500 is a debugging
      *> line that holds >>DEFINE: COBOL8092 at 2002. The 2014 and 2023
      *> texts have no such rule.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB5701D.
000300 PROCEDURE DIVISION.
000400     DISPLAY "A".
000500D>>DEFINE VV AS 1
000600     STOP RUN.
