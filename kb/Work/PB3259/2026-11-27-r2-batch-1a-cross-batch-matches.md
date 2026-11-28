## R2 batch 1a (pin 9e65164eb): the filer matched three upheld findings to landed notes of other mechanisms

`file_census_notes.py --r2` over batch 1a's collected.json skipped three upheld findings as "already-filed (its note is landed)". Each match was `same_mechanism` on overlapping members or sites between findings of one wave kind, and in each the landed note names a different mechanism:

- `bound-tree-1--duplication--f1#3` (item identification decided by bind-time nodes for CORRESPONDING and emit-time locals for STRING, UNSTRING, INSPECT, INITIALIZE, OO, CALL) matched PB3238 (the ON / NOT ON conditional-phrase pair rendered by hand in five emitters).
- `bound-tree-3--duplication--f1#2` (a statement edition gate asked by the conformance pass and again by StatementBinder, ENTER gated at bind time) matched PB2725 (a quoted literal class decided twice in three grammar rules and five consumers).
- `procedure-binding-4--code--f1#1` (EVALUATE skips the intermediate for a later subject whose one use sits behind the first phrase's && chain) matched PB2722 (EVALUATE accepts NOT before ANY, TRUE and FALSE).

Without a hand check the three would have been dropped, since the filer files nothing for a mechanism whose matched note is terminal. They were filed as PB6230, PB6231 and PB6232. The same run's within-batch grouping also merged `bound-tree-1--architecture--f1#2` (the generated walk is blind to the InitializeAction and ValidateStep helper hierarchies) into `bound-tree-1--duplication--f1#1` (BoundReferences and the generated walk are two mechanisms); their rules and targets differ, so they were filed apart as PB6233 and PB6224.

Every shape PB3259 lists is a match on site or member overlap with no test of the rule and target. These four are further evidence for its Target, and a match against a TERMINAL note should never silently drop a finding.
