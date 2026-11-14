- **D-CE1 — where a FREE-form comment-entry ends** (kb/Work PB3245, review finding R1-1; PB1494, PB1758). The
  comment-entry paragraphs (AUTHOR, INSTALLATION, DATE-WRITTEN, DATE-COMPILED, SECURITY) are COBOL-85's,
  removed by COBOL-2002, the edition that introduced free form, and REMARKS is in no held edition (§1985:VI.3.2.1.1,
  kb/Work PB5026), so no edition of the standard says where a free-form comment-entry ends: it is accepted only at
  `--std 85 --source-format free` (WiseOwl's free form at 85; REMARKS not even there) and under `--permissive`. **Determination (CLAUDE.md rule 1: the standard is silent, so GnuCOBOL decides):** GnuCOBOL 3.2's
  `-free` reading. Inside an IDENTIFICATION DIVISION, a line whose FIRST word is one of those paragraph words opens the
  paragraph, its period optional, and the entry is the rest of that line; the next line is program text again. GnuCOBOL
  3.2 measured: `AUTHOR. Pat O'Brien.`, `AUTHOR Pat O'Brien.` and `SECURITY. J. Smith.` are accepted, the paragraph
  word part-way along a line is not a header, and a second line of entry text is refused. **Mechanism:** the §2023:6.5 logical
  conversion's free-form classifier (`ReferenceFormatProcessor.FreeFormConverter`) writes the header line as `AUTHOR. .`
  and discards the entry, exactly as the fixed-form classifier does with a COBOL-85 entry that runs to the next Area-A
  word, so no comment-entry character reaches the lexer in either format: an apostrophe in a name is no unclosed literal
  (COBOL5563), and a comma with no space after it is no misplaced separator (COBOL2631). Which division a line stands in
  is the one `DivisionCursor` rule, carried from each SOURCE FORMAT segment to the next in either format. Before this
  determination, free-form entry text ran to the parser's first period token: entries with a period inside or an
  unbalanced quotation mark were refused, and an entry continued on further lines was accepted. Pinned by
  `conformance:wiseowl/cases/6.5-Logical-conversion/pb3245_free_comment_entry_is_commentary`,
  `conformance:wiseowl/cases/6.5-Logical-conversion/pb3245-free-comment-entry-ends-at-line-end`,
  `conformance:cases/6.5-Logical-conversion/pb3245_free_segment_hands_on_its_division` and
  `unit:ReferenceFormatLogicalConversionTests.FreeFormCommentEntry_IsTheRestOfItsHeaderLine_AndIsDiscarded`.
  **Fixed form is not that determination** (kb/Work PB5168, PB5169): in COBOL-85's own reference format the header is "A reserved
  word, followed by the separator period" (§1985:III, cite-checked), the format prints each paragraph's period and gives it "the
  status of a required word" (§1985:IV.2.1.6), so a fixed-form header with no separator period after its word is COBOL7166 at
  the word (the paragraph is still written `AUTHOR. .`, so the parse recovers), and "The continuation of the comment-entry by the
  use of the hyphen in the indicator area is not permitted" (§1985:VI.3.2.1.2 SR1, and §1985:VI.3.4.3 SR1 for DATE-COMPILED), so a
  line of the entry with a hyphen in the indicator area is COBOL7165 and is still discarded with the entry; the entry may take
  further lines instead. Both are asked in the one converter (`CommentEntryHeader`, `CommentEntryOf`) and, for a candidate
  decided late (D-CE2), in `ConvertCommentEntryCandidate`. Free form has no indicator area and keeps D-CE1's optional period.
