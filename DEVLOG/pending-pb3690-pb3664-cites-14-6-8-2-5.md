## PB3690: PB3664's sign rule cites §14.6.8.2 5) for its numeric-edited receiver, not rule 4

PB3664's branch reviewer found that the new editing-sign rule cited §14.6.8.2 4), which covers a fixed-point NUMERIC
receiver, for a numeric-EDITED one. The quoted text passed `cite.py --check`, but it answered a different question. The
code comment in `CobolEdit.cs`, the summary of `CobolEditStoredSignTests`, the header of the golden
`pb3664_edited_sign_stored_value` and the `docs/CONFORMANCE.md` determination now cite §14.6.8.2 5): "further
alignment, zero fill or truncation, and transfer of digits take place as described in the general rules and editing
rules in 13.18.40". The same four sites now present §8.8.4.2.4's "Zero is considered a unique value regardless of the
sign" as what that clause says of comparisons, corroboration rather than the governing rule. A sweep of every
`§14.6.8.2 4)` citation in `src`, `tests`, `docs`, `scripts` and `conformance-suite` found no other numeric-edited
receiver: each of the rest names a fixed-point numeric item. No behavior changes.
