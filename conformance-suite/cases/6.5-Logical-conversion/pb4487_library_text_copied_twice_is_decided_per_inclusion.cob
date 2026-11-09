      *>--- conformance-case
      *> id: pb4487_library_text_copied_twice_is_decided_per_inclusion
      *> rules: [GR-6.5-5]
      *> files: [PB4487RB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.5: the logical conversion happens "before the
      *> application of replacing and conditional compilation actions",
      *> so after the REPLACE statement below, which names DIVISION, it
      *> cannot tell which division the REMARKS paragraph of library text
      *> PB4487RB stands in, and carries it undecided in each of the two
      *> texts the two COPY statements bring in (kb/Work PB4281, PB4487,
      *> docs/CONFORMANCE.md D-CE2). The first COPY stands in the
      *> IDENTIFICATION DIVISION, where REMARKS is the comment-entry
      *> paragraph and its entry is commentary. The second stands in the
      *> PROCEDURE DIVISION, where REMARKS is a paragraph of the program,
      *> and the REPLACE statement acts on its words: 7.2.4.4 2)
      *> "Pseudo-text-1 specifies the text to be replaced by
      *> pseudo-text-2".
      *> No edition has the REMARKS paragraph (1985 VI.3.2.1.1 prints
      *> none; kb/Work PB5026), so the first inclusion is refused at
      *> every edition with COBOL0902 and the program is a refusal: the
      *> per-inclusion decision it exercises is held by the unit tests
      *> CommentEntryResolverTests (PB4487 facts).
      *> Leg that can fail: the IDENTIFICATION DIVISION inclusion read
      *> as program text, so no COBOL0902 is reported.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4487B.
000300 REPLACE ==PB4487-R== BY =="R"== ==DIVISION== BY ==DIVISION==.
000400     COPY PB4487RB.
000500 DATA DIVISION.
000600 WORKING-STORAGE SECTION.
000700 01 PB4487-V PIC 9 VALUE 1.
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     DISPLAY PB4487-V.
001100     GO TO REMARKS.
001200     COPY PB4487RB.
001300     STOP RUN.
