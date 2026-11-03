      *>--- conformance-case
      *> id: pb4487_paragraph_a_program_text_candidate_copies_in_is_a_comment_entry
      *> rules: [GR-6.5-5]
      *> accept: [85]
      *> files: [PB4487IB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.5: the logical conversion happens "before the
      *> application of replacing and conditional compilation actions",
      *> so after the REPLACE statement below, which names DIVISION, it
      *> cannot tell which division a comment-entry paragraph word stands
      *> in, and carries both REMARKS and AUTHOR undecided (kb/Work PB4281,
      *> PB4487, docs/CONFORMANCE.md D-CE2). REMARKS stands in the
      *> PROCEDURE DIVISION, so it is a paragraph of the program, and its
      *> COPY statement is processed: 7.2.3.4 7) "the library text is
      *> included in the resultant text unchanged", and that text opens
      *> the IDENTIFICATION DIVISION of the nested program PB4487I. AUTHOR
      *> therefore stands in an IDENTIFICATION DIVISION and is the
      *> COBOL-85 comment-entry paragraph: its entry, a name with an
      *> apostrophe in it, is commentary. The run shows the item's value
      *> and then the nested program's DISPLAY.
      *> Leg that can fail: AUTHOR decided on the text in which REMARKS
      *> was still hidden from COPY (still in the PROCEDURE DIVISION), and
      *> kept so once REMARKS is read: the entry reaches the stages as
      *> program text, an alphanumeric literal that is never closed.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4487A.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 PB4487-V PIC 9 VALUE 1.
000600 PROCEDURE DIVISION.
000700 REPLACE ==DIVISION== BY ==DIVISION==.
000800 MAIN-PARA.
000900     DISPLAY PB4487-V.
001000     CALL "PB4487I".
001100     GO TO REMARKS.
001200 REMARKS.
001300     STOP RUN.
001400     COPY PB4487IB.
001500 AUTHOR. PAT O'BRIEN.
001600 PROCEDURE DIVISION.
001700 INNER-PARA.
001800     DISPLAY "INNER".
001900 END PROGRAM PB4487I.
002000 END PROGRAM PB4487A.
