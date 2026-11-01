      *>--- conformance-case
      *> id: pb4298_floating_debugging_text_on_d_line_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4298, review finding R1-1 - at COBOL-85 a D in the
      *> indicator area makes a debugging line, which without WITH
      *> DEBUGGING MODE is a comment line, and a comment line holds any
      *> characters. The floating debugging indicator and its SR8
      *> (ISO/IEC 1989:2002 6.1.2.1: not on a line with a fixed
      *> debugging indicator) are 2002's, so the >>D here is commentary
      *> text and the program compiles and prints A and B. The same line
      *> from 2002 is COBOL5770 (pb4298-floating-debugging-on-fixed-indicator).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4298D85.
000300 PROCEDURE DIVISION.
000400     DISPLAY "A".
000500D    >>D DISPLAY "DEBUG".
000600     DISPLAY "B".
000700     STOP RUN.
