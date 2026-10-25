## Entry 2504 — 2026-10-24 19:33 PDT — PB3664: an editing sign follows the value the receiver holds, not the sending value

The fourth fuzz night (fz05181) found `MOVE -0.000092 TO PIC -Z(7)9.99` printing `-       0.00`. The owner approved the
fix on 2026-10-24 after the operator laid out the ISO text and GnuCOBOL's behavior.

**Re-probe.** On this build, with `S PIC S9V999 VALUE -0.001`, `MOVE S TO` the items `-9.99`, `+9.99`, `9.99CR` and
`9.99DB` printed `[-0.00][-0.00][0.00CR][0.00DB]`; `-Z(7)9.99` printed `-       0.00`; `MOVE -100 TO PIC -99`, `99-`
and `99CR` printed `-00`, `00-`, `00CR`. The same value through a signed numeric (`MOVE S TO N`, `COMPUTE`) printed
` 0.00`, so one value had two images depending on the path.

**Rule.** §13.18.40.5 5): "Table 8, Results of fixed insertion editing, shows the character(s) produced by an editing sign
control symbol, depending on the value of the data item", and Table 8's first column is "Positive or zero value". The data
item holds the value after alignment and truncation (§14.9.25.4 6) takes both from §14.6.8.2 4), "zero fill or truncation on
either end as required"), and "Zero is considered a unique value regardless of the sign" (§8.8.4.2.4). GnuCOBOL 3.2
agrees: `cob_move_display_to_edited` guards every `-`, CR and DB with `neg && !is_zero`, `is_zero` taken from the digits that
land in the picture. Every citation went through `cite.py --check`.

**Fix.** `CobolEdit.Render` computed `negative = value < 0` before the alignment and truncation, while BLANK WHEN ZERO, Z
suppression and floating insertion read the truncated digit string, so the four paths disagreed inside one call. It now
decides `negative` once, after the digit string exists (`value < 0` and a nonzero digit), and returns it; `Format` passes
that same sign to `Materialize` (it used to recompute `value < 0` for the wide EDITING literals). The fixed `+ - CR DB`,
the floating `+ -`, the FOR-form fixed and floating character-1 and the wide literals all read it.

**Sibling sweep.** `CobolLocaleEdit.Format` (format-2 locale editing, GR18 `+`) had the same pre-truncation sign and now
reads `!storesZero`, which it already computed for BLANK WHEN ZERO. The floating-point numeric-edited arm
(`CobolEdit.Float.cs`) cannot truncate a nonzero significand to zero and already renders a positive zero image on
underflow. `DeEdit` reads the sign from the image, and an image can no longer carry a sign on a zero value. The VALUE-clause
composition (`RuntimeApi.EditCompose`) goes through the same `Format`.

**The adjacent question.** `MOVE -100 TO PIC S9(2)` stores `+00` (the carrier has no negative zero; GnuCOBOL stores a
negative-signed zero and DISPLAYs `+00`). The edited receiver now agrees with it: a value truncated to zero is positive
zero. The determination is recorded in `docs/CONFORMANCE.md` section 3.

**Tests.** `conformance-suite/cases/13.18.40.5-Editing-rules/pb3664_edited_sign_stored_value` (one test, all four editions,
because the behavior does not differ by edition) and `CobolEditStoredSignTests` (the fixed and floating signs, the FOR form
fixed and floating, the locale editor; also the nonzero controls that keep their sign). The inventory rows: the note claimed
none, and none is closed.

