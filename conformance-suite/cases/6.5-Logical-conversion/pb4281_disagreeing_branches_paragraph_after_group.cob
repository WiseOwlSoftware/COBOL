      *>--- conformance-case
      *> id: pb4281_disagreeing_branches_paragraph_after_group
      *> rules: [GR-6.5-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.5: "Source text and library text in fixed-form
      *> reference format are logically converted to free-form reference
      *> format before the application of replacing and conditional
      *> compilation actions." The conversion therefore cannot know which
      *> branch of a >>EVALUATE the conditional compilation stage will
      *> select, so when the branches disagree on the division (one
      *> enters the IDENTIFICATION DIVISION, the other leaves it) it does
      *> not decide whether a paragraph named AUTHOR, written after
      *> >>END-EVALUATE, is the obsolete comment-entry paragraph of
      *> COBOL-85 or a paragraph of the program: the paragraph is decided
      *> on the compiled text (kb/Work PB4281, docs/CONFORMANCE.md D-CE2).
      *> Here the selected branch (the variable is 1) is the one that
      *> opens the PROCEDURE DIVISION, so AUTHOR is an ordinary paragraph,
      *> its two lines of statements are program text, and the program
      *> displays them. The other branch spells ID DIVISION. and is
      *> omitted (7.3.13.4 4): "all lines of text-2 associated with a WHEN
      *> OTHER phrase are omitted from the resultant text".
      *> Leg that can fail: the carried paragraph (its header and its
      *> two statement lines) lost, or read as a comment-entry paragraph:
      *> the output would be empty.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4281G.
000300 >>DEFINE PB4281-X AS 1
000400 >>EVALUATE PB4281-X
000500 >>WHEN 1
000600 PROCEDURE DIVISION.
000700 MAIN-PARA.
000800     PERFORM AUTHOR.
000900     STOP RUN.
001000 >>WHEN OTHER
001100 ID DIVISION.
001200 >>END-EVALUATE
001300 AUTHOR.
001400     DISPLAY "IN-AUTHOR-ONE".
001500     DISPLAY "IN-AUTHOR-TWO".
