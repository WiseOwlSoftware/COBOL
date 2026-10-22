## Entry 2345 — 2026-10-22 03:21 PDT — PB3505: DISPLAY of a numeric item shows the decimal point and a leading sign by default; the digit image is `--numeric-display digits`

**What.** `DISPLAY` of `PIC 9(5)V99` holding 12.5 printed `0001250`, and of `PIC S9(5)V99` holding -12.5 printed
`000125}`: the digit image, no point, the sign over-punched onto the last digit. ISO §14.9.11.4 GR1 leaves the conversion
between a DISPLAY operand and the device to the implementor, and on 2026-10-21 the owner decided (PB3467, "yes") that the
default is GnuCOBOL 3.2.0's default form, `00012.50` and `-00012.50`, with today's image behind a compile option.
615 of the first overnight fuzz run's 1,141 output differences were this one form.

**The option.** `--numeric-display decimal|digits` (`NumericDisplayForm`, default `decimal` at every `--std`), carried like
`--sign-encoding`: `CompilerDriver.Options` to `EditionContext` to `EmitContext`, baked into the compiled program. It
reaches DISPLAY's conversion only: MOVE's character image, comparisons and record images stay the digit image, and
`NumericDisplayFormDriftTests` holds that reach (the DISPLAY emitter alone reads the option, `OperandText.AsDeviceString` is
its one entry, and every non-default value is pinned by a `wiseowl/options.json` test).

**The form.** One runtime composition, `CobolNum.DisplayDecimal`, serves a native carrier and an item stored as its character
image, so a REDEFINES view prints what its twin prints: the digit positions, the point at the net scale in the program's
separator (a comma under DECIMAL-POINT IS COMMA), the zeros a P scaling stands for (`PIC 9(3)PP` holding 1200 shows `01200`,
`PIC PP9(3)` holding .00123 shows `.00123`), and a signed item's sign as a leading `+` or `-`, trailing for `SIGN TRAILING
SEPARATE`.

**What the survey found that the note did not predict.** The note asked for GnuCOBOL's default form measured across the
whole category before coding, so `cobc -x -std=default` ran over every usage, sign form, scale, group, literal, edited item,
float, function and index (the table is in `docs/CONFORMANCE.md` item 56).
- `SIGN TRAILING SEPARATE` shows its sign trailing in GnuCOBOL (`001.50-`), and the form copies that.
- A COMP-5 or BINARY-* item shows its container's digits, 3, 5, 10 or 20 for 1, 2, 4 or 8 bytes. So a COMP-5 value beyond its
  PICTURE shows whole: GnuCOBOL 3.2.0 prints `18446744073709551615` for `PIC 9(18) COMP-5`, where kb/Work R13 had recorded
  the cut `446744073709551615` as GnuCOBOL's. The digit image keeps the cut (R13's golden became
  `pb3505_display_comp5_beyond_picture_digits`); R13 carries a superseded-in-part note. This also fixes a wrong answer in
  the digit image's family: `PIC 9(3) COMP-5` holding 12345 printed `345`.
- GnuCOBOL prints a SCALED COMP-5 as its unscaled integer, losing the scale. Not copied; the point is shown.
- Function results (item 92's literal form) and floats keep their own forms; GnuCOBOL's per-function field widths are not a
  rule, and the divergence is recorded in item 56.

**Goldens.** 200 suite `.out` files and 46 differential `.out` files moved to the decimal form. Each new value was checked
against the old under an independent digit-signature fold (same digits, same count of negative markers); the 29 files that
differ in width (container digits, P zeros, a COMP-5 value past its picture) were read against the rule by hand. Six new
WiseOwl tests pin both option values, the comma mode, the container widths and the REDEFINES, packed and table views, with expected values written from the
declarations; `NumericDisplayDecimalTests` pins the survey table row by row; 77 inline expectations in the Conformance
project and one Unit test were re-derived from the pictures. A spec assertion that runs before a golden hid one differential
golden until the second pass (`AddCorresponding_RoundedAppliesToEachPair`).

**Differential fuzz harness.** The display-form fold encoded the old difference, so a run made now would have hidden real
defects as `OUTPUT_LATITUDE`. A row now records the form its WiseOwl compile used (`numeric_display`); only a digit-image row
folds, and a row recorded without the field (every row before this change) is a digit-image row and keeps its fold when
re-read. A display-form difference in a new run stays `OUTPUT_DIFFERS`, where the triage reads it.

**Citations.** Two inherited clause numbers were wrong: the P-scaling "operations" rule is §13.18.40.4 GR14, not §13.18.40.3.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261022T095503Z-3ed10b). Beyond leg 1, the whole Conformance and Unit
projects and the Characterization snapshots were run locally: green except two `ExternalCorpusPopulationDriftTests` cases
that need the GPL external corpus a worktree does not carry.

