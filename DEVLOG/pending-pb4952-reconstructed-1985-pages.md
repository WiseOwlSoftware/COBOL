## PB4952: the 1985 transcription reconstructs the four printed pages the scan lacks, and marks every rule it reads there provisional

The scan of ANSI X3.23-1985 lacks folios XIII-30, XIII-31, XIII-54 and XIII-55, and the owner chose on 2026-11-09 to
fill the three gaps with reconstructed placeholders until ANSI supplies the pages. This landing does that, end to end.

**Re-probe.** On the base (bd0a0f442b) `extract_rule_catalog.py --edition 1985 --check` reported no finding with 2,255
rows: the gaps were invisible to it except as the `GAP_RESUMES` entry that cited TYPE syntax rules 5 to 7 by the printed
page (`§1985:p.XIII-56`). Paragraph 3.10.7 and TYPE syntax rules 1 to 4 had no row at all.

**The transcription.** Each gap now holds editorial text between a `> ⚠ **RECONSTRUCTED — not printed in our copy
(folios ...).**` note, which names its sources and its confidence, and a `> ⚠ **END RECONSTRUCTED (...).**` note:
paragraph 3.10.7 (page heading group presentation rules and Table 2, from the sibling paragraphs 3.10.6, 3.10.9 and
3.10.10 and §2023:13.18.57.4 GR7 and GR8; low confidence), the SUM clause's general rule 9 items b and c and rules 10
and 11 (from §1985:XIII.3.19.4 GR7, §1985:XIII.3.20.4 GR10 and §2023:13.18.54.4 GR2 and GR7; low), and the TYPE clause's
heading, function, general format (the TYPE clause of §1985:XIII.3.9.2 Format 1) and syntax rules 1 to 4
(§2023:13.18.57.3 SR9, SR10, SR12, SR13 and SR14 without the later additions; high in substance). The Preface says
what a reconstruction is. Every citation the boxes carry was run through `cite.py --check`.

**One reader per job.** `cite.RECONSTRUCTED` and `RECONSTRUCTED_END` are the notes' one definition, with
`reconstructions` and `reconstructed` their one reader. The 1985 catalog reads a reconstruction as text, so its headings
open their blocks and a rule continues across it, and each row carries `provisional` (`none`, `part`, `whole`) and the
folio it reconstructs: 12 rows, SUM GR9 `part`. TYPE syntax rules 5 to 7 are `§1985:XIII.3.20.3 SR5` to `SR7` now.
`GAP_RESUMES`, the `unattributed` finding and `citation.SCAN_GAP_85` are deleted, with the scan-gap excuse of
`_orphan_paragraphs_85`. The mark travels: `inventory_view.py --check` counts the provisional rows, the 1985 audit's
requests quote and prompt them as provisional, `cite.py --check` marks a quotation it verifies inside a reconstruction,
and `sweep_figures` draws no format from one.

**The drift check.** `verify_publishable.check_reconstructions` (the SPEC CORRECTIONS 1985 audit) holds each
reconstruction closed and sourced, and to exactly the folios the printed folios skip where it stands: a gap nobody filled
is refused, and so is a reconstruction left beside its transcribed printed page. Taking a page's folio as the last
folio-shaped line of its page keeps the Index's continuation lines (`      VI-73`) out of the sequence.

**Found on the way (I9).** Page 894 carried `<invoking-skill>` and `</invoking-skill>` around the RANDOM function, left by
the PB4816 assembly; two catalog rows (1989-RV-A.2.34.4-1, 1989-RV-A.2.35.4-3) had read them as rule text. The tags are
gone, the rows were re-recorded against their clean text, and `verify_publishable.HARNESS_TAG` now refuses a
hyphenated or underscored tag in any transcription.

**Inventory.** Batch `pb4952-reconstructions` records the new rows' change records (TYPE SR1-4 KEPT into SR9, SR10,
SR12 and SR13-14; the TYPE format CHANGED into FMT-13.18.57.2; 3.10.7 GR1-4 and SUM GR10-11 KEPT; SUM GR9 keeps #1413's CHANGED adjudication, which rests on the printed item a)
and re-records the three rows whose text changed. 1985 at this head: 2,266 rules, 1,035 GAP (the CHANGED format is one
new GAP), 12 provisional.
`closes_rows: []`: the rows are new.

**Self-review.** Architecture: one definition of the notes, one reader, every consumer through it; no second gap
mechanism left. Code: the reconstructed rules follow the cited sources, and the self-tests plant a rule continuing into a
reconstruction, a reconstructed heading followed by printed rules, an unclosed and an unsourced reconstruction, a gap
with none, and a transcribed page beside one. Performance: `in_reconstruction` walks back with a substring test first.
Duplication: the four readers share `cite.reconstructed`.

**Also in this landing (register only).** The merge with #1413 conflicted on SUM GR9's record: #1413's CHANGED
adjudication stays, with this branch's digest and the provisional marker (review round 3 CLEAN). PB5031: SET GR2
(page 360, VI-128) was read on the rendered page; it prints `index-name-1`, as transcribed. The owner overruled the
Errata's keep-as-printed policy: the transcription is the most accurate version of the standard, so evident misprints and
a later revision's corrections are applied with attribution; PB5045 (filed here) owns that correction sweep.

**Gate:** see the branch's gate record.
