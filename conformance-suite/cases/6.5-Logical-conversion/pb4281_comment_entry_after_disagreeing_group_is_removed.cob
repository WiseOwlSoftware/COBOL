      *>--- conformance-case
      *> id: pb4281_comment_entry_after_disagreeing_group_is_removed
      *> rules: [GR-6.5-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4281, docs/CONFORMANCE.md D-CE1 and D-CE2. The
      *> comment-entry paragraphs (AUTHOR here) were removed by COBOL-2002
      *> and no edition has them together with the >>IF directive, so a
      *> program that writes one is refused at 2002 and later with
      *> COBOL0902 (an error in strict mode). The question this test
      *> asks is WHERE the refusal lands: the branches of the >>IF
      *> disagree on the division (the compiled one is the
      *> IDENTIFICATION DIVISION paragraph PROGRAM-ID), so the converter
      *> cannot place the AUTHOR paragraph that follows, carries it, and
      *> the paragraph is decided on the compiled text: a comment-entry
      *> paragraph, refused as the removed paragraph it is, its entry
      *> text (an apostrophe that no token rule accepts) discarded.
      *> Leg that can fail: the paragraph read as program text draws
      *> COBOL5563 and COBOL2633 on the apostrophe instead.
000100 IDENTIFICATION DIVISION.
000200 >>DEFINE PB4281-Y AS 1
000300 >>IF PB4281-Y IS DEFINED
000400 PROGRAM-ID. PB4281N.
000500 >>ELSE
000600 PROCEDURE DIVISION.
000700 >>END-IF
000800 AUTHOR. PAT O'BRIEN.
000900 PROCEDURE DIVISION.
001000     DISPLAY "X".
001100     STOP RUN.
