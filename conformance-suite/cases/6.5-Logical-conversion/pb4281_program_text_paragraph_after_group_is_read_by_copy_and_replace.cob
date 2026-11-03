      *>--- conformance-case
      *> id: pb4281_program_text_paragraph_after_group_is_read_by_copy_and_replace
      *> rules: [GR-6.5-5]
      *> accept: [2002, 2014, 2023]
      *> files: [PB4281RB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.5: the logical conversion happens "before the
      *> application of replacing and conditional compilation actions",
      *> so it cannot tell which branch of the >>EVALUATE below is
      *> compiled and carries the paragraph REMARKS after it undecided
      *> (kb/Work PB4281, docs/CONFORMANCE.md D-CE2). The compiled branch
      *> opens the PROCEDURE DIVISION, so REMARKS is a paragraph of the
      *> program, and the program text of a paragraph is subject to the
      *> text-manipulation statements like any other: 7.2.4.4 2)
      *> "Pseudo-text-1 specifies the text to be replaced by
      *> pseudo-text-2" applies to the DISPLAY statement written in
      *> REMARKS, which therefore shows 7, and 7.2.3.4 7) "the library
      *> text is included in the resultant text unchanged" applies to
      *> the COPY statement in it, whose DISPLAY is replaced the same way.
      *> Leg that can fail: the paragraph decided after the REPLACE and
      *> COPY stages without a second run of them (the first DISPLAY
      *> would show the item's value 1, and the COPY statement would
      *> reach the parser unexpanded).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4281R.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 PB4281-LIMIT PIC 9 VALUE 1.
000600 >>DEFINE PB4281-Y AS 1
000700 >>EVALUATE PB4281-Y
000800 >>WHEN 1
000900 PROCEDURE DIVISION.
001000 REPLACE ==PB4281-LIMIT== BY ==7==.
001100 MAIN-PARA.
001200     PERFORM REMARKS.
001300     STOP RUN.
001400 >>WHEN OTHER
001500 ID DIVISION.
001600 >>END-EVALUATE
001700 REMARKS.
001800     DISPLAY PB4281-LIMIT.
001900     COPY PB4281RB.
