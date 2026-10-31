      *>--- conformance-case
      *> id: pb4241_when_other_word_is_not_the_default_at_85
      *> rules: [GR-6.5-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4241 - the refusal below
      *> pb4241_when_other_word_is_not_the_default: the >>DEFINE and
      *> >>EVALUATE directives are COBOL-2002 introductions (7.3.11,
      *> 7.3.13), so at --std 85 the same program is refused with
      *> COBOL0900 (and nothing else is wrong with it: the REMARKS
      *> paragraph is read as an ordinary one).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241W85.
000300 >>DEFINE OTHERV AS 2
000400 PROCEDURE DIVISION.
000500 MAIN-PARA.
000600     PERFORM REMARKS.
000700     STOP RUN.
000800 >>EVALUATE 1
000900 >>WHEN OTHERV
001000 IDENTIFICATION DIVISION.
001100 >>END-EVALUATE
001200 REMARKS.
001300     DISPLAY "IN-REMARKS".
