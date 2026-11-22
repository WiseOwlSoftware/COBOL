## 2026-11-21 - what of this note is still 1985's: only the `item <n>` designator (wave 20262132 group c)

Of the remaining items of `2026-11-20-items-6-and-3-for-1985.md`, re-checked at ba114d006b:

- **Item 3, 1985 (the mint):** done by PB5452 (landed): the 1985 catalog holds 824 rows minted from units, each equal in
  text to its segmentation unit (`extract_rule_catalog.units_1985`, 824 of 824 compared), and the 1985 manifest holds no
  pending unit (`cite.pending_units(1985)`: 548 GR, 214 SR, 533 restates, 92 not-a-rule).
- **Item 5, a 1985 unit's `box`:** done: the minted rows carry it (109 whole, 18 part, 714 none).
- **Item 4, `item <n>`: NOT done, and 1985 needs it.** `cite.py --check "§1985:VIII.1.3.4 item 1 b" "<text>"` and
  `"§1985:B.2.2 item 45"` answer FAIL "'item ...' is not a rule designator", and 66 tracked files under `src/`, `tests/`,
  `conformance-suite/`, `docs/` and `scripts/` carry a 1985 citation in that form
  (`git grep -l` on the edition prefix, a locator and ` item `;
  the most frequent: `§1985:VI.6.4.4 item 2` 20 times, `§1985:III.2 item 3` 10, `§1985:B.2.2 item 45` 9). So no such
  citation is checked. `cite.unit_designator` also renders a second list's item as `item L2.3`, a form `citation.parse`
  refuses (the lead group o of wave 20262131 joined to PB6015).
- Items 1 (2023's reader) and the 2002 `candidate` stay 2023's and 2002's.

So `editions` stays [1985, 2002, 2023] while item 4 is open; PB479 and PB5448, whose 1985 part was the mint, drop 1985
the same day (their `2026-11-21-editions02.md`).
