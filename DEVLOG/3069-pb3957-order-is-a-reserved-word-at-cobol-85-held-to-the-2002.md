## Entry 3069 — 2026-11-03 19:44 PST — PB3957 — ORDER is a reserved word at COBOL-85, held to the 2002 standard's own list of the words it added

**Mechanism.** `reserved-words.json` carried `ORDER r85=false`, so `--std 85` compiled `01 ORDER PIC 9.` while the
85 SORT format itself prints `WITH DUPLICATES IN ORDER`. The flag was not GnuCOBOL's: GnuCOBOL's 85 list reserves
ORDER, and `gen-reserved-words.ps1` removed it on a "CCVS proof" (DEVLOG 585) that ST127A and the sort differentials use
it as a data name. Re-probed on this build: `ord1.cob` compiled at 85 and drew COBOL0901 at 2002+. The proof is false:
ST127A line 257 is `WITH DUPLICATES IN ORDER`, the keyword, and no CCVS-85 program declares a data item named ORDER
(grep over `tests/nist/programs`: every other hit is a comment or a literal).

**Authority.** No 1985 text is held, but the register holds the 2002 post-ballot text (kb/Work PB3602 source P2). Its
§8.9 list holds ORDER, and its Annex F.1 item 6, "The following reserved words have been added", names the 84
alphanumeric words 2002 reserved that the previous standard did not. ORDER is not among them, so the previous standard
reserved it. Run across the whole 2002 list, ORDER is the only word the item does not name that the 85 flag lacked; the
other 318 agree, which is also the check that the item is a usable witness. (The item also names LAST, which GnuCOBOL's
85 list reserves: the check runs one direction only, "not added" implies "reserved at 85".)

**Fix shape (root, not a one-row flip).** The override block is deleted; the F.1 list becomes data
(`reserved-words-2002.json addedByAnnexF1`, words only, the printed `GROUUP-USAGE` read as GROUP-USAGE); the generator
throws if a 2002 §8.9 word the item does not name is absent from its 85 source, and `ReservedWordsDriftTests` holds the
committed table to the same text, so the next such word fails a test instead of a user's program. Regenerated: one row
changes. The consequence was bigger than the row. PB3794 had given ORDER a `keywordWhereFree` determination and a generated
`orderKeyword` rule because ORDER was free at 85. With no free edition the determination is dead (the one way to free a word,
`>>COBOL-WORDS UNDEFINE`, withdraws "any syntax requiring the use of" it, §7.3.10.4 GR3), so the row loses it, `orderKeyword`
is gone, `sortDuplicatesPhrase` and `orderTableClause` spell the bare token, and `gen-cobol-words.ps1` step 5c plus a
`CobolWordsDriftTests` assertion refuse a determination on any word reserved at every edition. The GnuCOBOL conformance
profile's `extra-reserved-words` 85 list loses ORDER (it is the standard's word now, not an extra). VCR row 7.19 argued the
ORDER TABLE edge from "ORDER is not reserved at 85"; it now states the 2002 text's SPECIAL-NAMES format instead.

**Goldens.** `8.3.2-COBOL-words/pb3957-order-reserved-at-every-edition` (renamed from `pb3794-order-reserved-from-2002`,
refused at 85, 2002, 2014, 2023 with COBOL0901 naming ORDER). Deleted: `14.9.40-SORT/pb3794_order_data_item_duplicates_in_order`
(a legal COBOL-85 program only while ORDER was free), `_unassigned/pb704-order-as-data-name` (the same refusal, weaker,
now covered), and ORDER removed from the COBOL-85 acceptance lane `pb693_reserved_words_as_data_names`. The keyword use is
still witnessed at 85 by `pb704_sort_duplicates_in_order_85`. Inventory: FMT-14.9.40.2, GR-14.9.40.4-3 and SR-8.3.2.1-1
change witnesses only (batch `w20261856c-PB3957`), no row closed, GAP 1 -> 1. D-RW3 records the determination.

**Self-review.** Architecture: one flag source per edition, the 85 source held to a text; no wrapper. Full code: the
guard cites §7.3.10.4 GR3 (`cite.py --check` OK). Duplication: the invariant is stated in the generator and the drift test
over the same data. Found and left: the LAST question above, which needs the 1985 text. The gate's verdict line is in the
branch report.

