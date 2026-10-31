      *>--- conformance-case
      *> id: pb4241_selected_branch_division_header
      *> rules: [GR-6.5-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.5: "Source text and library text in fixed-form
      *> reference format are logically converted to free-form reference
      *> format before the application of replacing and conditional
      *> compilation actions." The conversion cannot know which branch of
      *> a >>IF the conditional compilation stage will select, so a
      *> division header written in a branch is no evidence of where the
      *> text after the directive stands, in EITHER direction (kb/Work
      *> PB4241, review round 1, R1-3): here the SELECTED branch (the
      *> variable is defined) leaves the IDENTIFICATION DIVISION, and the
      *> paragraph named REMARKS after >>END-IF is an ordinary paragraph
      *> of the PROCEDURE DIVISION, not the obsolete comment-entry
      *> paragraph.
      *> Leg that can fail: REMARKS read as a comment-entry paragraph
      *> draws a syntax error on the statement after its header.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241SB.
000300 >>DEFINE PB4241-SEL AS 1
000400 >>IF PB4241-SEL IS DEFINED
000500 PROCEDURE DIVISION.
000600 MAIN-PARA.
000700     PERFORM REMARKS.
000800     STOP RUN.
000900 >>END-IF
001000 REMARKS.
001100     DISPLAY "IN-REMARKS".
