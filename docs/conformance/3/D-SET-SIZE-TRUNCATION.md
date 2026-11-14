- **D-SET-SIZE-TRUNCATION — SET SIZE OF — "truncated to the nearest integer" (§2023:14.9.39.4 GR37 second sentence) is TRUNCATION TOWARD
  ZERO** (kb/Work PB464). "If arithmetic-expression-5 evaluates to a number that is not an integer, the result of
  arithmetic-expression-5 is truncated to the nearest integer" pairs two operations that differ: truncating 2.7
  discards the fraction (2), rounding it to the nearest integer selects 3. It is the standard's only use of the phrase
  (one occurrence in the whole document), so no sibling use calibrates it. **Determination: the fraction is
  discarded, 2.7 gives 2, a half does not round away from zero (2.5 gives 2), and 0.9 gives length 0 with no
  condition** — a nonnegative amount that is merely fractional raises nothing, because the only conditions GR37/GR38
  name are "not a nonnegative number" and "greater than the maximum size". **Why this and not the other:** the
  standard gives "truncation" a defined meaning of its own, the ROUNDED MODE IS TRUNCATION mode that every
  arithmetic statement without a ROUNDED phrase takes (§2023:14.7.4.3 GR2), and defines that mode in the very idiom the SET SIZE rule
  uses: "rounded to the nearest value nearer to zero" (§2023:14.7.4.3 GR10; §2023:11.9.11.2 GR2 e) for the intermediate
  value). "Truncated to the nearest integer" is that idiom compressed — the nearest integer in the toward-zero
  direction. **Order of the tests:** the sign test (GR37 first sentence) and GR38's "evaluates to a number that is
  greater than the maximum size" are asked of the EVALUATED value, before the truncation, so −0.5 takes GR37's leg
  and 9.5 against a maximum size of 9 takes GR38's clamp and sets the condition (the length, 9, is the same either
  way); the truncation only chooses the length stored. **The amount is read in its own domain:**
  `CobolDynString.SetSize` has one overload per evaluation carrier (binary64, SDIDI, scaled `Int128`, unsigned-wide
  `UInt128`), and `SetEmitter.SetSizeCall` maps each `NumXCarrier` onto its own, so 2.99999999999999999 truncates to
  2 and not, through its binary64 image 3.0, to 3 — the same discipline CONTINUE AFTER already follows
  (`StatementEmitter.ContinueAfterCall`, kb/Work PB1529). Goldens `2023/pb464_set_size_fraction_truncates` (2.7, 2.2,
  2.5, 0.9, 8.9, the grow case, the 17-digit near-integer and the fractional amount above the maximum) and the
  refusal below 2023 `pb464-set-size-fraction-below-2023`.
