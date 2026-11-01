      *>--- conformance-case
      *> id: pb4241_omitted_branch_division_header
      *> rules: [GR-6.5-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.5: "Source text and library text in fixed-form
      *> reference format are logically converted to free-form reference
      *> format before the application of replacing and conditional
      *> compilation actions." The conversion cannot know which branch of
      *> a >>IF the conditional compilation stage will omit, so a
      *> division header written in a branch only one path of the
      *> directive reaches is no evidence of where the text after the
      *> directive stands (kb/Work PB4241). Here the omitted branch spells
      *> IDENTIFICATION DIVISION; the text after >>END-IF is still in the
      *> PROCEDURE DIVISION, and the paragraph named REMARKS is an
      *> ordinary paragraph, not the obsolete comment-entry paragraph.
      *> Leg that can fail: REMARKS read as a comment-entry paragraph
      *> draws a syntax error on the statement after its header.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241OB.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     PERFORM REMARKS.
000600     STOP RUN.
000700 >>IF PB4241-NEVER IS DEFINED
000800 IDENTIFICATION DIVISION.
000900 >>END-IF
001000 REMARKS.
001100     DISPLAY "IN-REMARKS".
