      *>--- conformance-case
      *> id: pb4298_floating_debugging_text_on_d_line_85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4298, review finding R1-1, as PB5466 reads it. At
      *> COBOL-85 a D in the indicator area makes a debugging line, and
      *> the floating debugging indicator with its SR8 (ISO/IEC
      *> 1989:2002 6.1.2.1: not on a line with a fixed debugging
      *> indicator) are 2002's, so no COBOL5770 is drawn: the >>D is
      *> text, not an indicator. But text is all it is: §1985:VI.7.3
      *> requires "a syntactically correct program" with the debugging
      *> lines considered as comment lines or not, and the line
      *> >>D DISPLAY "DEBUG" is no COBOL-85 source, so the program
      *> formed with it is not correct. The unit has no WITH DEBUGGING
      *> MODE, so the line is a comment line and the program parses
      *> without it: the rule is broken from the without-mode side, the
      *> direction PB5466 asks. Expected: COBOL7061 at the D line at 85
      *> (it compiled and printed A and B until PB5466). The same line
      *> from 2002 is COBOL5770 (pb4298-floating-debugging-on-fixed-
      *> indicator).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4298D85.
000300 PROCEDURE DIVISION.
       PB4305-LEAD.
000400     DISPLAY "A".
000500D    >>D DISPLAY "DEBUG".
000600     DISPLAY "B".
000700     STOP RUN.
