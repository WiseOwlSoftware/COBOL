## kb/Work PB1563: what a READ of a LINE SEQUENTIAL line does with two dynamic-length members is derived and documented (D-FRA (x)), and a REWRITE of such a record no longer answers '44'

PB1563 asked what the standard requires READ to do with a line when the record holds two or more dynamic-length
members and the line, being plain text, cannot carry the PB1053 extent table. The re-probe on this build reproduced
the take step: `AA|KEY|CCCC` under `LIMIT 9` / `X(3)` / `LIMIT 8` reads back as `A=[AAKEYC] KY=[CCC] C=[]`.

The derivation, now `docs/CONFORMANCE.md` §3 D-FRA (x): a line is plain text (§12.4.5.10.3 GR2, §9.1.7.2), so every
character between two delimiters is the record, no table can ride beside it, and an in-line encoding would use
characters below U+0020 that are outside the line sequential character set (a WRITE of one is '71', §14.9.51.4 GR23).
What the standard requires of the READ is the record area's contiguous image (§14.9.30.4 GR13 c), GR15; §8.5.1.11.2;
`FUNCTION LENGTH`, §15.50.4 rule 7), and that image is exactly the line, padded to the minimum size or truncated to the
maximum. No rule says where one dynamic member ends inside it (§8.5.1.12.2 relates two groups' storage, not a medium);
GnuCOBOL, IBM and Micro Focus admit no dynamic item in the file section, so the take step of (ii) stays, the one split
every table-less medium uses. No diagnostic is required and none is issued. The determination is cross-linked from
D-FRA (v) and from the DOC-A.1-63 row (the line sequential exception to "every member comes back at its length").

The sweep of the neighbouring paths found one real defect. `SequentialConnector.NextFrame` decided that a READ had
transferred only PART of a line (the fact §14.9.35.4 GR17 a) refuses a REWRITE on) by comparing the line with
`RecordWidth`, the character window, which for a variable-length group is only its 3-character fixed run; GR15's bound
is the descriptions' maximum size (`MaxRecordSize`), which the READ's own truncation already used. A whole 9-character
line of that record therefore made every REWRITE '44'. The two comparisons now ask the one bound.

Goldens: `conformance:cases/12.4.5.10-ORGANIZATION/pb1563_line_sequential_dynamic_members` (accept 2023; walks WRITE
with GR21 space dropping, READ, '47', '48', '71', REWRITE in place and '44' for a longer record, '06' with the remainder
by the next READ, the empty line padded to the minimum, '10', '46', the REWRITE after a partial READ ('44'), and SORT
USING/GIVING, with a USE procedure and FILE STATUS and with FILE STATUS alone) and the refusal
`pb1563-line-sequential-dynamic-members-below-2023` (85, 2002, 2014: COBOLNET0900). No inventory row was claimed or
closed; `closes_rows: []` with its reason. Self-review (silent-failure, comment, test-analyzer lenses): the one-token
fix has no catch or default arm to swallow a failure, the new comments name D-FRA (x) and the bound they rely on, and
the golden covers each status the record can answer rather than only the success path.

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261018T133019Z-4ac93f).
