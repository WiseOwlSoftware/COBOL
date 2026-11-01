      *>--- conformance-case
      *> id: pb4298-floating-debugging-below-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The floating debugging indicator >>D is introduced with the
      *> floating indicators at COBOL-2002 (ISO/IEC 1989:2002 6.1.2; kb/
      *> Work PB4298): COBOL-85 has only the fixed D. At 85 the line is
      *> refused with the introduction gate COBOL0900, not the parser's
      *> "unexpected '>'". Accepted at 2002 (pb4298_floating_debugging_
      *> fixed), removed at 2014 (pb4298-floating-debugging-at-2014).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4298B85.
000300 PROCEDURE DIVISION.
000400     DISPLAY "A".
000500     >>D DISPLAY "DEBUG".
000600     STOP RUN.
