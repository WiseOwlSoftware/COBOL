# PB5230 review round 3 (E:/COBOL-coord/scratch/review-build-stage2-drift.jsonl): one finding, fixed at the scanner

**R3-1 (medium): a verbatim literal ending in an escaped quote hid code.** `_raw_spans` paired any run of three quotes as
a C# raw string literal, but a verbatim literal ending in an escaped quote (`@"x"""`; `DeclinedFacilityOperandDriftTests.cs:93`,
`ExternalizedNameFormationDriftTests.cs:45`, `ExactWideSettlementDriftTests.cs:152` and more) also holds three quotes. Two
such literals made one "raw literal", so `_context` read every line of code between them as string text, and both
`_root_use` (a bare `TestRepo.Root`) and `bypasses_in` (`AppContext.BaseDirectory`, a literal handed to System.IO) dropped
what they should refuse. The reviewer's plant (`PlantedVerbatim.cs`) passed silently beside a control that was RED.

**Root and fix.** The audit had two partial readers of C# string syntax: a whole-file regex for raw literals and a
per-line scanner for the rest, each blind to what the other followed. Both are replaced by ONE lexer of the whole file,
`buildgraph._lex` (cached per source), which partitions the text into code, comment, string and hole runs; `_context`
is a lookup in that partition. It follows regular, verbatim (`""`, over lines), interpolated (`{{`, holes holding
strings), interpolated verbatim, raw (N quotes, `$$` holes of as many braces) and character literals, and line and block
comments.

**Sweep.** The same mechanism (a literal form the scanner did not follow) had three more sites, each confirmed on the old
scanner and now an arm of the self-test: a character literal `'"'` opened a "string" that hid the rest of its line; a
`/* … */` comment read as code; the later lines of a multi-line verbatim literal read as code. And one consumer sibling:
`bypasses_in` counted only top-level code, so a bypass inside an interpolation hole (`$"{AppContext.BaseDirectory}"`, which
runs) was dropped; it now counts a hole as code (`RUNS`). That found one more line, `tests/_shared/TestRepo.cs:156`, the
locator's refusal message naming its start directory, judged in `scripts/build_targets.json` with its reason.

**Proof.** A differential of `reads_in` and `bypasses_in` over all 1,115 tracked `tests/**/*.cs` sources, old scanner
against new: no read changed; one bypass added (TestRepo.cs:156 above). `buildgraph.py --self-test`: PASS, 33 arms, the two
new ones (the reviewer's scenario, and each literal form) failing on the old scanner. `buildgraph.py check`: GREEN.
