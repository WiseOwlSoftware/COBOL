## 2026-11-21 - two more designators cite.py renders and then refuses (the 85w reads, wave 20262131 group o)

**Sites:** `scripts/spec/cite.py#unit_designator`, `scripts/spec/citation.py` (`parse`, `_rule`, `rule_designator`).

1. **A list item of an unnumbered paragraph (`L<m>.<n>`).** The 85w mint (PB5452) minted rows such as
   `1985-GR-III.2-L2.2` (the second item of the Integer definition's list). `cite.py --check` locates the text "under
   item L2.2", and `unit_designator` renders it `item L2.2`, but the parser refuses both spellings (re-probed on branch
   train85b-reads, 2026-11-21):

       python scripts/spec/cite.py --check "§1985:III.2 L2.2" "a numeric data item"
       FAIL: 'L2.2' is not a rule designator: write SR n, GR n, AR n or RV n ...
       python scripts/spec/cite.py --check "§1985:III.2 item L2.2" "a numeric data item"
       FAIL: 'item L2.2' is not a rule designator: ...

   So every `L<m>.<n>` row is uncitable by the form its own tool prints (reader lead, g-03 slice).
2. **A lettered item of a numbered general rule** (§1985:VII.1.3.5, the I-O status values): cite.py reports the text
   under `GRL2.1d` / `GRL2.4g` and then refuses that designator, so "I-O status = 07" and "= 48" have no valid citation
   (reader lead, g-04 slice).

Same mechanism as this note's first finding (two paths for one designator): the renderer and the parser must share one
grammar of designators, with a drift self-test that every designator the renderer prints parses.
