## A token the margin cut in two is named even when the program still parses (PB4999)

Fixed-form text past margin R (position 72) is not program text (ISO 1989:2023 6.3.1, Annex A.1 item 158), so the format drops
it. A number `0.7` with its `7` at position 73 therefore reads `0.`, the program still parses, and it computes `Y ** 0` with no
word from the compiler: the margin-R note (COBOL5788) is given only beside a syntax error. This was the cause of PB4981's false
wrong-answer finding.

**Re-probe.** A `COMPUTE X = Y ** 0.7` line with the `.` at position 72 and the `7` at 73 compiled with no diagnostic.

**Fix.** The listener's question, "was this line's program text cut where it may have been program text?", now has its second
arm: `MarginRCutScreen.Warnings` asks the same `IMarginRCutLocator` of every default-channel token after the parse and gives the new
warning COBOL6688 for a token that ends at position 72 with cut text at position 73 (`LocatedMarginRCut.EndsAtMarginR`, the decision
the listener already used). A line that drew COBOL5788 is not named again. Sequence numbers and tags set off by a space, recurring
tags, an inline comment's rest, comment lines, omitted branches and free form stay silent. The sentence that says what was cut moved
into `MarginRCut.Account` and `MarginRCut.Remedy`, shared by both warnings.

**Witnesses.** `unit:MarginRCutTokenTests` (10 cases), conformance case `pb4999-token-cut-at-margin-r` (output `1`, derived from
the text the format keeps). The suite has no form for an accepted program's warning, so the warning itself is pinned by the unit test.

Review round 1 (R1-1): the screen now asks the locator at every character of a token, so a hyphen-continued literal whose first segment ends at position 72 is named too (test AContinuedLiteralCutAtMarginR_IsNamed); the COBOL2449 summary spacing was restored (R1-2).
