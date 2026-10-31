      *>--- conformance-case
      *> id: pb4241_continued_division_header_across_blank_line
      *> rules: [GR-6.5-6]
      *> accept: [85, 2002, 2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.3.6: "A blank line may be written as any line of
      *> a compilation group", and 6.5 2): a blank line is logically
      *> discarded, so the continuation line after it still appends to
      *> the latest logical line (6.5 6) b)). The header PROCEDURE DIVI /
      *> (blank line) / -SION. is ONE logical line, PROCEDURE DIVISION.,
      *> so it leaves the IDENTIFICATION DIVISION and the paragraph named
      *> REMARKS after it is an ordinary paragraph (kb/Work PB4241,
      *> review round 1, R1-1). A blank line does not complete the line
      *> before it.
      *> Leg that can fail: REMARKS read as a comment-entry paragraph
      *> draws a syntax error on the statement after the header.
      *> COBOL-2023 removed the continuation of a word in fixed form
      *> (Annex E.2), so the program is accepted from 85 through 2014.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241BL.
000300 PROCEDURE DIVI

000400-    SION.
000500 MAIN-PARA.
000600     PERFORM REMARKS.
000700     STOP RUN.
000800 REMARKS.
000900     DISPLAY "IN-REMARKS".
