## The tree audit, and the sweep outside C# (wave 20262131 group s, branch train85b-g3-cite)

**The audit (rule 5: the next case is automatic).** `citation.loose_designator` names every designator `scan` cannot
read after an edition-form citation: a spaced sub-item path (`GR1 b)`, `GR3 c) 2)`), a rule named in words
(`rule 2`, `<x> phrase rule 1`), a bare printed label (`2)`, the `§2023:8.3.2.1 1)` of PB4958's golden) and a list item
(`item 1`). `audit_code_citations.py` RESOLVE refuses each as `designator-not-canonical` (a `cite.FAILURE_KINDS` kind)
over RESOLVE's population, except the work register's note bodies (their owner's history; `REGISTER_HISTORY`). The
debt it found is RESOLVE's shrink-only baseline (`scripts/spec/data/cite-resolve-baseline.json`), so a NEW one is red.

**The canonical form, where there is exactly one** (`audit_code_citations.canonical_of`): the spaced path glued to its
rule (`GR3c.2`); a rule named by number turned into the designator the rule catalog gives that ordinal
(`cite.designator_of`: exactly one rule of that number in the clause, and no list that restarts, since `§2023:8.3.2.2
rule 1` was GRL4.1's text and `§2023:15.4.1 RV1` RVL2.1's). Never for a list (`a) b)`, `37) 39)`, `a)/c)`) or an item.
`--canonicalize <prefix>...` rewrites those in place; RESOLVE then checks each like any other citation.

**Run** over `conformance-suite/ docs/ tests/version-matrix/inventory kb/` (C#, grammar, DEVLOG and kb/Work left, the
first two to the train's C# citation groups): 3,468 lines. RESOLVE read the rewritten designators and exposed five that
name a sub-item or ordinal the rule lacks; those lines were restored to their old spelling (still on the baseline, to be
derived): `docs/conformance/7/DOC-A.1-163.md:2` and `docs/OO_DESIGN.md:178` (`§2023:12.3.8.3 SR6` has no sub-item b),
`docs/rearchitecture/DESIGN-frontend-grammar.md:1739` (`§2023:14.5.3.3 GR1` has only GR1a),
`tests/version-matrix/inventory-1985/1985-GR-VIII.4.4.4-13.json:14` (`§1985:VIII.1.3.4 GR4` reads no sub-items; a lead on
the 1985 label reader for that rule). The fifth, `docs/conformance/5/A.4.4.md`, chained GR30 to the wrong clause: it is
`§2023:14.9.39.4 GR30`, written so now.

**This note's named sites:** the `13.18.40.3 SR15` of `pb4641_float_edited_narrow_receiver_quotient.cob`,
`pb4641_float_edited_receiver_quotient.cob` (and `pb4975_...`, `pb66-float-edited-picture-symbols.cob`) is
`§2023:13.18.40.3 SR15` (checked with its text); `pb4958_key_named_typedef_is_a_user_word_85.cob:10` is
`§2023:8.3.2.1 SR1` (checked: "Reserved words shall not be used as user-defined words or system-names"). Not done here
(C#, the train's groups h/q own those files): `EcEmitter.cs`, `NumericRenderer.cs`, `CobolDec.cs` (`r3d`), and
`RecordLayout.cs` (`§2023:8.5.1.9.1 2`, a bare digit with no paren, which the audit does not read: a lead).

**Left, by family (the baseline, 6,041 entries at the first pass; 6,056 after 15 list-spanning rewrites were undone, a list such as `rules 2, 3 and 7` having no one canonical form):** spaced sub-item 2,033 (C# 1,930, grammar 22, other 81: lists),
`rule N` 1,016 (C# 902), bare label `N)` 1,751 (C# 1,558), `item N` 1,241 (C# 668; its designator waits on kb/Work
PB5449 item 4). The C# burn-down is one command once the C# groups have merged: `python scripts/spec/audit_code_citations.py
--canonicalize src/ tests/` then `--write-baseline`, then derive the rest from the standard.
