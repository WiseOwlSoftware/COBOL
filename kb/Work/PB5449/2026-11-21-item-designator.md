## 2026-11-21 - item 4 built: `item <n>` is a designator; every 1985 item citation checks (wave 20262132 group e)

Branch `train85d-itemcite` (main ba114d006b + `origin/train85b-g3-cite`, group s's one designator grammar, + group c's
`origin/train85d-register`, whose `cite.py` it builds on). The decision it implements is recorded as an addendum to
decision PB5448 (`kb/Work/PB5448/2026-11-21-item-designator.md`).

**Built.** `citation.ITEM_DESIGNATOR` (`item <n>`, `item L<m>.<n>`, the numbered rule's glued sub-item path:
`item 3c`) in the one `RULE` grammar; `cite.RuleBook.printed_items`, the one reading of the items a clause prints (its
rows cited by number, its item units with no row, else its printed top-level labels); `cite._resolve_item` with the
failure kinds `no-item` and `item-is-a-rule`; `cite.item_designator` (the canonical designator of an item named
loosely, when one list alone prints it); `unit_holding` naming an item; `Rule.designator` of an item ROW is its
numbered designator (it rendered `item 1`, which the parser refused); `_unparsed_suggestions` lost its item exemption
and requires every no-row item it can suggest to resolve; `audit_code_citations.canonical_of` / `canonicalize_line`
rewrite a loose item (`item (3) c)`) and an `item-is-a-rule` that one list alone prints. Self-tests: `citation.py`,
`cite.py`, `audit_code_citations.py`, each with the new cases planted.

**1985: done.** Every `§1985:` item citation of the RESOLVE population checks (84 of 87; the three left are lines of
this note's own history addenda, quoted as the forms cite.py refused, now baseline entries `item-is-a-rule`). 51 sites
re-pointed by hand, each read in its context (the commit lists them); the 1985 entries of the RESOLVE baseline went
from 132 to 59 (56 are spaced sub-item paths, group s's family, outside this item).

**Left, and not 1985's:**
- **2023's `item <n>` citations**, now read as designators: 1,064 `item-is-a-rule` (most `§2023:9.1.13.x item <n>`,
  the I-O status lists the catalog holds as GRn; `--canonicalize` rewrites each that one list alone prints), 17
  `no-item`, 100 loose item spellings; all in the baseline (6,056 -> 6,050, `--grow`). Not swept here: train C
  rewrote 5,058 lines of `src/` and `tests/` with `--canonicalize` and a 2023 sweep on this branch would conflict with
  it; run `python scripts/spec/audit_code_citations.py --canonicalize src/ tests/ docs/ conformance-suite/` on the
  merged train, then `--write-baseline`.
- Items 1 and 3 (2023's reader and margin levels) and the 2002 unit's `candidate`, as written in
  `2026-11-20-items-6-and-3-for-1985.md`.
