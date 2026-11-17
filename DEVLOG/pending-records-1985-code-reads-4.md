## Records branch 4 of the 1985 code reads: read pass 17 on record, three rulings recorded, and the last inherited 1985 row closes on a test (1985 reads 0 GAP)

A records branch (kb/Work PB5819, PB5849, PB5683): it records decisions already made and changes no compiler or
runtime source.

**Imports.** Read pass 17, slices `1985-237` to `1985-242` at pin `946daf5a4d`, `--replace`: 34 reads, 23 confirmed,
2 contradicted, 9 undecided (32 replaced an earlier record). Its top-up, slice `1985-243` at pin `3aea938236`: 4 reads,
3 confirmed, 1 undecided (4 replaced). Slice `1985-244` (the five rows pull requests 1835 and 1836 fixed) at pin
`1554cc5deb`: 5 reads, 4 confirmed, 1 undecided (5 replaced); its one ruling (`kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-18.md`,
tracked here unchanged) rejects the doubt on `1985-SR-XIII.3.7.3-2` and adjudicates it CONFORMS on kb/Work PB5866's
reading. Each set was dry-run on a copy of the store first; no slice was refused.

**Rulings of read pass 17** (`kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-17.md` and `-17b.md`, 12 rows): `work`
named on 2 contradicted records (PB5881, PB5885), PARTIAL adjudicated with its `work` on 8 undecided ones (PB5883,
PB5884 twice, PB5886 four times, PB5888), and the two rejected doubts adjudicated CONFORMS (`1985-GR-VII.2.8.4-3`, the
method is the implementor's and status 71 is PB5882's; `1985-SR-VII.2.3.3-3`, a symbolic-character word in ASSIGN is the
file's name). No 2023 row was re-recorded PARTIAL in this branch: the fixes of those notes were reviewed and gating in
wave 20262097 while it ran, and each fix branch sets its own rows right.

**Two adjudications from rulings of their own.** `1985-GR-VII.4.7.4-13` CONFORMS (R13: the 1985 text does not say where
the page stands after a CLOSE; GnuCOBOL's CLOSE ends the line a WRITE AFTER left open, as the compiler's does).
`1985-SR-VI.5.14.3-2` CONFORMS (R49: "the same usage" is the manner of representation, so COMPUTATIONAL under BINARY is
one usage; GnuCOBOL gives both words one usage).

**Cases, each expected output derived from the rule or the determination its comment names, never from a run.**
- `pb5819_record_sizes_kept_after_extend_85` closes the last inherited 1985 row, `1985-GR-XIII.4.5.4-18` (the ruling
  `kb/Work/PB5819/2026-11-16-the-row-takes-a-test.md`): a report file created under RECORD CONTAINS 12, extended
  through the same description and read as 12-byte records shows `READ 00` three times, `READ 04` on the residue of the
  line ends and `READ 10`. Only statuses are displayed, so one output holds on every host; the runtime's stderr notice
  is not judged (the harness compares the standard output alone). A compiler that changed the sizes at the OPEN EXTEND
  prints 04 on the whole blocks. Mutating the expected line to `READ 05` made it fail.
- R49's witness `pb5849_comp_under_binary_group_85` (`+0001`) and its control `pb5849_packed_under_binary_group_refused_85`
  (COBOL1927).
- A17 mechanism 5: `pb5683_assign_symbolic_character_word_85` (`RAN`).
- R13's witness `pb5683_print_close_ends_the_line_85`, in the form the tree already uses for print streams, a declared
  output file (as the kb/Work PB5850 report cases and `g85_z07`): an empty first line, AA, BB, an empty line, CC.
  Mutating its expected file to drop the empty line made it fail.
- The four register entries owed a witness: `pb5800_line_counter_contained_report_85` and
  `pb5800_line_counter_own_report_over_global_85` (D-85-COUNTER-QUAL), `pb5824_terminate_break_at_final_85`
  (D-85-TERMINATE-BREAK), `pb5870_file_status_group_item_85` (D-85-GROUP-CATEGORY),
  `pb5870_renames_same_word_two_items_85` (D-85-RENAMES-SAME-NAME), each from the probe its ruling ran.
The eight rows' own-edition witnesses were recorded with `record_verdicts.py --edition 1985` (8 rows, +10 test-ref,
GAP 1 to 0).

**Register.** DOC-A.1-129 and DOC-A.1-159 say they hold at every edition and name the 1985 rules (`§1985:XIII.4.5.4 GR6`
with `§1985:VII.4.3.4 GR8`, `§1985:VIII.4.4.4 GR8`, `§1985:IX.4.4.4 GR8`; `§1985:XIII.3.2.4 GR2`). DOC-A.1-208 gains R49's
sentence (IBM is not cited: its Language Reference was not read). DOC-A.1-71 gains A17's sentence on a symbolic-character
word; its constant-name sentence is a different case and is unchanged. New determination D-PRINT-CLOSE-ENDS-THE-LINE.

**Notes.** PB5819 and PB5849 landed. The two refuter records of the row are kept as history in
`tests/version-matrix/derivation-refutations/` (`pb5822-1985-kept-5.jsonl`, `-6.jsonl`). PB5829's lead about the
sequential sibling stays open, with the measure the ruling gives.

**Summary, before and after:**
- before: `1985 2274 rules · 1 GAP (1 inherited, no own-edition witness)`; code reads 1308 confirmed · 43 contradicted ·
  1 undecided · 0 stale · 4 KEPT rows owed a read.
- after: `1985 2274 rules · 0 GAP (v1.0 = zero GAP)`; code reads 1343 confirmed · 11 contradicted · 0 undecided · 0
  stale, no re-read owed · 1 KEPT row owed a read (`1985-GR-XIII.4.5.4-18`, now witnessed by its own test). 2023 (4 GAP) and 2002 (1183
  GAP) are unchanged.
