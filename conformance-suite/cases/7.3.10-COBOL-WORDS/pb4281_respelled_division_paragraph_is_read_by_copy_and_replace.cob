      *>--- conformance-case
      *> id: pb4281_respelled_division_paragraph_is_read_by_copy_and_replace
      *> rules: [GR-7.3.10.4-2, GR-7.2.4.4-2]
      *> accept: [2023]
      *> files: [PB4281RC.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 7.3.10.4 2): DIV, made a synonym for DIVISION by
      *> the directive, "may be used in any syntax requiring the use of"
      *> DIVISION, so the header PROCEDURE DIV. leaves the
      *> IDENTIFICATION DIVISION and REMARKS after it is an ordinary
      *> paragraph of the program. The logical conversion reads the words
      *> as written (6.5: "before the application of replacing and
      *> conditional compilation actions"), cannot tell DIV from a user
      *> word, and carries the paragraph undecided past the stages that
      *> follow (kb/Work PB4281, docs/CONFORMANCE.md D-CE2); once it is
      *> decided to be program text those stages read it: 7.2.4.4 2)
      *> "Pseudo-text-1 specifies the text to be replaced by
      *> pseudo-text-2" replaces the item's name in the DISPLAY statement
      *> written in the paragraph and in the library text of its COPY
      *> statement (7.2.3.4 7): "the library text is included in the
      *> resultant text unchanged"), so both show 7.
      *> Leg that can fail: the paragraph converted after the REPLACE and
      *> COPY stages without a second run of them: the first statement
      *> would show 1 and the COPY statement would reach the parser
      *> unexpanded.
000100 >>COBOL-WORDS EQUATE "DIVISION" WITH "DIV"
000200 IDENTIFICATION DIV.
000300 PROGRAM-ID. PB4281F.
000400 DATA DIV.
000500 WORKING-STORAGE SECTION.
000600 01 PB4281-LIMIT PIC 9 VALUE 1.
000700 PROCEDURE DIV.
000800 REPLACE ==PB4281-LIMIT== BY ==7==.
000900 MAIN-PARA.
001000     PERFORM REMARKS.
001100     STOP RUN.
001200 REMARKS.
001300     DISPLAY PB4281-LIMIT.
001400     COPY PB4281RC.
