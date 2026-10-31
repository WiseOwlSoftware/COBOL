      *>--- conformance-case
      *> id: pb4241_continued_division_header
      *> rules: [GR-6.5-6]
      *> accept: [85, 2002, 2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.5 6) b) (kb/Work PB4241): "otherwise, the
      *> content of the program-text area, beginning with the first
      *> non-space character, is appended immediately to the right of the
      *> last character in the latest logical line of the resultant
      *> compilation group." The header PROCEDURE DIVI / -SION. is ONE
      *> logical line, PROCEDURE DIVISION., so it leaves the
      *> IDENTIFICATION DIVISION and the paragraph named REMARKS after it
      *> is an ordinary paragraph, not the obsolete comment-entry
      *> paragraph (whose body would be discarded, and the PERFORM below
      *> would find no paragraph). The division a text stands in is read
      *> from the logical lines, never from half of a continued word.
      *> Leg that can fail: REMARKS read as a comment-entry paragraph
      *> draws a syntax error on the statement after the header.
      *> COBOL-2023 removed the continuation of a word in fixed form
      *> (Annex E.2), so the program is accepted from 85 through 2014.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241CH.
000300 PROCEDURE DIVI
000400-    SION.
000500 MAIN-PARA.
000600     PERFORM REMARKS.
000700     STOP RUN.
000800 REMARKS.
000900     DISPLAY "IN-REMARKS".
