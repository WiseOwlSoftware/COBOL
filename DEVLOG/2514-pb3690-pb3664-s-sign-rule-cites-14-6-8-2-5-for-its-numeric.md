## Entry 2514 — 2026-10-24 20:36 PDT — PB3690: PB3664's sign rule cites §14.6.8.2 5) for its numeric-edited receiver, not rule 4

PB3664's branch reviewer found that the new editing-sign rule cited §14.6.8.2 4), which covers a fixed-point NUMERIC
receiver, for a numeric-EDITED one. The quoted text passed `cite.py --check`, but it answered a different question. The
code comment in `CobolEdit.cs`, the summary of `CobolEditStoredSignTests`, the header of the golden
`pb3664_edited_sign_stored_value` and the `docs/CONFORMANCE.md` determination now cite §14.6.8.2 5): "further
alignment, zero fill or truncation, and transfer of digits take place as described in the general rules and editing
rules in 13.18.40". The same four sites now present §8.8.4.2.4's "Zero is considered a unique value regardless of the
sign" as what that clause says of comparisons, corroboration rather than the governing rule. The first sweep claimed no other
numeric-edited receiver cited rule 4, and the branch reviewer refuted it: its pattern missed the `r4` spelling. Re-run over
every spelling and read hit by hit, it found seven more sites (the BLANK WHEN ZERO golden pb566, two more
`CobolEdit.cs` comments, both numeric design docs, PB566's note and that row's inventory notes), all now citing
rule 5; every other hit names a fixed-point numeric receiver. PB3687, a duplicate of PB3690 the enqueue unit's
clerk filed from the same lead, is retired as folded into it. No behavior changes.

