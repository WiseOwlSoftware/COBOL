      *>--- conformance-case
      *> id: pb4281_respelled_division_is_not_a_comment_entry
      *> rules: [GR-7.3.10.4-2]
      *> accept: [2023]
      *> files: [PB4281CB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 7.3.10.4 GR2: a word that EQUATE makes a synonym
      *> "may be used in any syntax requiring the use of" the reserved
      *> word it stands for. Here DIV stands for DIVISION, so the header
      *> PROCEDURE DIV. leaves the IDENTIFICATION DIVISION, and the
      *> paragraphs named REMARKS and SECURITY after it are ordinary
      *> paragraphs of the program, not the obsolete comment-entry
      *> paragraphs of COBOL-85 (kb/Work PB4281, docs/CONFORMANCE.md
      *> D-CE2). The logical conversion reads the words as written, before
      *> the COBOL-WORDS directive respells any of them, so it cannot tell
      *> DIV from a user word; the paragraphs are decided on the finished
      *> text, through the directive's word map. The SECURITY paragraph
      *> comes from library text, converted with the same doubt and
      *> decided in place once the COPY statement is expanded (7.2.3.4 7).
      *> Leg that can fail: the division left unseen, so REMARKS read as a
      *> comment-entry paragraph (its statements dropped, or refused with
      *> a syntax error on the second period); or a carried paragraph of
      *> the library text lost.
000100 >>COBOL-WORDS EQUATE "DIVISION" WITH "DIV"
000200 IDENTIFICATION DIV.
000300 PROGRAM-ID. PB4281EQ.
000400 PROCEDURE DIV.
000500 MAIN-PARA.
000600     PERFORM REMARKS.
000700     PERFORM SECURITY.
000800     STOP RUN.
000900 REMARKS. DISPLAY "IN-REMARKS".
001000 COPY PB4281CB.
