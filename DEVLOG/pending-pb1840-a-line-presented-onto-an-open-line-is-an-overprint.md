## PB1840: a line presented onto an open line is an overprint, not a continuation

A BEFORE-placed record presented while an earlier write's line was still open (an AFTER write's, a plain WRITE's, a
BEFORE ADVANCING 0's) was appended to that line's text: AFTER 1 "AAAA" then BEFORE 1 "BBBB" left `AAAABBBB`, and AFTER 1
then BEFORE ADVANCING PAGE left the same weld before the form feed. §14.9.51.4 GR25 e) presents a BEFORE write's line
"before the representation of the printed page is advanced" and GR25 f) an AFTER write's after its own advance, so no
advance lies between the two lines and the second is at the SAME vertical position as the first, which is GR25 c)'s zero
advance by another route. The standard states the position, so it leaves no latitude and rule 1's GnuCOBOL fallback does
not apply (GnuCOBOL appends); the device representation is the one R54 chose for ADVANCING 0, a bare carriage return.

Re-probe on this build before the change: `\nAAAABBBB\n`, `\nAAAABBBB\f`, `\nAAAABBBB\r` for the three shapes; after it
`\nAAAA\rBBBB\n`, `\nAAAA\rBBBB\f`, `\nAAAA\rBBBB\r`, on a record sequential print stream, a line sequential file and a
LINAGE file alike.

Fix shape: `SequentialConnector.Present`, the one presentation every WRITE arm and the report writer go through, writes
the return before a non-empty record when the device is on an open line it has not already returned over. The old
`_lineOpen` boolean became one three-state field, `DeviceLine { Fresh, Open, Returned }`, because the rule needs "open"
and "returned" to agree: `WriteTravel(0)` moves Open to Returned, so AFTER 1 "A", BEFORE 0 "B", BEFORE 1 "C" is `A` CR `B`
CR `C` and never two returns in a row. A record with no characters (a line of spaces, the report writer's page-feed
carrier) overprints nothing and writes no return, so no report golden moved. The BEFORE amount is irrelevant to the
rule (a count, 0, PAGE, with or without LINAGE), which closes the PAGE shape the PB1027 refuter measured without a
second arm. A refused WRITE presents nothing, so it neither writes the return nor closes the line.

Tests: golden `14.9.51-WRITE/pb1840_before_after_open_line` (all four editions, seven scenarios derived from GR25 and
§13.18.34.4 GR7, the LINAGE counter and page arithmetic included); `OpenLineOverprintTests` (19 cases: the success shape
on record sequential, line sequential and LINAGE files, and the exception paths the owner's rule asks for: a '44' outside
the RECORD IS VARYING bounds with and without a USE declarative, a '71' for a character outside the line sequential set,
and a GR6 b) 2 LINAGE violation at the page transition of a BEFORE write with and without a declarative). The two tests
whose expectation encoded the append were re-pinned (`LinageConformanceTests` LNGBYF, `FileIoDifferentialTests`
WriteAdvancingMnemonic_ZeroLineAdvance_RecordAlwaysReleased). The determination replaces the "not decided here" paragraph
of docs/CONFORMANCE.md "ADVANCING 0". Rows: none claimed, none closed. Not covered, because it needs a fault-injecting
medium the sequential connector does not have: a host write failing between the return and the record ('34'/'30').

Self-review (pr-test-analyzer, silent-failure-hunter, comment-analyzer lenses): the new tests derive their bytes from the
rule and were not captured; both arms of the dispatch (AFTER 0's travel return and the new presentation return) are
covered by the same state; the doc comments of `WriteTravel` and the field were brought to the three-state model. The
whole NIST corpus (378) and the conformance suite (10,831 cases) ran green on the branch before the gate. Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`
(run 20261018T214428Z-6734e2). Linux gate: the lander's (batched gating).
