      *>--- conformance-case
      *> id: pb4298-floating-debugging-on-fixed-indicator
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2002 6.1.2.1 SR8: "A floating debugging indicator
      *> shall not be specified on a line that contains a fixed debugging
      *> indicator or a fixed continuation indicator" (kb/Work PB4298).
      *> Line 000500 holds the fixed D in column 7 and >>D after it:
      *> COBOL5770 from 2002 (a 2002 rule: COBOL-85 reads the text as a
      *> comment, pb4298_floating_debugging_text_on_d_line_85).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4298SR8.
000300 PROCEDURE DIVISION.
000400     DISPLAY "A".
000500D    >>D DISPLAY "DEBUG".
000600     STOP RUN.
