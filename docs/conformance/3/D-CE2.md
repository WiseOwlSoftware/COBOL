- **D-CE2 — which division a comment-entry stands in when the logical conversion cannot know** (kb/Work PB4241, PB4281).
  §2023:6.5: "Source text and library text in fixed-form reference format are logically converted to free-form reference
  format before the application of replacing and conditional compilation actions" (cite-checked). The converter
  decides whether a line opens a COBOL-85 comment-entry paragraph (D-CE1) from the division the text stands in
  (`DivisionCursor`), and at that point it has not seen the text conditional compilation will omit, what a REPLACE or a
  `COPY … REPLACING` will write, or the synonyms a `>>COBOL-WORDS` directive will give `DIVISION`. No edition of the
  standard has both a comment-entry (removed by COBOL-2002) and a `>>IF` (introduced by COBOL-2002 together with the
  comment-entry's removal), so the standard is silent on the mixed text. **Determination (CLAUDE.md rule 1: the standard
  is silent, so GnuCOBOL decides):** the paragraph is decided on the COMPILED text. GnuCOBOL 3.2 preprocesses (COPY,
  REPLACE, `>>IF`) and only then scans the identification division's paragraphs; measured, it accepts the
  `AUTHOR. Pat O'Brien.` that follows a `>>IF` group whose compiled branch is the PROGRAM-ID paragraph, and the `REMARKS.` paragraph that follows `PD.` after `REPLACE ==PD== BY ==PROCEDURE DIVISION==.`. §2023:7.2.4.4 1) agrees
  about where replacing looks: the REPLACE statement's "source text" is "the conditionally-processed compilation group"
  (cite-checked). **Mechanism: a three-valued division and a stage after the others.** `DivisionCursor` reports the
  division the text stands in as yes, no or NOT KNOWN. The converter is exact where it can be: a certain IDENTIFICATION
  DIVISION discards the comment-entry (the header written `AUTHOR. .`), a certain other division reads the paragraph
  as program text, exactly as before. The cursor says NOT KNOWN, and the converter CARRIES the candidate instead of
  deciding it, where something that runs after the conversion could have changed what its words mean: (1) a `>>IF …
  >>END-IF` or `>>EVALUATE … >>END-EVALUATE` group whose branches do not all end in the same division (the branch no
  condition selects counted when there is no `>>ELSE` or `>>WHEN OTHER`); (2) a `>>COBOL-WORDS` directive that NAMES a
  word the cursor reads (`DIVISION`, `IDENTIFICATION`, `ID`, an identification paragraph: as the word to respell or the
  word to write, or a compilation-variable-name in a literal slot, whose value only conditional compilation knows,
  §2023:7.3.11.4 1)), which may respell it, from there on, in the text or in a library text it reads (the merged driver, whose
  blocks end at a directive line and never hold it, tells its cursor of the line itself); a directive that names none,
  `>>COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"`, changes nothing the cursor reads and leaves the division known, as an
  unrelated REPLACE does; (3) a REPLACE statement or
  a `COPY … REPLACING` that names a word the cursor reads (`DIVISION`, `IDENTIFICATION`, `ID`, an identification
  paragraph), to match it or to write it. A carried candidate is the paragraph's header line and the lines of its entry,
  each an inert slot of the logical text that every later stage reads as a comment (`CommentEntryCarrier`: the floating
  comment indicator and a Unicode noncharacter, holding the physical line), so no character of an entry reaches a scanner
  that would refuse it. `CommentEntryResolver` decides it in `Frontend.Preprocess`, AFTER conditional compilation, COPY,
  REPLACE and the `>>COBOL-WORDS` stage and before the text is lexed, on a cursor that reads the finished text through the
  group's word map (§2023:7.3.10.4 GR2: a synonym "may be used in any syntax requiring the use of" the word it stands for;
  GR4: a substituted word is a user word): in an IDENTIFICATION DIVISION the header is written `AUTHOR. .` and the entry
  discarded (D-CE1), elsewhere the lines are converted as the ordinary lines they are, through the same walker and the
  same reference-format diagnostics. `CommentEntryResolutionOrderDriftTests` keeps the order. Both directions are pinned:
  a division word in a branch no condition selects moves nothing (`>>IF NEVER IS DEFINED / IDENTIFICATION DIVISION. /
  >>END-IF` in a PROCEDURE DIVISION), a division word in the SELECTED branch is not undone, and `AUTHOR. Pat O'Brien.`
  after a group whose compiled branch is the PROGRAM-ID paragraph is a comment-entry (refused with COBOL0902 from 2002
  as the removed paragraph it is, not with COBOL5563 and COBOL2633 on the apostrophe), while `REMARKS. DISPLAY "HI".`
  after a group whose compiled branch is the PROCEDURE DIVISION, or after `PROCEDURE DIV.` under `>>COBOL-WORDS EQUATE
  "DIVISION" WITH "DIV"`, is a paragraph of the program. A `>>WHEN` whose selection object merely begins with the letters
  OTHER (`>>WHEN OTHERV`) is not the `>>WHEN OTHER` default branch (§2023:7.3.13.3 SR5/SR6: the phrase is a word, asked of
  `CompilerDirectiveLine.StartsWithWord` by this cursor and by the conditional-compilation stage alike). **The cost of
  carrying, and how it is paid:** a carried candidate that proves to be a paragraph of the program (named AUTHOR,
  INSTALLATION, DATE-WRITTEN, DATE-COMPILED, SECURITY or REMARKS in area A, after one of the three cases above) was
  hidden from exactly the stages that must read it: its COPY statements, the REPLACE statement in force over its words,
  its compiler directives. Converting it after them left a silent wrong answer (a REPLACE that did not apply) or a refusal
  (an unexpanded COPY), so the resolver does not convert it late: it NAMES the EARLIEST such candidate of the finished
  text by its physical line in its inclusion (`CommentEntryResolver.Resolution.ProgramText`, a `CommentEntryPin`), and
  `Frontend.RunTextStages` runs the text stages AGAIN with that line pinned as program text (`CommentEntryPins`, read by
  the two converters where the division is not known), to a fixed point. **One pin per run, the earliest (kb/Work
  PB4487):** reading a pinned candidate as program text changes the text AFTER it (its COPY statements bring library text
  in, its REPLACE statements and directives act on what follows) and never the text before it, so the earliest
  program-text candidate, and every comment-entry before it, was decided on text no later pin can change; a LATER
  candidate was decided with that one still hidden, and a COPY in its paragraph may bring in the IDENTIFICATION DIVISION
  the later one stands in (`REMARKS. … COPY` of a nested program's `IDENTIFICATION DIVISION. PROGRAM-ID. …`, then
  `AUTHOR. Pat O'Brien.`: a comment-entry). So each run settles one candidate and the ones after it are decided again by
  the run that reads it; a run that names none ends the iteration, which is every run of a program without a candidate
  (one search per run), and a pinned candidate is never carried again (`CommentEntryPins.With` refuses a second naming),
  so it ends. §2023:6.5 orders the walk this way: "The rules of logical conversion are applied to each line of a compilation
  group in the order that lines of source text and library text are obtained by the compiler" (cite-checked). **A pin
  names an INCLUSION, not only a line:** a library text two COPY statements bring in is two texts of the group, and one
  may stand in an IDENTIFICATION DIVISION and the other in a PROCEDURE DIVISION, so the COPY driver numbers each
  inclusion as it normalizes it (the source text is 0, then 1, 2, … in text order; `CommentEntryPin.Inclusion`, carried
  in the carrier), and the numbers up to the earliest undecided candidate are the same in every run. The pinned run
  converts the candidate as the ordinary lines they are, so every stage sees them; the free-form 255-position gate (§2023:6.1 3) a)) is asked of EVERY physical line once, by
  the converter, a carried candidate's and an omitted branch's included; the resolver never asks it again. The merged driver's COPY cursor reads the text the
  conditional-compilation stage has already cut, so a COPY's library text starts in the division the cut text says, or
  NOT KNOWN. Pinned by
  `conformance:cases/6.5-Logical-conversion/pb4241_omitted_branch_division_header`,
  `conformance:cases/6.5-Logical-conversion/pb4241_selected_branch_division_header`,
  `conformance:cases/6.5-Logical-conversion/pb4241_when_other_word_is_not_the_default`,
  `conformance:cases/6.5-Logical-conversion/pb4241_continued_division_header`,
  `conformance:cases/6.5-Logical-conversion/pb4241_continued_division_header_across_blank_line`,
  `conformance:cases/6.5-Logical-conversion/pb4241_copy_operand_names_division`,
  `conformance:cases/6.5-Logical-conversion/pb4281_disagreeing_branches_paragraph_after_group`,
  `conformance:cases/6.5-Logical-conversion/pb4281_comment_entry_after_disagreeing_group_is_removed`,
  `conformance:cases/6.5-Logical-conversion/pb4281_program_text_paragraph_after_group_is_read_by_copy_and_replace`,
  `conformance:cases/7.3.10-COBOL-WORDS/pb4281_respelled_division_is_not_a_comment_entry`,
  `conformance:cases/7.3.10-COBOL-WORDS/pb4281_respelled_division_paragraph_is_read_by_copy_and_replace`,
  `conformance:cases/7.3.10-COBOL-WORDS/pb4281_unrelated_cobol_words_directive_keeps_remarks_paragraph_in_the_stages`,
  `conformance:cases/6.5-Logical-conversion/pb4487_paragraph_a_program_text_candidate_copies_in_is_a_comment_entry`,
  `conformance:cases/6.5-Logical-conversion/pb4487_library_text_copied_twice_is_decided_per_inclusion`,
  `unit:DivisionCursorTests`, `unit:CommentEntryResolverTests` and `unit:CommentEntryResolutionOrderDriftTests`.
