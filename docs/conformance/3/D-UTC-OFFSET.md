- **D-UTC-OFFSET — CURRENT-DATE / WHEN-COMPILED UTC-offset positions 17–21 — the TRUE offset is reported, out of the rule's
  stated range when the host is (§2023:15.21.3 r1 / §2023:15.99.3 r1; documented per §2023:4.2.6 / §2023:4.2.7).** One determination
  covers both functions because one formatter serves both (`CobolDate.Format21`; the compile-time bake and the
  run-time read are separate seams onto it). §2023:15.21.3 r1 fixes positions 18–19 at "00 through 13" when position 17
  is `+` and "00 through 12" when it is `–`, and §2023:15.99.3 r1 carries that table character for character. **Real
  offsets exceed both bounds.** UTC+14:00 is a shipping zone on both platforms WiseOwl COBOL targets — IANA
  `Pacific/Kiritimati`, Windows `Line Islands Standard Time` ("(UTC+14:00) Kiritimati Island", verified against the
  host zone table, not assumed) — and the `WISEOWL_COBOL_CLOCK` pin reaches **−14:00** as well, because it parses a
  `DateTimeOffset` directly and that type's offset range is ±14:00. So the departure is **symmetric: both signs**.
  For such a clock **the rule's table offers no in-range value at all**, and every available behaviour departs from
  something: emitting `14` leaves the stated range; clamping to `13` misreports the offset, which §2023:15.21.1 defines
  as "the … local time differential factor provided by the system on which the function is evaluated"; and writing
  `0`/`00`/`00` asserts position 17's "the system … does not have the facility to provide the local time
  differential factor", which is false — the system has it and it is 14 hours.
  **The determination: report the true offset.** It is the only one of the three that does not state something
  untrue, and it keeps the value usable as an offset. The departure is confined to positions 18–19 and to
  |offset| > 13 (`+`) / > 12 (`–`); positions 1–16, 17 and 20–21 conform outright at every offset, which is what
  makes this one bounded determination rather than an open-ended divergence — `CurrentDateOffsetDeterminationTests`
  asserts exactly that scope (the full §2023:15.21.3 r1 position table re-derived from the rule text, with the violation
  set required to be **precisely** `{18-19}` across the whole fixture range and empty for every in-range offset).
  End to end through the clock seam: `CurrentDateOffsetPinTests` (a COBOL program under `WISEOWL_COBOL_CLOCK` at
  ±14:00, +13:00, +05:45, +00:00 and −09:30). Inventory rows `RV-15.21.3-1` and `RV-15.99.3-1` carry this one
  determination at DOCUMENTED-NON-SUPPORT. **It holds for the 1989 intrinsic-function amendment as well** (kb/Work PB5304):
  §1989:A.2.11.3 prints the same table ("two numeric digits are returned in the range 00 through 13"), and the same formatter
  serves `--std 85`, so the 1989 row `1989-RV-A.2.11.3-1` departs in the same two positions for the same reason. No 1985 latitude
  covers it: §1989:A.2.11.3 RV1 states the ranges with no implementor-defined wording, §1985:I.1.5.2.1 concerns substitute or
  additional language elements, and §1989:A.2.3 leaves to the implementor only what a NUMERIC or INTEGER function's value does not
  specify. The departure is therefore documented non-support of §1989:A.2.11.3 RV1 (a stated exception to §1985:I.1.5.2.2,
  "An implementation must accept the syntax and provide the function for all Standard COBOL language elements"), and the row
  is recorded DOCUMENTED-NON-SUPPORT with this entry as the statement (kb/Work PB5304).
