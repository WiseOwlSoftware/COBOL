## Lead: ListingRequest's init accessors validate against the OTHER property's current value, so a `with` or object initializer is accepted or refused by the order its assignments are written (step 2, review round 2, R2-1, low)

Source: review-LS2 round 2, `E:/COBOL-coord/scratch/reports/w20262104b-PB5925-report.md` line 86.

**Sites:** `src/WiseOwl.Cobol.Listing/ListingRequest.cs#OutputBase`, `#Formats` (init accessors, lines 32-45), `#FormatsProblem`.

The R1-3 fix (the beside file `2026-11-18-listing-step-2-review-r1.md`) validates in each `init` accessor. The `OutputBase` accessor asks
`FormatsProblem(value, _formats)` and the `Formats` accessor asks `FormatsProblem(_outputBase, value)`, each against the OTHER property's value at
the moment it runs. So `request with { OutputBase = null, Formats = Text | Html }` is read in the order written: the formats are set (and checked)
against the OLD output base, then the output base is set. A request that changes both together (standard output, which takes one format, and two
formats) is refused or accepted by the order of the two assignments, not by the request it builds. Read on origin/main 0d4042678a: lines 32 and 45
are exactly this; the constructor (line 26-28) checks the pair once, with both values, and is order-free. No caller changes both in one `with` today
(the lead's own statement; not re-searched).

Fix shape: validate the PAIR once after the object is built, not in each accessor (a `with` expression and an object initializer both end in a
point where the whole request exists, so one `Validate()` the consumer calls first sees both values), and plant
the two orders of one pair (`with { Formats = Both, OutputBase = null }` and `with { OutputBase = null, Formats = Both }`) so they give one answer.
