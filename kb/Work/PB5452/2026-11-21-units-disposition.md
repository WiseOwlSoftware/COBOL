## 2026-11-21 — §1985:III.2 and §1985:B.2.1 are decided unit by unit (wave 20262131 group e, branch `train85b-cat`)

**Why no tool could mint them.** Both were `not-rules` blocks whose refuter (85b-01) OVERTURNED the disposition, so the
import set them `pending`; but the units mechanism reads only the normative region (Sections IV, VI-XVI, Section A) and
a declared block's lists are held, while the block mechanism can only admit a whole list as rules or keep it out. The
glossary's rule sentences ("integer must be a numeric literal which is an integer", §1985:III.2) are no item of its
lists, and §1985:B.2.1's item (9) ("An implementor must provide the capability to handle 50 levels of qualification")
sits among 106 items that summarise changes and mostly restate module rules: admitting either list mints wrong rows,
keeping it out loses the rules.

**What was built.** A third block disposition, `units` (`text_units.UNITS`): the clause's whole text, its lists
included, is segmented into units wherever it lies (`rule_catalog_1985._bodies`'s `whole`; the ISO arm reads it too),
each entered pending and disposed like any other. The two manifest entries now carry `units` (their `why` says so and
keeps the overturned record's text). Regenerated: §1985:III.2 has 382 units, §1985:B.2.1 117 (its items `1`..`107`,
2.1A's `L2.n`, 2.1B's `L3.n`); `pending-clauses` is empty, so the status line's "2 clauses pending adjudication" is
now 499 units of unnumbered text pending.

**Next:** the readers and refuters take the slices `E:\COBOL-coord\scratch\w20262131e\in\85w-01..09.json` and the
first 22 units of `85w-10.json` (III.2) and `85w-11..13.json` (B.2.1); `import_dispositions.py` and the mint follow,
on the train branch after the PB5453 import.
