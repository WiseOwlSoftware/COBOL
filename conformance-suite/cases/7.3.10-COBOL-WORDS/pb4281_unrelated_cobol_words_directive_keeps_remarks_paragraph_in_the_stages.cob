      *>--- conformance-case
      *> id: pb4281_unrelated_cobol_words_directive_keeps_remarks_paragraph_in_the_stages
      *> rules: [GR-7.3.10.4-2, GR-7.2.4.4-2]
      *> accept: [2023]
      *> files: [PB4281UB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 7.3.10.4 2): the word SHOW, made a synonym for
      *> DISPLAY by the directive, "may be used in any syntax requiring
      *> the use of" DISPLAY. The directive names no word that tells a
      *> division (DISPLAY and SHOW are statements' words), so it does not
      *> change where the PROCEDURE DIVISION begins, and the paragraph
      *> REMARKS after it is an ordinary paragraph of the program, not
      *> the COBOL-85 comment-entry paragraph (kb/Work PB4281,
      *> docs/CONFORMANCE.md D-CE2). Its text is therefore subject to the
      *> statements of text manipulation as any other: 7.2.4.4 2)
      *> "Pseudo-text-1 specifies the text to be replaced by
      *> pseudo-text-2" replaces the item's name in both DISPLAY
      *> statements, so both show 7, the one written in the paragraph
      *> through the synonym and the one in its COPY statement's library
      *> text (7.2.3.4 7): "the library text is included in the resultant
      *> text unchanged").
      *> Leg that can fail: the directive taken to make every later
      *> paragraph named REMARKS a doubtful comment-entry paragraph, which
      *> the stages that follow then do not read: the first statement
      *> would show 1 and the COPY statement would reach the parser
      *> unexpanded.
000100 >>COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"
000200 IDENTIFICATION DIVISION.
000300 PROGRAM-ID. PB4281U.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01 PB4281-LIMIT PIC 9 VALUE 1.
000700 PROCEDURE DIVISION.
000800 REPLACE ==PB4281-LIMIT== BY ==7==.
000900 MAIN-PARA.
001000     PERFORM REMARKS.
001100     STOP RUN.
001200 REMARKS.
001300     SHOW PB4281-LIMIT.
001400     COPY PB4281UB.
