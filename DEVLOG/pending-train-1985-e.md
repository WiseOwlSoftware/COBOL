## 1985 train E: three fix branches in one landing; the citation canonicalizer is safe on message text

Wave 20262132's train E lands in one pull request. It carries the fix branches of groups g (`train85e-e2-cite`), f
(`train85e-e1-diag`) and i (`train85e-e4-proc`), merged into `train-1985-e` on top of train D (`train-1985-d`, 0e25c0e6c5)
and gated once. Group h (`train85e-e3-cat`: PB6022, PB3965, PB5683, PB5924) was not reviewed CLEAN when this train's gate
started, so it joins the next train. `edition_status.py --edition 1985` read `13 open work notes` on train D's head and
reads `1 code reads · 4 open work notes` on this train's head: 0 GAP and 0 prose holes. The four open notes are group h's.
The one code read is a re-read of 1985-SR-VIII.2.4.3-1, whose stored member `DataBinder.ScreenClauseOperandShape` PB2041
folded into `ScreenDataNameShape`.

**The citation canonicalizer is safe on message text (g, PB6035); PB4976 and PB6000 are closed.** The first run over C#
(train C, gate 000003) broke 61 cases and was reverted. `scripts/spec/citation_rewrite.py` now does the rewrite. It reads
a sub-item path whole. It counts the parentheses around a citation, over a C# string's whole concatenation. It refuses a
designator that a hole, a format item or a concatenation may complete. It changes a `src/` message only together with
every test literal and diagnostic expectation that quotes it, and it refuses a fragment that means two things. The audit
also refuses `SR12a2` (canonical `SR12a.2`) and reads `r<n>`. The run rewrote 7,546 designators, 214 of them in messages,
with 27 expectations. RESOLVE's baseline went from 6,021 to 1,953 entries, each with the reason it stays. PB6040 is filed
for the bare-ordinal designator.

**A signed integer-n is refused by its rule (f, PB6021, PB5530, PB2041).** `OCCURS +3 TIMES`, `PERFORM -3 TIMES`,
`RECORD CONTAINS -10` and `WRITE ADVANCING +0` are refused once, by new COBOL7737. At 85 it cites §1985:III.2 p142, and
later editions cite §2002:5.4 item 1, §2014:5.5 item 1 and §2023:5.5 SR1. Before, each was a parse error naming the
entry's closed clause list. The SEARCH ALL messages now cite through `EditionInfo.CiteRule`. The 1985-to-2002 subscript
edge became `DialectBehavior.SearchAllKeySubscriptSentences`, with VCR row 7.91. The data-name-n screen (COBOL2024) now
closes each refusal with the operand's own clause rule at 85 and 2023. Its 1985 arm quotes the data-name definition
(§1985:III.2 p69). The 2002 and 2014 arms are PB6038.

**Edition edges, witnesses and orphaned children (i, PB6031, PB6037, PB5057).** A test's program child is now bound to
its host's lifetime through a Windows kill-on-close job object (`HostBoundChildren`), so a dead test host no longer
orphans it. A C# witness of a 1985 row compiles fixed-form text or none. Four that compiled free form now compile fixed,
and `EditionWitnessFormatDriftTests` holds the rule. For PB6031, an edition edge is a `DialectBehavior` member or a
`constructs/` row, never an inline year. `InlineEditionComparisonDriftTests` measured 155 such comparisons in 61 files
and ratchets them down. 39 of them moved into 25 members, and 116 remain. PB6031 stays open with editions `[]`.

**The integration.** Groups f and i both took `DialectBehavior` ordinal 35 and VCR row 7.91. Group f keeps them, and
group i's 25 members renumber to ordinals 36-60 and VCR rows 7.92-7.116. The renumbering reaches the enum, the registry,
the anchored VCR rows, `PublicAPI.Unshipped.txt`, the pipeline design doc and PB6031's decision addendum.
`compiler-facts.json`, `conformance-register.json` and `cite-resolve-baseline.json` were regenerated, never
hand-merged. The canonicalizer re-ran over the merged tree and rewrote the 3 designators the merge resolution had
re-introduced in comments.
