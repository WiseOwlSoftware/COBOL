      *>--- conformance-case
      *> id: pb5701-floating-directive-on-debugging-line
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2002 7.2.2 SR8 d): the floating debugging
      *> indicator >>D makes a debugging line too, and a compiler
      *> directive on it is COBOL8092 at 2002 (kb/Work PB5701).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB5701F.
000300 PROCEDURE DIVISION.
000400     DISPLAY "A".
000500     >>D >>DEFINE VV AS 1
000600     STOP RUN.
