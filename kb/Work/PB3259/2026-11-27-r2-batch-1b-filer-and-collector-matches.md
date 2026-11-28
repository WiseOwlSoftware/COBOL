## R2 batch 1b (pin 9e65164eb): the filer and the collector matched upheld findings to notes of other mechanisms, and merged unlike findings

`file_census_notes.py --r2` over batch 1b's collected.json repeated the 1a defect, and the collector's own "already tracked" bucket held more of it. Each item below was checked against the matched note's title and sites.

Filer matches that would have dropped or mis-filed a finding:
- `oo--architecture--f2#1` (the SR5 presented-prototype choice is written by a validation entry) was merged into PB3289 (TierCIsland, the imageless-group boundary decided at emit time). The two share no site or member. Filed as PB6252.
- `parser-tree-4--duplication--f1#1` (the whole-group parse pass written twice with different error strategies) matched PB2735, which is landed and covers the token-stream preparation sequence. Filed as PB6253, with `parser-tree-4--architecture--f1#3` merged in.

Collector "already tracked" findings whose named note is another mechanism, filed as notes:
- `oo--duplication--f1#1` (the INHERITS closures walked by hand) named PB5580 ("2002 requires the OO optional items"), which has no site. Filed as PB6249.
- `parser-tree-3--architecture--f2#3` (pre-parse screens report as forged ANTLR errors) named PB4314 (the MaxErrors cap). Filed as PB6254.
- `parser-tree-3--code--f1#1` (boolExprAhead's hand-written verb list) named PB4244, which is retired on a behavior ruling. Filed as PB6250.
- `runtime-values-4--performance--f2#1` (splice kernels copy the image twice) named PB2088 (StorageCell.Ref cost by area width). Filed as PB6251.

Collector "already tracked" findings that do belong to their note, recorded as finding files beside it: PB2333, PB2245 (two findings), PB4243, PB2407, PB2246 and PB2382. `parser-tree-3--architecture--f2#1` is the same mechanism as the upheld `parser-tree-3--duplication--f2#2` (PB6242) and was merged there; its finder had named R56, an owner decision and not the mechanism.

Merges by the collector's "same wave kind on overlapping members" rule that join two mechanisms, filed apart: `oo--duplication--f1#2` (two placement tables for one screen, PB6239) from `oo--duplication--f2#1` (three METHOD-ID constructors, PB6238); `oo--architecture--f2#1` (PB6252) from `oo--architecture--f1#2` and `f2#2` (the OoClassTable god class, PB6234). The merge of `runtime-values-4--architecture--f1#1` into `runtime-values-4--duplication--f1#1` (OverlappingMove's over-punch tables, PB6248) is correct.

Further evidence for PB3259's Target: a match, by finder or filer, on site or member overlap with no test of rule and target.
