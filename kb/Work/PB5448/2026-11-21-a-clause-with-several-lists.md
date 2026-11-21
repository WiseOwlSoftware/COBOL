## 2026-11-21 — an addendum to point 2: the id of an item of a clause's later numbered list, and a paged table's row

Point 2 gave a numbered item of a prose clause the id `<kind>-<clause>-<n>`, which assumed one numbered list per clause.
kb/Work PB5452's pilot found clauses with several (§1985:IV.4.2.2.1.1 prints two lists, §1985:X.1.3.8 five), whose ids
would collide; kb/Work PB5998 fixed the segmentation and extends the scheme in this decision's own terms:

- An item of the clause's FIRST numbered list keeps `<kind>-<clause>-<n>`; an item of its m-th list (m > 1) is
  `<kind>-<clause>-L<m>.<n>`, the form a rule block's later list already has (`citation.rule_designator`,
  `extract_rule_catalog`'s rule ids, `rule_catalog_1985.add_row`). A list starts where an item's number is not the last
  item's next. So a clause that gains a second list never re-mints the first list's ids, and `-L<m>.<n>` cannot collide
  with `-p`, `-T` or `-C`, nor with a numbered rule, which a prose clause has none of.
- A table printed across pages is one table, its body rows counted on across the pages (`-T<t>.<r>`, `r` never
  restarting at a page), since each page's part repeats the header and the same number (§1989:A.2.4's Table 1, three
  pages, `T1.1` to `T1.42`).
- No two units of a clause share a label: the segmentation refuses such a clause, so one id is never minted twice.

The design's "Phase A, continued", point 2, states the extended scheme; its paragraph "Step 1, corrected" gives the counts.
