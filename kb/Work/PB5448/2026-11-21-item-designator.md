## 2026-11-21 - addendum: the designator of a numbered list item (point 5), decided and built (kb/Work PB5449 item 4)

Decided by the implementer of wave 20262132 group e (account 2), under the ownership the owner delegated: point 5 left
"a prose clause's item number" without a designator because the tree wrote `item <n>` for lists that are no unit.

**The decision.** `item <n>` (`item L<m>.<n>` for an item of a clause's later list, its sub-item path glued as a rule's)
names a numbered list item that NO numbered rule designator names: an item unit with no row, or, in a clause whose lists
are neither rows nor units, an item of its printed list (an appendix's list, a printed page, 2023's Annex A.1). An item
the catalog holds as a numbered rule (a rule block's rule, an admitted prose paragraph's, an item row minted as one) is
cited by that rule's designator and `item <n>` there is refused, naming it.

**Why this shape and not the other.** The other choice, `item <n>` for every item of a clause that prints no rule
heading, would rename the citation of every item row and of every numbered rule of an admitted prose paragraph (771
1985 rows, the catalog's own `citation` field and some 200 stored and written citations of the 76 item rows alone),
and it would make the manifest's recorded admission of those paragraphs as rule blocks say one thing and their citations
another. One spelling per printed thing holds either way; this one keeps every designator the catalog already writes,
and when a later mint makes a pending item a row, its `item <n>` citations fail `item-is-a-rule` and
`--canonicalize` rewrites them, so the next case is automatic. The cost: an item is cited `GRL4.3` in a glossary entry
that prints no rule (§1985:III.2's Text Word), the catalog's designator, not the printed word.
