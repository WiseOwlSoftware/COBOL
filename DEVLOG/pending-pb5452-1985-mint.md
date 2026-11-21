## The 1985 mint: 629 rows of unnumbered text, the 37 not-rules blocks refuted, 21 carved parents reopened

kb/Work PB5452's disposition campaign decided and refuted every pending 1985 unit of unnumbered text; this landing turns
those records into catalog rows in one change (decision PB5448 point 5: each edition's denominator corrected once,
nothing closed at minting).

**What was built.** `scripts/spec/import_dispositions.py`, the one importer: the latest slice's reader record wins, an
OVERTURNED refuter record's correction wins over it, and it refuses a stale key, a unit with no record and a
`restates`/`not-a-rule` with no UPHELD record (its self-test shown red on each of the three planted failures). The refuter
records are committed in `docs/rearchitecture/spec-dispositions-1985/`. The 1985 reader now carves a decided margin
paragraph out of its rule's text and mints every unit disposed a row kind (`rule_catalog_1985.mint`), with `box`,
`level`, `definitional` and the list's `lead`. Two more refuter slices were needed before the import could stand: six
corrections to `restates` that no refuter had attacked and two unread lead-ins (85x-01: 4 upheld, 4 overturned) and one
corrected list (85x-02, upheld).

**Found and fixed at the root on the way.** A paragraph headed "Examples" and a format band on a line of its own were
units (27 records retired); the 1985 pointer grammar missed five of the text's phrasings ("is located on page", the
glossary pointer, a footnote's marker, "See paragraph <n>, <title>, page", "elsewhere in these specifications"); cite.py
read a paragraph row's `ordinal` as a rule number, so `GR2` could name `p2` in a clause holding both; `rule_moves`'
split assumed a carved paragraph always ends its parent; the content-keyed scan had no block digest for a `not-rules`
record to key on; and two standing-list entries said "no rule states it" where a minted row now does.

**Counts** (`inventory_view.py --summary --edition 1985`; `edition_status.py --edition 1985`):

| | before | after |
|---|---|---|
| 1985 rules | 2,275 | 2,911 (629 unit rows, 7 rows of two admitted blocks) |
| units pending | 1,026 | 3 (kb/Work PB6001) |
| clauses pending | 0 | 2 (§1985:III.2, §1985:B.2.1: overturned `not-rules`, no mechanism mints their rows) |
| 1985 GAP | 0 | 657 (636 new rows + 21 reopened parents) |
| catalog prose holes | 840 | 357 |
| KEPT rows owed a code read | 1 | 4 |

The import wrote 999 dispositions: 629 rows (GR 413, SR 200, RV 11, AR 5), 251 `restates`, 116 `not-a-rule` (data 57,
lead-in 43, cross-reference 16), 3 pending. Of the 37 `not-rules` blocks 33 were upheld and 4 overturned. Of 29 parents
the carve shrank, 8 kept their change records through an upheld recheck and 21 were reopened (16 whose reason quotes the
carved words, 5 whose recheck failed). The 2023 and 2002 `rules` arrays are byte-identical to main's.

**Not done.** PB5452 stays open: three units are held by segmentation defects (kb/Work PB6001: a second numbered list in
a clause whose block is disposed, and HTML tables), and two clauses await a way to mint their rows.
