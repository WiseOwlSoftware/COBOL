## ACCEPT reads the decimal device form DISPLAY writes (PB3513)

PB3505 made DISPLAY of a numeric item write GnuCOBOL 3.2.0's default form (`00012.50`, `-00012.50`) and left ACCEPT decoding
a digit window, so text DISPLAY wrote and a later ACCEPT read came back about ten times too small with its sign lost:
`00012.50`, `-00012.50`, `+005` into `PIC 9(5)V99`, `PIC S9(5)V99`, `PIC S9(3)` gave `00001.25|+00000.12|+000|` where
GnuCOBOL 3.2.0 gives `00012.50|-00012.00|+000|`. The cause is one: the numeric arm of `AcceptDisplayEmitter.EmitAcceptDevice`
read a window of the item's size and decoded its digit characters at the PICTURE's scale, so the decimal point and the sign
character contributed nothing.

**The fix.** ACCEPT's GR1 conversion (ISO §14.9.1.4 GR1: "Any conversion of data required between the device and the data
item referenced by identifier-1 is defined by the implementor") now follows the option DISPLAY follows,
`--numeric-display`. Under `decimal`, the default, the emitter calls `AcceptSource.DeviceNumeric`, which transfers the
item's own size in characters for an item stored as characters (GR3/GR4 b, as before) and one whole record for a binary or
packed item, and converts the text with `CobolNum.ParseDeviceDecimal` (`CobolNum.DeviceDecimal.cs`, beside the composition
it inverts). Under `digits` the digit window and `ParseDisplay` stay, byte for byte. The reader's rules are GnuCOBOL's, found by
running `cobc -x -std=default` over about 1,300 receiver and text pairs in WSL: leading spaces, an optional leading
sign, digits with one decimal separator (the program's, the other separator and spaces skipped), a fraction truncated at
the scale, high-order integer digits dropped, a BINARY-CAPACITY container wrapped, a P-scaled item read by value; any other
character, a trailing sign included, makes the text zero with no exception; a character item stops reading when its last
position is filled, so `12.50CR` into `PIC 9(5)V99` is 12.50, where a binary item reads the whole record. The new
reader matches all of the survey except a scaled COMP-5's DISPLAY form (PB3505's divergence) and a NATIONAL item given a line
longer than itself, which GnuCOBOL fills with unrelated digits. DOC-A.1-1 carries the determination, including that DISPLAY's
signed or scaled text does not always read back whole (the window is the item's size, in GnuCOBOL too).

**What the re-probe and the survey showed.** Two things the note did not say: GnuCOBOL's binary and packed items do not take
the size window at all (a decimal point or sign would be cut off), and its stop-when-full rule for DISPLAY items is why
`12.50CR` reads and `12.5CR` does not. Both are in the reader.

**Sweep.** The only emitter that reads the device into a numeric item is `EmitAcceptDevice`; its group, reference-modified,
alphanumeric, national, boolean and float arms are character stores or the float reader (already decimal). The other callers
of `NumParseDisplay` decode storage images. The differential fuzz harness never ACCEPTs, so its display-form fold is
unaffected. `NumericDisplayFormDriftTests` pins the new reader's reach (one façade method, one emitter file).

**Tests.** Five process-level tests in `AcceptDifferentialTests` (the note's repro under both values of the option, every usage,
invalid text, DECIMAL-POINT IS COMMA) and eight rows in `NumericDisplayDecimalTests` (the reader's table, container wrap, P
scaling, the inverse over DISPLAY's own text), their expected values from the survey and the cited rules. The
conformance suite runs programs with standard input closed, so the golden the note named could not carry input; the C#
tests pipe it. `kb/Work/PB3513` landed with `closes_rows: []` (it implements an owner decision and claims no inventory row).

**Self-review.** Architecture: one reader beside its inverse, one façade method, the option read in the one emitter file;
no second spelling of the form. Full code: the reader implements the cited rules and the measured survey; sibling arms agree.
Performance: the reader allocates nothing beyond the transferred window. Duplication: none. Comment and silent-failure
passes: the zero for invalid text is a documented choice (the standard defines no exception for ACCEPT conversion), and the
NATIONAL divergence is written where it applies.
