## PB4590 and PB4591: the suffix message quotes the written synonym, and the reserved-word funnel spells a word only when it rejects it

PB4590: the ACCEPT FROM / DISPLAY UPON arm of `CobolErrorStrategy.MnemonicSuffixMessage` quoted the token text, which for a
COBOL-WORDS synonym the lexer retyped is the original word. It now reads `WrittenSource.Of(word)` like the SET arm beside it
(witness `AMnemonicSuffixMessageNamesTheSynonymTheSourceWrote_NotItsOriginal`). PB4591: `VersionConformancePass.FlagReservedUserWord`
now takes the token and spells it only after `RejectsAt`, so `VisitCobolWord` and `VisitReservedGatedWord` no longer allocate the
written spelling of every word they visit. Both changes were first written as review round 3 of the duplicate PB4573 branch
`worktree-wf_acba161d-4d1-4` (kb/Work PB4593). They are carried onto main here by hand, recorded in the dispatch ledger as wave
20261864 A.
