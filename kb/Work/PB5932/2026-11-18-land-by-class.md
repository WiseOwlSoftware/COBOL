## Decision (controller, 2026-11-18): step 6b lands by name class, behind a temporary shrink-only baseline

**What the design said.** `docs/rearchitecture/DESIGN-listing.md` section 11 "Step 6b": the step lands only with
`NameTableOwnershipDriftTests` green, that is with every name table of the binder private to `SymbolTable`, every lookup through
`Identify` or classified, and every comparison of COBOL names moved in or classified.

**What the first session measured.** The remainder after the cd-names and the file-names moved (this note's dated file
`2026-11-18-step-6b-progress`) is the binder's whole name resolution: 56 tables, 63 unclassified lookups and 206 unclassified
comparison sites, in eight name classes, the data-name class alone being `NameResolution`, `ReferenceResolver.Identify` and about
1,000 lines of `DataBinder`. Each class moves WITH its resolution code (a private table is read only by the table's own members).
One branch would carry that for many sessions against a moving main, merging every day's binder changes into a restructuring of
the binder.

**Decided.** Step 6b lands in slices, one or more whole name classes per slice, behind a TEMPORARY baseline:

- `NameTableOwnershipDriftTests` reads `tests/WiseOwl.Cobol.Tests.Unit/Listing/name-table-ownership.baseline.json`, the
  measured findings of rules (1), (3) and (5) not yet moved, each by its key (the member for rules (1) and (3), the site for
  rule (5)).
- A finding neither classified nor in the baseline is red: no new name table, lookup or comparison may appear.
- A baseline entry no longer found is red: a slice lowers the baseline in the same change, so it only shrinks. The baseline is
  under a ceiling in the test that only falls, names no rule but (1), (3) and (5), and shares no key with a classification.
- Rules (2) and (4) take no baseline (both are green).
- Each slice: whole name classes from the progress table, each with its resolution code; every lookup through `Identify`;
  every comparison moved in or classified; `python scripts/arch/compare_oracle.py` IDENTICAL after every source change; the
  baseline and its ceiling lowered; a dated file beside this note saying what moved and what remains.
- The LAST slice empties the baseline and DELETES the file and the code that reads it, so the test is again the design's five
  rules, green with no exceptions, and no permanent ratchet remains. It also measures the no-listing path against its parent
  (section 9) and flips PB5932 to landed. Step 7b branches after it.

**Why.** Each slice is a behaviour-neutral restructuring with its own differential, so landing it early carries no risk to what
any program means, while holding it on a branch costs a merge of the binder every day. The baseline keeps the guarantee the
design wants between slices: nothing new can bypass the table (the first red), and nothing moved can be forgotten in the
baseline (the second red). The codebase's precedent for such a restructuring is `architecture-edges.json`'s tolerated rows,
which only fall; unlike those, this baseline has a fixed end, the last slice, which deletes it.

**Slice 1** (branch `listing-step-6b`): the drift test and its analyzer, `SymbolTable` in the binder layer with `Identify`,
`Declarations` (`DeclarationFact`) and `CobolName`, the two classifications, the cd-names and the file-names, and the baseline
(325 entries: 56 tables, 63 lookups, 206 sites). The design's sections 6.7.1 and 11 say so (the corrections marked "step 6b").
