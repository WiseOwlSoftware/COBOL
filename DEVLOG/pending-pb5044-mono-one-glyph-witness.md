## PB5044: the underline self-test witnesses the monospaced one-glyph guard

Two branches answered the same PB5044 review round 1 in parallel. The one that landed (entry 3398) planted the ragged
monospaced rule at 91 % and the `MT` word narrower than `MONO_RULE_MIN_W`. This branch carried the same two repairs and
one more, which entry 3398 does not have: the `len(text) < 2` half of the monospaced skip in
`figure_raster.underlined_hints` had no witness. Dropping it left the self-test GREEN, because every planted one-character
word was proportional.

Fix. Case 8b of `test_figure_raster.py` now plants `M`, a one-glyph monospaced word 7 pt wide (wider than
`MONO_RULE_MIN_W` 6.0) with a whole rule under it, which the detector must not report. With the length test removed the
case turns RED (it reports `M`). Self-test 78/78. The conflict with main was resolved by taking main's MONO and `MT`
planting, its TRANSCRIPTION-STATE wording and its stamped entry 3398, and adding only the `M` witness; this branch's
duplicate `MN` case is dropped. No inventory row closed; no compiled program changes.
