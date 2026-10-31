## Entry 2874 — 2026-10-31 13:18 PDT — PB107 review round: a number's lead stands below every digit-like character, and the review's five leads are answered

The branch review of the PB107 collation landing (`worktree-wf_94e81f77-f38-7`, head `d917370e0`) came back with six findings. This entry answers them.

**R1-5 (correctness), re-probed first.** On the built runtime `en-u-kn-true` compared `"0"` with a superscript zero at +1 where the host ICU, and plain `en`, give -1; `Compare("0", "⓪")`, `("a0", "a⁰")` and `("1", "⁰")` all +1. Cause: a number began with `Lookup('0')`, the digit zero's own element, whose primary the compatibility zeros (U+2070, U+2080, U+24EA, and the expansions they lead) share. The number's count and digit elements are small integers (1..n), compared only with a number's own kind by the iterator's own documented invariant, but a compatibility zero's following elements are real primaries, so the invariant was false and every number sorted inside that run. Fix at the root: `CollationTable.NumericLead` is the digit zero's first element with its primary ONE LOWER, a value no character carries, so only another number's lead equals it and every number sorts before every string that begins with a digit-like character, which is ICU's order. `root - 1` could be handed to a tailored character when 15 slots were inserted after the root below the zero; `CldrTailoringBuilder.Line` now reserves the top value of every primary gap (`reservedTop`), so the lead is never taken. All 135 pack locales' tables fingerprint identical before and after (elements of 0..17FF, 2000..24FF, 3000..30FF, a Han and Hangul sample, FF00..FFEF). Tests: `NumericOrdering_ANumberSortsBeforeAnyStringBeginningWithACompatibilityDigit` (five digits, six numbers, both orders, keys), `NumericOrdering_TheLeadPrimaryIsNeverGivenToATailoredCharacter` (fills the gap to its edge; with `reservedTop` set back to 0 it fails on the lead being a pool primary), and the host-ICU corpus now holds `⁰ ² ₀ ⓪` (352 disagreements before, none after).

**R1-1 (duplication).** `TheTableKnowsItsDecimalDigits` used a private `RepoRoot()`; it uses `TestRepo.At` now. The guard that should have caught it, `TestRepoDriftTests.NoTestSource_FindsTheRepoRootItself`, lacked `Path.GetDirectoryName(` among its ascents; added.

**R1-6 (duplication).** "The second element of an implicit weight is never renumbered" was written in `CldrTailoringBuilder.IsImplicitSecond` and again, with a literal floor, in `ElementRemap.Apply`. It is `CollationTable.IsImplicitSecond` / `ImplicitSecondFlag` now (also the `| 0x8000` of `GetImplicit`), asked by the builder's roots, its special positions and the remap; `ImplicitSecondDriftTests` fails a second `0x8000` under `Collation/`.

**R1-2, R1-3, R1-4 (comments).** `CldrCollation.Unsupported`'s doc described the engine before the landing; `ElementRemap` is split from `TailoringPlan`'s summary again; the note's Landing section no longer says the level-4 weight under NonIgnorable is the tailored slot alone.

**Found on the way (I9).** `en-u-kh-true` listed the hiraganaQ line twice: the tag parser and the builder both reported it. The builder, which sees the effective settings (the collation's overridden by the tag's), reports it once; the tag no longer does. `LocaleManagerTests` asserts a single entry.

**Still open.** `[hiraganaQ on]` stays the owner's question in PB107 (CLDR-deprecated; ICU 53+ ignores it; needs a Script property the pinned data lacks), reported per collation, never dropped. No inventory row moves (0 claimed).

Gate: see the report.

