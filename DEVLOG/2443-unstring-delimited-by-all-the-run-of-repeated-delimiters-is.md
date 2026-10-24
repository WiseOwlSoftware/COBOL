## Entry 2443 — 2026-10-23 23:30 PDT — UNSTRING DELIMITED BY ALL: the run of repeated delimiters is consumed after the INTO store, as libcob does (PB1909)

PB1909 was the one measured GnuCOBOL difference PB1907 (wave 1002) kept for cost. An UNSTRING with `DELIMITED BY ALL <identifier>` whose INTO,
DELIMITER IN or COUNT IN store of the same statement overwrites that delimiter (or the sender) is undefined by §14.9.48.4 GR18 and Annex A.2
item 61 (both `cite.py --check` OK; §4.4 2) too), so CLAUDE.md rule 1 sends the choice to GnuCOBOL: libcob's `cob_unstring_into` stores into the
receiving area first and only then skips the delimiter's repeats, against the delimiter item's own storage. WiseOwl COBOL consumed the whole run
inside the one examination call, before the store.

**Re-probe.** On a fresh Debug build the golden's leg B2 gave `Y2=[b;;;;c;  ]` against cobc 3.2.0's `Y2=[;b;;;;c; ]`. Four more probes differed the
same way wherever the run's length depended on the overwrite: `"a;aab"` with the first INTO overwriting the delimiter with `a` (ours `Y2=[   ] Y3=[b  ]`,
libcob `Y2=[b  ] Y3=[zzz]`), an INTO store into the sender inside the run, a two-character `ALL` delimiter overwritten by the second area, and a
`DELIMITER IN` into the `ALL` delimiter (this last one agreed already).

**Fix.** The note proposed two kernel calls "on the overlap path". The structure is one rule in one place, so the split is made for every ALL phrase and the
overlap path only adds the re-read. `CobolStringOps.UnstringExtract` consumes ONE occurrence of the matched delimiter and returns the matched delimiter's
list position in a new `matched` out-argument; that replaces `MatchedDelimiterIndex`, which guessed the position back from the delimiter text and was
deleted, and the `allFlags` argument left it. `CobolStringOps.UnstringSkipRun` consumes the contiguous repeats (GR7) over the delimiter and sender it is
handed. `StringEmitter.EmitAllRun` renders it after the receiving area's INTO store and before DELIMITER IN and COUNT IN (libcob's order), re-reading the
sender and the matched `ALL` delimiter first where `StorageSharing.MayShare` says a store can reach them. A statement with no `ALL` phrase renders no
skip and no flag array. The runtime major goes 16 to 17: `PublicAPI.Shipped.txt` lost `MatchedDelimiterIndex` and `UnstringExtract` changed shape, so
`RuntimeAbiPinDriftTests` is re-pinned (runtime hash `997188CC94F3A8D8`; the call-ABI pin did not move).

**Goldens.** `14.9.48-UNSTRING/pb1907_unstring_overlap_delimiter`: B2 now reads libcob's line and the file gains B3 (the run follows the store, POINTER 6),
B4 (the INTO store reaches the sender) and B5 (a two-character `ALL` delimiter); all five lines were run through cobc 3.2.0 and agree. Unit:
`UnstringKernelTests` (the two kernel calls, values read from GR7, GR9, GR10 and GR13) and five new `UnstringLiveReadDriftTests` facts (no skip without an
`ALL`, a skip per area, a re-read ahead of it only under the predicate). The Characterization snapshot `char_string_ops.85.g.cs.txt` moved by the new
call shape only. `docs/CONFORMANCE.md` D-UNS2 lost its "one documented difference from GnuCOBOL" sentence and now states the order; the string-ops
design and the compiler design carry the new signatures.

**Rows.** PB1909 claimed no inventory row (`closes_rows: []` with its reason); GR-14.9.48.4-18 was already closed by PB1907. The first gate went red
on `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`: that row's code-location still named the deleted `MatchedDelimiterIndex`.
A `record_verdicts` batch retired that witness, added `UnstringSkipRun` and `UnstringExtract` and two test witnesses, and restated the row's notes (GAP 63 to 63).

**Self-review.** Architecture: one rule in one place (the run is `UnstringSkipRun`'s alone, the position is the kernel's alone). Silent failure: the skip's
`matched` bounds guard that quietly did nothing was removed so a mismatched flag array is loud; a zero-length live delimiter is the one deliberate no-op
(an empty delimiter would match without end). Comments: the kernel and emitter summaries were rewritten with the code. Tests: the sibling arms were swept
(`ALL` literal, `ALL` identifier disjoint, overlapped delimiter, overlapped sender, no `ALL`).

