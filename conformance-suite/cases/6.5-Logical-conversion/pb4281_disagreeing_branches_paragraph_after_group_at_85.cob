      *>--- conformance-case
      *> id: pb4281_disagreeing_branches_paragraph_after_group_at_85
      *> rules: [GR-6.5-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4281 - the refusal below
      *> pb4281_disagreeing_branches_paragraph_after_group: the >>DEFINE
      *> and >>EVALUATE directives are COBOL-2002 introductions (7.3.11,
      *> 7.3.13), so at --std 85 the same program is refused with
      *> COBOL0900.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4281G85.
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
