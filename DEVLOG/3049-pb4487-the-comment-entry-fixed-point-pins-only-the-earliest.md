## Entry 3049 — 2026-11-03 14:15 PST — PB4487: the comment-entry fixed point pins only the earliest program-text candidate per run, and a pin names the inclusion it was proved in

**Mechanism.** PB4281 settled a COBOL-85 comment-entry candidate that the §6.5 conversion cannot place by deciding it on the finished
text. A candidate that proves to be program text is pinned, and the text stages run again (`Frontend.RunTextStages`). Each run pinned
EVERY candidate it decided was program text, and a pin was never revisited. A later candidate, though, was decided on a text in which an
earlier program-text candidate was still hidden from COPY. In the PB4281 review probe (`np1.cob` + `NPBOOK.cpy`, `--std 85`), a
procedure-division paragraph `REMARKS.` holds `COPY NPBOOK` (a nested program's `IDENTIFICATION DIVISION. PROGRAM-ID. INNER.`). Run 1
pinned both `REMARKS` and the `AUTHOR. ME COPY NOSUCHBOOK.` that follows. Run 2 expanded NPBOOK, but by then AUTHOR was pinned as
program text, so the compiler drew CBL3620 (NOSUCHBOOK), COBOL1971 and COBOL0001 where the program prints `1` / `INNER`.

**Re-probe on base 11c6abfb1.** The defect reproduced exactly as above. `np2.cob` (NPBOOK inlined) and `np4.cob` (no candidate) printed
`1` / `INNER`. After the fix, all three print `1` / `INNER`.

**Fix shape.** The design question the note raised was settled against D-CE2, and the determination was corrected in the same change.
(1) Order. The resolver names one candidate per run, the EARLIEST in the finished text that is program text (`Resolution.ProgramText`,
now a single `CommentEntryPin?`). Reading a pinned candidate as program text changes the text after it and never the text before it, so
that candidate, and every comment-entry before it, was decided on text no later pin can change. The candidates after it are decided again
by the next run. §6.5 walks the lines "in the order that lines of source text and library text are obtained by the compiler"
(cite-checked). Pins stay monotone, and each run settles one more candidate. `CommentEntryPins.With` throws on a second naming of a pinned
candidate, which is never carried again, so the iteration cannot loop silently. (2) Identity. The old key was the physical line, and it
could not serve a library text that two COPY statements bring in and decide differently: that inclusion stayed converted late, hidden from
REPLACE and COPY. That was a documented residue of PB4281, which this change removes. A pin is now (file, INCLUSION, line). The source text
is inclusion 0, `CopyProcessor` numbers each library text it normalizes 1, 2, … in text order, and the number travels in the carrier
(`CommentEntryCarrier` layout `H|B F|R 1|0 <inclusion>:<line>;…`). The numbers up to the earliest undecided candidate agree in every run.
A stale cref in `CopyProcessor.NormalizeCopybookMapped` that named an old overload of the normalizer was corrected (I9).

**Goldens (accepted at 85; COBOL0902 refuses the comment-entry paragraph from 2002, as it does for every such program).**
`cases/6.5-Logical-conversion/pb4487_paragraph_a_program_text_candidate_copies_in_is_a_comment_entry` (the np1 shape; output `1` /
`INNER`) and `cases/6.5-Logical-conversion/pb4487_library_text_copied_twice_is_decided_per_inclusion` (one library text, a comment-entry in
the IDENTIFICATION DIVISION and a REPLACE-read paragraph in the PROCEDURE DIVISION; output `1` / `R`). The new unit tests in
`CommentEntryResolverTests` cover four cases: only the earliest candidate is named, a pin applies to its inclusion only, two inclusions
are decided apart, and a second naming throws. `CommentEntryResolutionOrderDriftTests` pins the new loop shape.

**Rows.** None claimed or closed (`closes_rows: []` with its reason). The fix changes how the D-CE2 fixed point settles a candidate. It
changes no verdict.

**Self-review.** Architecture: one mechanism. The pin is made in `Frontend` only and read in the converters only (the drift test), and
the inclusion number has one source (`CopyProcessor._inclusions`). Code: the resolver names a header-opened candidate only, because the
converters pin headers. Performance: one more text-stage run per program-text candidate, and none for a program without one.
Duplication: the per-key aggregation in the resolver is gone.

