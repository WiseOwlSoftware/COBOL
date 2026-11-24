## PB2627 and PB2724 carried onto main; PB2622 and PB3231 re-measured and left for their own dispatches

**What this branch carries.** Wave 20262161 group A finished PB2627 and PB2724 on `worktree-wf_b65a448b-ca0-1` and
returned SPLIT, so the work was not landed. Wave 20262164 group A cherry-picked those unlanded commits onto main. It
also merged `worktree-wf_c37a519f-ba4-15` (PB5707, in the merge queue) and the current main. That main includes PB3227
(pull request 2064) and PB5725, so the PB3227 commits that were replayed add nothing to the net diff. The citation
baseline `scripts/spec/data/cite-resolve-baseline.json` was regenerated for the merged tree (1935 entries, then
`audit_code_citations.py`: 0 findings).

**One stale comment fixed.** In `CobolData.g4`, the comment above `propertyClause` said "EXTERNAL clause". It now names
the PROPERTY clause, §2023:13.18.42.

**Not done.** PB2622 and PB3231 were bundled into this group again, but each is a mechanism of its own. A 2026-11-24
re-measure found PB2622 at 1467 descriptor-plus-message emit sites over 678 descriptors in 149 files, where the note
counted 862. PB3231's forgers moved to `ObjectPropertyResolver.cs`, and the change to its input reaches 278 uses of
`DataReferenceContext`. Each note has a 2026-11-24 addendum with the measured surface and the re-sited code sites.
