# PB5150 landing - adjudication result (2026-11-14)

**Verdict: the standard decides, and the golden was wrong.** Two same-named subordinates of GLOBAL records in one source element violate §2023:8.4.2.1 ("The same name shall not be used both as the name of an item possessing the global attribute and as the name of any other data item described in the source element that describes that global data item"; checked with `cite.py --check`). A subordinate possesses the attribute: §2023:13.18.27.4 GR1, "All data-names subordinate to a global name are global names" (checked). §2023:8.4.6.2.1 3) b) 1. does not conflict: it chooses among same-named items of DIFFERENT nested source elements.

**Re-probe (Debug build, this tree).** `pb1047-two-globals-one-element-ambiguous` was refused only at the reference (COBOL1639); with no `DISPLAY X` it compiled.

**Fix.** `ScreenGlobalNameUniqueness` now takes an item as global when its ROOT is a global record (`IsGlobalRecord(item.Root)`), not only when it is the record itself. The pb1047 golden now expects COBOL6862 at the later X; new negative `pb5150-global-subordinate-name-reused-by-local-record` and positive `pb5150_global_subordinate_name_unique_beside_other_names`.

**Sibling note (not fixed, outside the mechanism).** GR3 of §2023:13.18.27.4 (a GLOBAL on a REDEFINES entry makes only the subject global) is not consulted by the screen: a redefining 01 carrying GLOBAL is treated as global itself.
