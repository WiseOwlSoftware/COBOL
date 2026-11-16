      *>--- conformance-case
      *> id: g2002_fi_debug_lines_off
      *> rules: [2002-SR-6.1.2.1-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The same text as g2002_fi_debug_lines_on WITHOUT DEBUGGING
      *> MODE. §2002:6.2.7 "a debugging line has the characteristics of
      *> a comment line if the WITH DEBUGGING MODE clause is not
      *> specified"; the floating continuations append "CD", "TL" and
      *> "01" lines to the >>D logical line begun on the D line
      *> (§2002:6.4 steps 4, 5 and 9), so the whole continued logical
      *> line is a debugging line and only MAIN is displayed.
      *> A continuation not joined to the debugging line (§2002:6.4
      *> step 9) leaves "CD", "TL" or "01" as stray source text that
      *> fails to compile.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FI02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WN PIC N(4) VALUE N"NATL".
       01 WB PIC 1(4) VALUE B"1001".
       PROCEDURE DIVISION.
      D    DISPLAY "D1" *> an inline comment on a fixed debugging line
      D    DISPLAY "D2" *>X. DISPLAY "BAD"
           >>D DISPLAY "F3" *> an inline comment on a >>D line
           >>D DISPLAY "F4" *>X. DISPLAY "BAD"
      D    DISPLAY "AB"-
           "CD"
      D    IF WN = N"NA"-
           "TL" DISPLAY "N-OK" ELSE DISPLAY "N-BAD" END-IF
      D    IF WB = B"10"-
           "01" DISPLAY "B-OK" ELSE DISPLAY "B-BAD" END-IF
           DISPLAY "MAIN".
           STOP RUN.
