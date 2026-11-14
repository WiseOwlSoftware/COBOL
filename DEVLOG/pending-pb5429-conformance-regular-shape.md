## PB3726 step B1: docs/CONFORMANCE.md made regular in place, and the first commit of step B2's citation burn-down (PB5429, PB5430)

**PB5429 (step B1 of the conformance split).** The re-probe was the survey: `conformance_source.py --check`, written
first, found 60 departures on the document as it stood: 50 determinations of section 3 with no `D-<KEY>`, sections 3,
3.1, 3.2, 7 and 8 out of key order, prose after the tables of 3.2 and 7, and a section 4 whose 27 blocks had no key at
all. The design's survey had drifted (section 3.11 exists, 3.3 to 3.11 carry tables, sections 2 and 5 have nothing after
their tables), so the as-built shape is recorded in DESIGN-orchestrator-loop.md section 16.8. The reader now knows the
document's one shape: `KINDS` (BULLETS 3 and 3.1, TABLE 2, 3.2, 5, 7 and 8, BLOCKS 4, PROSE the rest), `shape(n)`,
`determinations()`, `natural_key`, `render_section`/`render` (the one layout, which step B2 renders through) and `check`,
a DOC AUDITS check. A one-off script over that parse keyed the 50 (`kb/Work/PB5429/2026-11-14-keys.md`), gave section 4
`#### <key>` blocks and an epilogue, made the 3.2 and 7 tails row bodies and an epilogue, sorted every keyed section and
rendered the whole. Oracle: the non-blank line multiset differs only on the 50 keyed lines, 29 `#### ` headings and 2
epilogue markers, plus one citation repaired on purpose (row DOC-A.1-169, now `§2023:12.4.6.4.4 GR4b`, `cite.py --check`
OK); `audit_derivations`, `differential_fuzz`'s declined sets and `--write-register` gave identical output, `audit_annex_a1
--json` and the rendered ledger equal line multisets, and the extract's two row lists equal sets, now written in key order.

Two audits had leaned on the old order, and both were fixed at the root: `audit_annex_a1.py` counted a row body's
`#### DOC-A.1-56` heading as a prose mention, and the CITATIONS audit read the row above a table row as its context, so a
row whose prose names a wrong citation excused the next row's refused citation (`context_of`; with it the baseline is
unchanged). Planted cases: `scripts/selftests/spec/conformance_source/test_shape.py`, one finding per departure.

**PB5430, commit 1 (the burn-down).** Over the CITATIONS audit's files, 42 `CONFORMANCE.md §N`/`#ANCHOR` citations did not
resolve. Eleven, each the standard's clause number 4.2.16 written as a section of the document, now name what they meant
(`DOC-A.1-123` for the Int128 intermediate, the runtime message in `CobolNum.cs` included; `DOC-A.1-12`, `-135`, `-193`,
`-213`); seven planted bad anchors carry the example marker. The rest (16 templates such as `#DOC-A.1-<n>`, 6 in kb/Work
note bodies) wait for the arm, which steps B2 and C continue on this branch.
