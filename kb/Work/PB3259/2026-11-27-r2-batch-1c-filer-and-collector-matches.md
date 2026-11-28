## R2 batch 1c (pin 9e65164eb): the filer and the collector matched upheld findings to notes of other mechanisms, and merged unlike findings

`file_census_notes.py --r2` over batch 1c's collected.json repeated the 1a and 1b defect. Each match below was checked against the matched note's title and sites. The 28 notes PB6255 to PB6282 were filed by running the filer over an adjusted copy of collected.json (the unlike merges split, the four wrongly "already tracked" findings moved to upheld) with an empty notes directory, so no match could swallow a finding; the site blocks were then rewritten by hand to the form `work.py check` reads (PB4080).

Filer matches that would have dropped or mis-filed a finding (all nine are other mechanisms; five name a landed note):
- `codegen-1--architecture--f1#1` and `codegen-1--duplication--f1#2` (the VALUE recipe written once per storage lane) were merged into PB3236 (store a numeric carrier value into a receiver, decided at 14 verb-emitter sites). The two share no mechanism; PB3236 only lists ValueInitializer.InitializerFrom among its members. Filed as PB6255.
- `codegen-1--performance--f1#1` (GroupImageCodec.VarParts recomputed per nesting level) matched PB2746, landed, which is the runtime StorageCell twin of the variable-length group carrier. Filed as PB6263.
- `codegen-2--code--f2#1` (COMBINED-DATETIME hands a NumX.U argument raw to an Int128 parameter) matched PB2631, landed, a NUMVAL-C grouping-separator defect. Filed as PB6266.
- `codegen-2--duplication--f2#2` (one function-by-carrier table written as seven switches) matched PB2627 and PB2637, both landed, which are the IntrinsicSig.RuntimeMethod key and the format-literal re-tokenizing. Filed as PB6267.
- `codegen-2--architecture--f2#1`, `codegen-2--duplication--f2#1` and `codegen-2--performance--f2#1` matched PB3215, landed, the collation-carrier field names. Filed as PB6257 (the lane decided twice) and PB6268 (RenderNum re-renders every argument per lane predicate).
- `codegen-4--architecture--f1#2` (three Place dispatches default silently) merged into PB6230 (item identification decided by two mechanisms). Filed as PB6269.
- `reference-resolution-2--architecture--f1#1` and `f2#1` (SymbolTable is a god class with no row in the design) matched PB6234 and PB6252, the OoClassTable god class and the SR5 prototype choice. Filed as PB6258.

Collector "already tracked" findings whose named note is another mechanism, filed as notes:
- `codegen-1--architecture--f1#3` (PhysicalModel carries each field initializer text and wires a four-type cycle) named PB936, the per-instance memo of nine DataEmitter constructions. Filed as PB6279.
- `codegen-1--modern-csharp--f1#2` (CA1806 in EditedImageOfNumericValue) named PB2395, the move of the value facts out of code generation. Filed as PB6280.
- `codegen-1--modern-csharp--f1#4` (CA1822, five members that read no state) named PB2290, the deletion of the CSharpEmitter shim, which covers one of the five. Filed as PB6281.
- `codegen-2--modern-csharp--f1#1` (CA1305 in emitted C#) named PB3239, landed, whose three sites are in other files. Filed as PB6282.

Collector "already tracked" findings that do belong to their note, recorded as finding files beside it: PB936 (`codegen-1--duplication--f1#4`), PB2334 (`codegen-1--architecture--f2#2`), PB4869 (`codegen-2--architecture--f1#2`), PB2241 (`codegen-4--architecture--f1#1`), PB2320 (`reference-resolution-2--architecture--f1#2`) and PB2418 (`reference-resolution-2--architecture--f2#4`).

Merges by the collector's "same wave kind on overlapping members" rule that join two mechanisms, filed apart: `codegen-1--code--f1#2` (an INDEX member of a Tier-B group seeded with a long, PB6259) from `codegen-1--duplication--f1#1` (the image lane's own numeric-edited VALUE arm, PB6261), two defects with one site in common; `codegen-2--performance--f2#1` (PB6268) from the two lane-decision findings (PB6257). The merges of `codegen-2--code--f1#1` into `codegen-2--duplication--f1#1` (the ZERO relation, PB6256) and of the two SymbolTable findings (PB6258) are correct.

The pin predates PB2337's split of GroupImageCodec (CarrierInitOfOne and ImageInitOf now sit in ImageInitializer): the filer's `stale-path` check names a path missing from the committed tree and cannot see a member that moved to another file, so five notes carry a "Tree since the pin" line and name the current members.

Further evidence for PB3259's Target: a match, by finder or filer, on site or member overlap with no test of rule and target; and a stale-site check that tests files, not members.
