# PB6210 addendum (2026-11-24): the mismatch is every edition's, and a self-test pins the printed form

Reproduced on main @ 070078bb16 with the 1985 query of cite.py's own self-test (`q85b`, "The file referenced by file-name-1 is
sequenced"): `cite.py --find --edition 1985` prints `§1985:p.XI-18 (6) b.`, and `cite.py --check "§1985:XI.4.4.4 (6) b." ...`
answers `FAIL: '(6) b.' is not a rule designator`. The self-test case "--find prints the 1985 path in the 1985 style"
(`scripts/spec/cite.py`, near line 1575) expects the printed `(6) b.` form, so `--find`'s printed-label output is a choice the
tests pin, not an oversight in one clause. The same search also cites the hit by page (`p.XI-18`) rather than by clause.

**What the fix has to do:** print, for each hit, the designator `--check` accepts. That is the catalog unit whose `lines` hold
the hit (`RuleBook.rules[locator]` and `RuleBook.printed_items(locator)`; for the 2002 case `item L2.2` at transcription index
7583), with the sub-item path glued as `item_designator` renders it. The printed label can stay beside it as a reading aid. The
self-test case changes in the same change, and every edition's `--find` hits get the round-trip test that the note's "Done when"
names. It is a fix-lane change to cite.py's output contract, not a one-line patch.
